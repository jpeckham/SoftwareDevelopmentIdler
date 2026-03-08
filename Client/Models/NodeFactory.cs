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
        NodeType.CustomerDiscovery => "Customer Discovery",
        NodeType.ProductManagement => "Product Management",
        NodeType.Development       => "Development",
        NodeType.Quality           => "Quality",
        NodeType.Operations        => "Operations",
        NodeType.Support           => "Support",
        _ => type.ToString()
    };

    private static void ConfigurePorts(Node node)
    {
        switch (node.NodeType)
        {
            case NodeType.CustomerDiscovery:
                Out(node, TokenType.Opportunity,    "Opportunity");
                break;

            case NodeType.ProductManagement:
                In(node,  TokenType.Opportunity,    "Opportunity");
                Out(node, TokenType.Feature,        "Feature");
                break;

            case NodeType.Development:
                In(node,  TokenType.Feature,        "Feature");
                In(node,  TokenType.Defect,         "Defect");
                Out(node, TokenType.Code,           "Code");
                break;

            case NodeType.Quality:
                In(node,  TokenType.Code,           "Code");
                Out(node, TokenType.ValidatedCode,  "Validated Code");
                Out(node, TokenType.Defect,         "Defect");
                break;

            case NodeType.Operations:
                In(node,  TokenType.ValidatedCode,  "Validated Code");
                Out(node, TokenType.RunningSoftware,"Running Software");
                Out(node, TokenType.Incident,       "Incident");
                break;

            case NodeType.Support:
                In(node,  TokenType.Incident,       "Incident");
                Out(node, TokenType.Defect,         "Defect");
                Out(node, TokenType.Opportunity,    "Opportunity");
                break;
        }
    }

    private static void In(Node node, TokenType type, string label)
        => node.InputPorts.Add(new Port { NodeId = node.Id, TokenType = type, Label = label, Direction = PortDirection.Input });

    private static void Out(Node node, TokenType type, string label)
        => node.OutputPorts.Add(new Port { NodeId = node.Id, TokenType = type, Label = label, Direction = PortDirection.Output });
}
