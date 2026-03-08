namespace SoftwareVSM.Client.Models;

public enum TokenType
{
    Opportunity,     // Market signal: customer demand for value
    Feature,         // Defined work item ready for development
    Code,            // Output of development work
    ValidatedCode,   // Code that has passed quality evaluation
    RunningSoftware, // Deployed, running software in production
    Defect,          // Quality issue requiring rework
    Incident,        // Production failure requiring ops response
}

public class Token
{
    public TokenType TokenType { get; set; }
    public double Quantity { get; set; }
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
}
