namespace SoftwareVSM.Client.Models;

public enum TokenType
{
    Demand,              // Market → Product Management
    Features,            // Product Management → Business Analysis (product features to build)
    UserStories,         // Business Analysis → Development
    Software,            // Development → Operations
    DeployableArtifacts, // Operations → HostedCompute / UserWorkstation
    WorkProduct,         // HostedCompute / UserWorkstation → Users
    Incidents,           // Users → Support (quality issues)
    FailureDemand,       // Support → Development (rework)
    Dissatisfaction,     // Support → Market (shrinks market)
}

public class Token
{
    public TokenType TokenType { get; set; }
    public double Quantity { get; set; }
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
}
