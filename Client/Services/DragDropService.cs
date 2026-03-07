namespace SoftwareVSM.Client.Services;

using SoftwareVSM.Client.Models;
using System;

public class DragDropService
{
    public Employee? DraggedEmployee { get; set; }

    public event Action? OnStateChange;

    public void StartDrag(Employee employee)
    {
        DraggedEmployee = employee;
        NotifyStateChanged();
    }

    public void Drop(string targetNodeId)
    {
        if (DraggedEmployee != null)
        {
            DraggedEmployee.AssignedNodeId = targetNodeId;
            DraggedEmployee = null;
            NotifyStateChanged();
        }
    }

    public void CancelDrag()
    {
        DraggedEmployee = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChange?.Invoke();
}
