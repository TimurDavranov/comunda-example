using System.Text.Json;
using Camunda_TZ.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camunda_TZ.Services;

public class SendMailJob(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() =>
        {
            var client = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IZeebeClient>();

            client.NewWorker()
                .JobType("sendMailJob")
                .Handler(async (c, j) => await SendMailJobHandler(serviceScopeFactory, c, j))
                .Name("Send mail to employee")
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

    private static async Task SendMailJobHandler(
        IServiceScopeFactory serviceScopeFactory,
        IJobClient jobClient,
        IJob job)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var smtpPort = configuration["Camunda:SmtpPort"];
        var smtpHost = configuration["Camunda:SmtpHost"];

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(smtpPort))
            return;

        var variables = JsonSerializer.Deserialize<AssignmentModel>(job.Variables);
        if (variables is null)
            return;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("demo", "demo@acme.com"));
        email.To.Add(new MailboxAddress(variables.SelectedAssignee, variables.SelectedAssigneeEmail));
        email.Subject = "Новая задача";
        email.Body = new TextPart("plain") { Text = $"Вам назначена новая задача #{variables.Task}" };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(smtpHost, Int32.Parse(smtpPort), MailKit.Security.SecureSocketOptions.None);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        await jobClient.NewCompleteJobCommand(job.Key)
            .Send();
    }
}