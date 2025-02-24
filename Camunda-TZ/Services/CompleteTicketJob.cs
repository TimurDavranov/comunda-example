using System.Text.Json;
using System.Text.Json.Nodes;
using Camunda_TZ.Entities;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camunda_TZ.Services;

public class CompleteTicketJob(IServiceScopeFactory serviceScopeFactory) : IHostedService 
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() =>
        {
            var client = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IZeebeClient>();

            client.NewWorker()
                .JobType("completeTicketJob")
                .Handler(async (c, j) => await CompleteTicketJobHandler(serviceScopeFactory, c, j))
                .Name("Update ticket status")
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
    
    private static async Task CompleteTicketJobHandler(
        IServiceScopeFactory serviceScopeFactory,
        IJobClient jobClient,
        IJob job)
    {
        var variables = JsonNode.Parse(job.Variables);

        if (variables is null)
            throw new ArgumentException("Invalid ticket!");

        var ticketId = variables["id"]?.GetValue<long>() ?? 0;

        using var scope = serviceScopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(s => s.Id == ticketId);
        if (ticket is null)
            throw new ArgumentException("Invalid ticket!");

        var isComplete = variables["checkbox_re029"]?.GetValue<bool>() ?? false;
        ticket.SupportNote = variables["textarea_comment"]?.GetValue<string>() ?? string.Empty;
        ticket.Status = isComplete ? TicketStatus.Completed : TicketStatus.InProcess;

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await jobClient.NewCompleteJobCommand(job.Key)
            .Variables(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "status", ticket.Status.ToString() }
            }))
            .Send();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var smtpPort = configuration["Camunda:SmtpPort"];
        var smtpHost = configuration["Camunda:SmtpHost"];

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(smtpPort))
            return;

        var clientName = variables["textfield_title"]?.GetValue<string>() ?? string.Empty;
        var clientEmail = variables["textfield_email"]?.GetValue<string>() ?? string.Empty;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("demo", "demo@acme.com"));
        email.To.Add(new MailboxAddress(clientName, clientEmail));
        email.Subject = $"Обращение #{variables["task"]?.GetValue<string>() ?? string.Empty}";
        email.Body = new TextPart("plain")
            { Text = $"Сообщение от сотрудника:\n {ticket.SupportNote}.\n Статус: {ticket.Status}" };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(smtpHost, int.Parse(smtpPort), MailKit.Security.SecureSocketOptions.None);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}