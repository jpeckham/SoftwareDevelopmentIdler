namespace SoftwareVSM.Client.Models;

public enum PracticeType
{
    Agile,
    TDD,
    ContinuousIntegration,
    ContinuousDelivery,
    DevOpsCulture
}

public class Practice
{
    public PracticeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public double AdoptionLevel { get; set; } = 0.0; // 0.0 to 1.0
}
