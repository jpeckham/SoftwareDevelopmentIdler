namespace SoftwareVSM.Client.Models;

public enum NodeType
{
    Customers,
    ProductLeadership,
    BusinessAnalyst,
    Developers,
    QA,
    Operations,
    Support
}

public class Node
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NodeType NodeType { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public double X { get; set; } = 0.0;
    public double Y { get; set; } = 0.0;
    
    public List<TokenType> InputTokenTypes { get; set; } = new();
    public List<TokenType> OutputTokenTypes { get; set; } = new();

    public Dictionary<TokenType, double> Queue { get; set; } = new();

    public double CycleTime { get; set; } = 1.0;
    public double ErrorRate { get; set; } = 0.0;
    
    public List<string> StaffAssigned { get; set; } = new();

    public double LastThroughput { get; set; }
}
