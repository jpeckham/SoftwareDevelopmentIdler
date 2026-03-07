namespace SoftwareVSM.Client.Models;

/// <summary>
/// Shared geometry constants for the node canvas.
/// Node cards are laid out as: [ports-left 20px] [content 160px] [ports-right 20px] = 200px total.
/// Port centres are calculated from node.X / node.Y so SVG lines match CSS port dots.
/// </summary>
public static class CanvasLayout
{
    public const double PortColWidth    = 20;   // left / right port column
    public const double ContentWidth    = 160;  // node card body
    public const double TotalWidth      = PortColWidth + ContentWidth + PortColWidth; // 200

    public const double HeaderHeight    = 56;   // node title + throughput row
    public const double PortRowHeight   = 30;   // height of one port row

    /// Canvas X of an input port dot centre (left edge of node).
    public static double InputPortX(Node node)  => node.X + PortColWidth / 2;

    /// Canvas Y of input/output port i.
    public static double PortY(Node node, int index)
        => node.Y + HeaderHeight + index * PortRowHeight + PortRowHeight / 2;

    /// Canvas X of an output port dot centre (right edge of node).
    public static double OutputPortX(Node node) => node.X + PortColWidth + ContentWidth + PortColWidth / 2;

    public static (double x, double y) InputPortCenter(Node node, int index)
        => (InputPortX(node), PortY(node, index));

    public static (double x, double y) OutputPortCenter(Node node, int index)
        => (OutputPortX(node), PortY(node, index));

    /// Token type → accent colour (used by both SVG paths and port dots).
    public static string TokenColor(TokenType type) => type switch
    {
        TokenType.Demand              => "#f59e0b",   // amber
        TokenType.Features            => "#3b82f6",   // blue
        TokenType.UserStories         => "#8b5cf6",   // violet
        TokenType.Software            => "#10b981",   // emerald
        TokenType.DeployableArtifacts => "#06b6d4",   // cyan
        TokenType.WorkProduct         => "#22c55e",   // green
        TokenType.Incidents           => "#ef4444",   // red
        TokenType.FailureDemand       => "#f97316",   // orange
        TokenType.Dissatisfaction     => "#ec4899",   // pink
        _ => "#94a3b8"
    };
}
