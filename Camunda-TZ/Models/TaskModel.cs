using System.Text.Json.Serialization;

namespace Camunda_TZ.Models;

public class TaskModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("taskDefinitionId")]
    public string TaskDefinitionId { get; set; }

    [JsonPropertyName("processName")]
    public string ProcessName { get; set; }

    [JsonPropertyName("creationDate")]
    public string CreationDate { get; set; }

    [JsonPropertyName("completionDate")]
    public string CompletionDate { get; set; }

    [JsonPropertyName("assignee")]
    public string Assignee { get; set; }

    [JsonPropertyName("taskState")]
    public string TaskState { get; set; }

    [JsonPropertyName("sortValues")]
    public List<string> SortValues { get; set; }

    [JsonPropertyName("isFirst")]
    public bool? IsFirst { get; set; }

    [JsonPropertyName("formKey")]
    public string FormKey { get; set; }

    [JsonPropertyName("formId")]
    public string FormId { get; set; }

    [JsonPropertyName("formVersion")]
    public int? FormVersion { get; set; }

    [JsonPropertyName("isFormEmbedded")]
    public bool? IsFormEmbedded { get; set; }

    [JsonPropertyName("processDefinitionKey")]
    public string ProcessDefinitionKey { get; set; }

    [JsonPropertyName("processInstanceKey")]
    public string ProcessInstanceKey { get; set; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("followUpDate")]
    public DateTime? FollowUpDate { get; set; }

    [JsonPropertyName("candidateGroups")]
    public List<string> CandidateGroups { get; set; }

    [JsonPropertyName("candidateUsers")]
    public List<string> CandidateUsers { get; set; }

    [JsonPropertyName("variables")]
    public List<VariableModel> Variables { get; set; }

    [JsonPropertyName("context")]
    public string Context { get; set; }

    [JsonPropertyName("implementation")]
    public string Implementation { get; set; }

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
}

public class AssignmentModel
{
    [JsonPropertyName("success")]
    public bool Success {get;set;}
    
    [JsonPropertyName("status")]
    public string Status {get;set;}
    
    [JsonPropertyName("task")]
    public string Task {get;set;}
    
    [JsonPropertyName("selectedAssignee")]
    public string SelectedAssignee {get;set;}
    
    [JsonPropertyName("selectedAssigneeEmail")]
    public string SelectedAssigneeEmail {get;set;}
    
    [JsonPropertyName("smtpHost")]
    public string SmtpHost {get;set;}
    
    [JsonPropertyName("smtpPort")]
    public int SmtpPort {get;set;}
    
}