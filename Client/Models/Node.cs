namespace SoftwareVSM.Client.Models;

public enum NodeType
{
    CustomerDiscovery,  // Generates Opportunity from market demand
    ProductManagement,  // Converts Opportunity → Feature
    Development,        // Converts Feature + Defect → Code (accrues TechDebt internally)
    Quality,            // Validates Code → ValidatedCode; detects Defects
    Operations,         // Deploys ValidatedCode → RunningSoftware
    Support,            // Routes Incident → Defect + Opportunity
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
