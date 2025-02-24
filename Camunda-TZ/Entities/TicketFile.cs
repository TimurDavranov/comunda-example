namespace Camunda_TZ.Entities;

public class TicketFile
{
    public long Id { get; set; }

    public string Bucket { get; set; }

    public string Path { get; set; }

    public string FileName { get; set; }

    public string StorageName { get; set; }
}