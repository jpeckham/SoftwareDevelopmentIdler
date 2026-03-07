namespace SoftwareVSM.Client.Models;

public enum TokenType
{
    Demand,
    Vision,
    DetailedDemand,
    Solutions,
    ValidatedSolutions,
    OperationalSolutions,
    FailureDemand,
    Revenue,
    ChurnRisk,
    ResolvedIssues,
    OperationalIncidents
}

public class Token
{
    public TokenType TokenType { get; set; }
    public double Quantity { get; set; }
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
}
