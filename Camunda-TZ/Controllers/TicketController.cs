using Camunda_TZ.Entities;
using Camunda_TZ.Models;
using Camunda_TZ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace Camunda_TZ.Controllers;

[Authorize]
public class TicketController(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IMinioClientFactory minioClientFactory) : Controller
{
    public async Task<IActionResult> Index()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var result = await db.Tickets
            .AsNoTracking()
            .Select(s => new TicketListDto()
            {
                Id = s.Id,
                Title = s.Title,
                Status = s.Status,
                Type = s.Type,
                ClientEmail = s.ClientEmail,
                ClientName = s.ClientName
            })
            .ToListAsync();

        return View(result);
    }

    public async Task<IActionResult> View(long id)
    {
        if (id == 0)
            return RedirectToAction(actionName: "Index", controllerName: "Ticket");
        
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var ticket = await db.Tickets
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new TicketDto()
            {
                Title = s.Title,
                Type = s.Type,
                ClientEmail = s.ClientEmail,
                ClientName = s.ClientName,
                Attachments = s.Attachments.Select(a => new TicketFileDto()
                {
                    FileName = a.FileName,
                    Bucket = a.Bucket,
                    Path = a.Path,
                    StorageName = a.StorageName
                }).ToList(),
                Note = s.Note,
                Status = s.Status,
                SupportNote = s.SupportNote
            })
            .FirstOrDefaultAsync();
        
        if(ticket is null)
            return RedirectToAction(actionName: "Index", controllerName: "Ticket");
        
        return View(ticket);
    }

    [HttpGet]
    public async Task<IActionResult> Form()
    {
        return View(new TicketDto());
    }

    private const string Bucket = "tickets";
    private const string Path = "attachments";
    private const string ContentType = "application/octet-stream";
    private const int MaxFileLength = 5 * 1024 * 1024;

    private static readonly IList<string> AcceptedFileExtensions =
        ["jpg", "png", "gif", "pdf", "txt", "doc", "docx", "xls", "xlsx"];

    [HttpPost]
    public async Task<IActionResult> Form([FromForm]TicketDto ticket)
    {
        if (!ModelState.IsValid)
            return View(ticket);

        await using var db = await dbContextFactory.CreateDbContextAsync();

        await db.Tickets.AddAsync(new Ticket()
        {
            ClientName = ticket.ClientName,
            ClientEmail = ticket.ClientEmail,
            Title = ticket.Title,
            Type = ticket.Type,
            Note = ticket.Note,
            Attachments = ticket.Attachments?
                .Where(s=>s.Id == 0)
                .Select(s => new TicketFile()
            {
                FileName = s.FileName,
                Bucket = s.Bucket,
                StorageName = s.StorageName,
                Path = s.Path
            }).ToList() ?? [],
            SupportNote = string.Empty
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return RedirectToAction(actionName: "Index", controllerName: "Ticket");
    }

    [HttpGet]
    public async Task<IActionResult> Download(
        string bucket,
        string path,
        string storageName,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var minio = minioClientFactory.CreateClient();

        var ms = new MemoryStream();

        await minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject($"{path}/{storageName}")
            .WithCallbackStream(cb => cb.CopyTo(ms)), cancellationToken);

        ms.Position = 0;

        return File(ms, ContentType, fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        using var minio = minioClientFactory.CreateClient();

        await CheckMinioBucket(minio, Bucket);
        var ext = System.IO.Path.GetExtension(file.FileName);
        var storageName = Guid.NewGuid() + (ext.StartsWith('.') ? ext : $".{ext}");
        
        await using var stream = file.OpenReadStream();
        
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(Bucket)
            .WithObjectSize(file.Length)
            .WithObject($"{Path}/{storageName}")
            .WithStreamData(stream)
            .WithContentType(ContentType));

        return Json(new TicketFileDto()
        {
            StorageName = storageName,
            Path = Path,
            FileName = file.FileName,
            Bucket = Bucket
        });
    }

    private static async Task CheckMinioBucket(IMinioClient client, string bucket)
    {
        var isExist = await client.BucketExistsAsync(new BucketExistsArgs()
            .WithBucket(bucket));

        if (isExist)
            return;

        await client.MakeBucketAsync(new MakeBucketArgs()
            .WithBucket(bucket));
    }
}