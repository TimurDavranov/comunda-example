using System.Text.Json;
using Camunda_TZ.Entities;
using Camunda_TZ.Models;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Zeebe.Client;

namespace Camunda_TZ.Services;

public class CheckNewTicketsJob(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var scope = serviceScopeFactory.CreateScope();

                var client = scope.ServiceProvider.GetRequiredService<IZeebeClient>();
                await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContextAsync(cancellationToken);

                var tickets = await db.Tickets
                    .Where(s => s.Status == TicketStatus.New).Include(ticket => ticket.Attachments)
                    .ToListAsync(cancellationToken: cancellationToken);

                foreach (var ticket in tickets)
                {
                    var model = new StartProcessModel()
                    {
                        id = ticket.Id,
                        select_type = ticket.Type.ToString(),
                        textfield_email = ticket.ClientEmail,
                        textfield_title = ticket.Title,
                        textfield_username = ticket.ClientName,
                        attachments = ticket.Attachments.Select(s => new StartProcessAttachmentModel()
                        {
                            fileName = s.FileName,
                            fileUrl = GetFileLink(s.Bucket, s.Path, s.StorageName)
                        }).ToList()
                    };

                    await client.NewCreateProcessInstanceCommand()
                        .BpmnProcessId("Process_0ecmbkw")
                        .LatestVersion()
                        .Variables(JsonSerializer.Serialize(model))
                        .Send(token: cancellationToken);

                    ticket.Status = TicketStatus.InProcess;
                }

                if (db.ChangeTracker.HasChanges())
                    await db.SaveChangesAsync(cancellationToken);

                db.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }, cancellationToken);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private string GetFileLink(string bucket, string path, string storageName)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var minioFactory = scope.ServiceProvider.GetRequiredService<IMinioClientFactory>();
        var minio = minioFactory.CreateClient();
        return minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject($"{path}/{storageName}")
                .WithExpiry(7 * 24 * 3600))
            .GetAwaiter()
            .GetResult();
    }
}