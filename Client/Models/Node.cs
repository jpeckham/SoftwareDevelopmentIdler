namespace SoftwareVSM.Client.Models;

public enum NodeType
{
    Market,
    ProductManagement,
    BusinessAnalysis,
    Development,
    Operations,
    HostedCompute,
    UserWorkstation,
    Users,
    Support,
}

public class Node
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NodeType NodeType { get; set; }
    public string Name { get; set; } = string.Empty;

    public double X { get; set; } = 0.0;
    public double Y { get; set; } = 0.0;

    public List<Port> InputPorts { get; set; } = new();
    public List<Port> OutputPorts { get; set; } = new();

    public Dictionary<TokenType, double> Queue { get; set; } = new();

    public double LastThroughput { get; set; }
}
