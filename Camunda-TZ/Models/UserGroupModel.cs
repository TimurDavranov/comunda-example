using System.Text.Json.Serialization;

namespace Camunda_TZ.Models;

public class UserGroupModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class StartProcessModel
{
    public long id { get; set; }
    public string textfield_username { get; set; }

    public string textfield_email { get; set; }

    public string textfield_title { get; set; }

    public string select_type { get; set; }

    public List<StartProcessAttachmentModel> attachments { get; set; }
}

public class StartProcessAttachmentModel
{
    public string fileUrl { get; set; }

    public string fileName { get; set; }
}