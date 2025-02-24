namespace Camunda_TZ.Entities;

public class Ticket
{
    public long Id { get; set; }
    
    public string ClientName { get; set; }
    
    public string ClientEmail { get; set; }
    
    public string Title { get; set; }
    
    public TicketType Type { get; set; }
    
    public string Note { get; set; }
    
    public IList<TicketFile> Attachments { get; set; }
    
    public string? Assignee { get; set; }
    
    public string SupportNote { get; set; }
    
    public TicketStatus Status { get; set; }
}