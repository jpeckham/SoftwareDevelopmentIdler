namespace SoftwareVSM.Client.Services;

using SoftwareVSM.Client.GameEngine;
using SoftwareVSM.Client.Models;
using System;
using System.Linq;

/// <summary>
/// Manages all canvas interaction state: toolbox drag, node repositioning,
/// and connection drawing between ports.
/// </summary>
public class CanvasDragService
{
    // Canvas origin — updated by GameCanvas on mount so child nodes can convert coords.
    public double CanvasLeft { get; set; }
    public double CanvasTop  { get; set; }

    public (double x, double y) ToCanvas(double clientX, double clientY)
        => (clientX - CanvasLeft, clientY - CanvasTop);

    // ── Toolbox drag ─────────────────────────────────────────────────────────
    public NodeType? ToolboxDragType { get; private set; }
    public void BeginToolboxDrag(NodeType type) { ToolboxDragType = type; }
    public void EndToolboxDrag()                { ToolboxDragType = null; }

    // ── Node repositioning ───────────────────────────────────────────────────
    public string? DraggingNodeId { get; private set; }
    private double _dragOffsetX, _dragOffsetY;

    public bool IsDraggingNode => DraggingNodeId != null;

    public void BeginNodeDrag(string nodeId, double canvasMouseX, double canvasMouseY, double nodeX, double nodeY)
    {
        DraggingNodeId = nodeId;
        _dragOffsetX   = canvasMouseX - nodeX;
        _dragOffsetY   = canvasMouseY - nodeY;
        NotifyChange();
    }

    public void UpdateNodePosition(Node node, double canvasMouseX, double canvasMouseY)
    {
        node.X = Math.Max(0, canvasMouseX - _dragOffsetX);
        node.Y = Math.Max(0, canvasMouseY - _dragOffsetY);
        NotifyChange();
    }

    public void EndNodeDrag() { DraggingNodeId = null; NotifyChange(); }

    // ── Connection drawing ───────────────────────────────────────────────────
    public bool      IsConnecting      { get; private set; }
    public string?   SourceNodeId      { get; private set; }
    public string?   SourcePortId      { get; private set; }
    public TokenType?ConnectingType    { get; private set; }
    public double    ConnectStartX     { get; private set; }
    public double    ConnectStartY     { get; private set; }
    public double    ConnectEndX       { get; private set; }
    public double    ConnectEndY       { get; private set; }

    public void BeginConnection(string nodeId, string portId, TokenType type, double startX, double startY)
    {
        IsConnecting   = true;
        SourceNodeId   = nodeId;
        SourcePortId   = portId;
        ConnectingType = type;
        ConnectStartX  = ConnectEndX = startX;
        ConnectStartY  = ConnectEndY = startY;
        NotifyChange();
    }

    public void UpdateConnectionEnd(double x, double y)
    {
        ConnectEndX = x;
        ConnectEndY = y;
        NotifyChange();
    }

    /// <summary>
    /// Attempt to complete a connection onto <paramref name="targetPortId"/>.
    /// Returns true if a connection was made.
    /// </summary>
    public bool TryCompleteConnection(GameState state, string targetNodeId, string targetPortId, TokenType targetType)
    {
        bool ok = IsConnecting
               && ConnectingType == targetType
               && SourceNodeId != targetNodeId   // no self-loops on same node
               && !state.Connections.Any(c => c.SourcePortId == SourcePortId && c.TargetPortId == targetPortId);

        if (ok)
        {
            state.Connections.Add(new Connection
            {
                SourceNodeId = SourceNodeId!,
                SourcePortId = SourcePortId!,
                TargetNodeId = targetNodeId,
                TargetPortId = targetPortId,
                TokenType    = targetType
            });
        }

        CancelConnection();
        return ok;
    }

    public void CancelConnection()
    {
        IsConnecting   = false;
        SourceNodeId   = null;
        SourcePortId   = null;
        ConnectingType = null;
        NotifyChange();
    }

    public event Action? OnChange;
    private void NotifyChange() => OnChange?.Invoke();
}
