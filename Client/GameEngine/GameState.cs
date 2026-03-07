namespace SoftwareVSM.Client.GameEngine;

using SoftwareVSM.Client.Models;

public class GameState
{
    public double Funds { get; set; } = 100000;
    public double TechnicalDebt { get; set; } = 0.0;
    public double CustomerSatisfaction { get; set; } = 1.0;

    public int CustomerCount { get; set; } = 100;
    
    public List<Node> Nodes { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public List<Practice> Practices { get; set; } = new();

    public double TotalDemandReceived { get; set; }
    public double TotalFeaturesDelivered { get; set; }
    public double TotalBugsGenerated { get; set; }
    public double TotalIncidents { get; set; }
}
