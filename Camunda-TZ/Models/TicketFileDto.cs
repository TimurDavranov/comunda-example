namespace Camunda_TZ.Models;

public class TicketFileDto
{
    public long Id { get; set; }
    
    public string Bucket { get; set; }
    
    public  string Path { get; set; }
    
    public string FileName { get; set; }
    
    public string StorageName { get; set; }
}