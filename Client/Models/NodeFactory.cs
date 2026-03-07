namespace SoftwareVSM.Client.Models;

public static class NodeFactory
{
    public static Node Create(NodeType type, double x = 0, double y = 0)
    {
        var node = new Node
        {
            NodeType = type,
            X = x,
            Y = y,
            Name = GetDisplayName(type)
        };
        ConfigurePorts(node);
        return node;
    }

    private static string GetDisplayName(NodeType type) => type switch
    {
        NodeType.Market            => "Market",
        NodeType.ProductManagement => "Product Management",
        NodeType.BusinessAnalysis  => "Business Analysis",
        NodeType.Development       => "Development",
        NodeType.Operations        => "Operations",
        NodeType.HostedCompute     => "Hosted Compute",
        NodeType.UserWorkstation   => "User Workstations",
        NodeType.Users             => "Users",
        NodeType.Support           => "Support",
        _ => type.ToString()
    };

    private static void ConfigurePorts(Node node)
    {
        switch (node.NodeType)
        {
            case NodeType.Market:
                Out(node, TokenType.Demand, "Demand");
                In(node,  TokenType.Dissatisfaction, "Dissatisfaction");
                break;

            case NodeType.ProductManagement:
                In(node,  TokenType.Demand,    "Demand");
                Out(node, TokenType.Features,  "Features");
                break;

            case NodeType.BusinessAnalysis:
                In(node,  TokenType.Features,    "Features");
                Out(node, TokenType.UserStories, "User Stories");
                break;

            case NodeType.Development:
                In(node,  TokenType.UserStories,   "User Stories");
                In(node,  TokenType.FailureDemand, "Failure Demand");
                Out(node, TokenType.Software,      "Software");
                break;

            case NodeType.Operations:
                In(node,  TokenType.Software,            "Software");
                Out(node, TokenType.DeployableArtifacts, "Deployable Artifacts");
                break;

            case NodeType.HostedCompute:
            case NodeType.UserWorkstation:
                In(node,  TokenType.DeployableArtifacts, "Deployable Artifacts");
                Out(node, TokenType.WorkProduct,         "Work Product");
                break;

            case NodeType.Users:
                In(node,  TokenType.WorkProduct, "Work Product");
                Out(node, TokenType.Incidents,   "Incidents");
                break;

            case NodeType.Support:
                In(node,  TokenType.Incidents,      "Incidents");
                Out(node, TokenType.FailureDemand,  "Failure Demand");
                Out(node, TokenType.Dissatisfaction,"Dissatisfaction");
                break;
        }
    }

    private static void In(Node node, TokenType type, string label)
        => node.InputPorts.Add(new Port { NodeId = node.Id, TokenType = type, Label = label, Direction = PortDirection.Input });

    private static void Out(Node node, TokenType type, string label)
        => node.OutputPorts.Add(new Port { NodeId = node.Id, TokenType = type, Label = label, Direction = PortDirection.Output });
}
