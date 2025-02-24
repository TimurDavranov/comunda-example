using Camunda_TZ.Entities;

namespace Camunda_TZ.Models;

public class TicketListDto
{
    public long Id { get; set; }
    
    public string ClientName { get; set; }
    
    public string ClientEmail { get; set; }
    
    public string Title { get; set; }
    
    public TicketType Type { get; set; }
    
    public TicketStatus Status { get; set; }
}