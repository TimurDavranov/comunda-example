using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Camunda_TZ.Entities;
using Camunda_TZ.Models;
using Microsoft.EntityFrameworkCore;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camunda_TZ.Services;

public class DistributionJob(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() =>
        {
            var client = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IZeebeClient>();

            client.NewWorker()
                .JobType("distributionJob")
                .Handler(async (c, j) => await DistributionJobHandler(serviceScopeFactory, c, j))
                .Name("Distribution job")
                .MaxJobsActive(5)
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task DistributionJobHandler(
        IServiceScopeFactory serviceScopeFactory,
        IJobClient jobClient,
        IJob job)
    {
        var token = await GetToken(serviceScopeFactory);

        if (token is null)
        {
            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "success", false }
                }))
                .Send();
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var supportGroupName = configuration["Camunda:SupportGroupName"];

        if (string.IsNullOrWhiteSpace(supportGroupName))
        {
            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "success", false }
                }))
                .Send();
            return;
        }

        var groups = await GetGroups(serviceScopeFactory, token.AccessToken, token.TokenType, supportGroupName);

        var supportGroup =
            groups.FirstOrDefault(s => s.Name.Equals(supportGroupName, StringComparison.OrdinalIgnoreCase));

        if (supportGroup is null)
        {
            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "success", false }
                }))
                .Send();
            return;
        }

        var users = await GetGroupUsers(serviceScopeFactory, token.AccessToken, token.TokenType, supportGroup.Id);

        if (users.Count == 0)
        {
            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "success", false }
                }))
                .Send();
            return;
        }

        var tasks = await GetTasks(serviceScopeFactory, token.AccessToken, token.TokenType);

        UserModel? selectedUser = users
            .Where(user =>
                !tasks
                    .Where(s => !s.TaskState.Equals("completed", StringComparison.OrdinalIgnoreCase))
                    .Any(s =>
                        s.Assignee.Equals(user.Id.ToString(), StringComparison.OrdinalIgnoreCase) ||
                        s.Assignee.Equals(user.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList()
            .FirstOrDefault();

        if (selectedUser is null)
        {
            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "success", false }
                }))
                .Send();
        }
        else
        {
            await using var db = await serviceScopeFactory.CreateScope().ServiceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();

            var id = JsonNode.Parse(job.Variables)?["id"]?.GetValue<long>() ?? 0;

            var ticket = await db.Tickets
                .FirstOrDefaultAsync(s => s.Id == id);

            if (ticket is null)
                throw new ArgumentException("Could not find ticket!");

            ticket.Assignee = selectedUser.Username;

            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(new AssignmentModel()
                {
                    Success = true,
                    Status = TicketStatus.InProcess.ToString(),
                    Task = job.Key.ToString(),
                    SelectedAssignee = selectedUser.Username,
                    SelectedAssigneeEmail = selectedUser.Email
                }))
                .Send();
        }
    }


    private static async Task<List<TaskModel>> GetTasks(
        IServiceScopeFactory serviceScopeFactory,
        string tokenType,
        string accessToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var identityRestApi = configuration["Camunda:TaskListUrl"];
        if (string.IsNullOrWhiteSpace(identityRestApi))
            return [];

        using var httpClient = serviceScopeFactory.CreateScope().ServiceProvider
            .GetRequiredService<IHttpClientFactory>().CreateClient();

        httpClient.BaseAddress =
            new Uri(identityRestApi);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            tokenType,
            accessToken);

        var response = await httpClient.PostAsync($"v1/tasks/search", null);

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            var token = await GetToken(serviceScopeFactory);
            if (token is null)
                throw new Exception("Auth service not working!");

            accessToken = token.AccessToken;
            tokenType = token.TokenType;

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                tokenType,
                accessToken);

            response = await httpClient.PostAsync($"v1/tasks/search", null);
        }

        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TaskModel>>(json) ?? [];
    }

    private static async Task<AccessTokenModel?> GetToken(
        IServiceScopeFactory serviceScopeFactory)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        using var httpClient = serviceScopeFactory.CreateScope().ServiceProvider
            .GetRequiredService<IHttpClientFactory>().CreateClient();

        var url = configuration["Camunda:AuthUrl"];
        var clientId = configuration["Camunda:ClientId"];
        var clientSecret = configuration["Camunda:ClientSecret"];
        var realm = configuration["Camunda:Realm"];
        var username = configuration["Camunda:Username"];
        var password = configuration["Camunda:Password"];

        if (string.IsNullOrWhiteSpace(url) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(realm))
            return null;

        httpClient.DefaultRequestHeaders.Accept.Clear();

        httpClient.BaseAddress = new Uri(url);

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("grant_type", "password")
        ]);

        var response =
            await httpClient.PostAsync($"/auth/realms/{realm}/protocol/openid-connect/token", content);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AccessTokenModel>(json);

        return result;
    }

    private static async Task<List<UserGroupModel>> GetGroups(
        IServiceScopeFactory serviceScopeFactory,
        string accessToken,
        string tokenType,
        string search = "")
    {
        using var scope = serviceScopeFactory.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        using var httpClient = serviceScopeFactory.CreateScope().ServiceProvider
            .GetRequiredService<IHttpClientFactory>().CreateClient();

        var url = configuration["Camunda:IdentityUrl"];
        if (string.IsNullOrWhiteSpace(url))
            return [];

        httpClient.BaseAddress = new Uri(url);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);

        var response = await httpClient.GetAsync($"/api/groups?search={search}");

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            var token = await GetToken(serviceScopeFactory);
            if (token is null)
                throw new Exception("Auth service not working!");

            accessToken = token.AccessToken;
            tokenType = token.TokenType;

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                tokenType,
                accessToken);

            response = await httpClient.GetAsync($"/api/groups?search={search}");
        }

        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<UserGroupModel>>(json);

        return result ?? [];
    }

    private static async Task<List<UserModel>> GetGroupUsers(
        IServiceScopeFactory serviceScopeFactory,
        string accessToken,
        string tokenType,
        Guid groupId)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var identityRestApi = configuration["Camunda:IdentityUrl"];
        if (string.IsNullOrWhiteSpace(identityRestApi))
            return [];

        using var httpClient = serviceScopeFactory.CreateScope().ServiceProvider
            .GetRequiredService<IHttpClientFactory>().CreateClient();

        httpClient.BaseAddress =
            new Uri(identityRestApi);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            tokenType,
            accessToken);

        var response = await httpClient.GetAsync($"api/groups/{groupId}/users");

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            var token = await GetToken(serviceScopeFactory);
            if (token is null)
                throw new Exception("Auth service not working!");

            accessToken = token.AccessToken;
            tokenType = token.TokenType;

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                tokenType,
                accessToken);

            response = await httpClient.GetAsync($"api/groups/{groupId}/users");
        }

        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UserModel>>(json) ?? [];
    }
}