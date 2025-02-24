using System.ComponentModel.DataAnnotations;
using Camunda_TZ.Entities;

namespace Camunda_TZ.Models;

public class TicketDto
{
    [Required]
    [MaxLength(100)]
    public string ClientName { get; set; }
    
    [Required]
    [EmailAddress]
    public string ClientEmail { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Title { get; set; }
    
    public TicketType Type { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Note { get; set; }
    
    public IList<TicketFileDto>? Attachments { get; set; }
    
    public TicketStatus Status { get; set; }
    
    public string? SupportNote { get; set; }
}