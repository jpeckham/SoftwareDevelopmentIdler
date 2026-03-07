namespace SoftwareVSM.Client.Models;

public enum EmployeeRole
{
    ProductManager,
    BusinessAnalyst,
    Developer,
    QAEngineer,
    OperationsEngineer,
    SupportAnalyst
}

public class Employee
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public EmployeeRole Role { get; set; }
    public double SkillLevel { get; set; } = 1.0;
    public double Salary { get; set; } = 100000;
    public double Productivity { get; set; } = 1.0;
    public double ErrorRate { get; set; } = 0.05;
    public double PracticeKnowledge { get; set; } = 0.0;

    public string AssignedNodeId { get; set; } = string.Empty;
    public string Name { get; set; } = "New Employee";
}
