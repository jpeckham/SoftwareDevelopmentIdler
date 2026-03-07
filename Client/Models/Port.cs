namespace SoftwareVSM.Client.Models;

public enum PortDirection { Input, Output }

public class Port
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NodeId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public PortDirection Direction { get; set; }
    public TokenType TokenType { get; set; }
}
