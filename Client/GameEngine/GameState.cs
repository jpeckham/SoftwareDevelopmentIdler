namespace SoftwareVSM.Client.GameEngine;

using SoftwareVSM.Client.Models;

public enum InfrastructureTier
{
    OnPrem      = 0,  // ×1.0 Operations throughput
    CloudVM     = 1,  // ×1.5
    Autoscaling = 2,  // ×2.5
    EdgeNetwork = 3,  // ×4.0
}

public class GameState
{
    public double Funds { get; set; } = 500000;
    public double TechnicalDebt { get; set; } = 0.0;
    public double CustomerSatisfaction { get; set; } = 1.0;

    public int CustomerCount { get; set; } = 1000;
    public int TotalAddressableMarket { get; set; } = 10000;
    public double ProductCapability { get; set; } = 0.0;

    public InfrastructureTier Infrastructure { get; set; } = InfrastructureTier.OnPrem;

    public List<Node> Nodes { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public List<Practice> Practices { get; set; } = new();

    public double TotalDemandReceived { get; set; }
    public double TotalWorkDelivered { get; set; }
    public double TotalBugsGenerated { get; set; }
    public double TotalIncidents { get; set; }
    public double TotalRevenue { get; set; }
}
