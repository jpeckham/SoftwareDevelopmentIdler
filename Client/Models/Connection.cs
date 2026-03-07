namespace SoftwareVSM.Client.Models;

public class Connection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}
