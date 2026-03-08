namespace SoftwareVSM.Client.GameEngine;

using SoftwareVSM.Client.Models;
using System;
using System.Linq;
using System.Timers;
using Timer = System.Timers.Timer;

/// <summary>
/// Connection-aware simulation engine. Flow follows the connections the player
/// draws between nodes rather than a hardcoded pipeline.
/// </summary>
public class SimulationEngine
{
    public GameState State { get; private set; }
    private readonly Timer _timer;

    public event Action? OnTick;

    private const double DemandPerCustomer  = 0.05;  // demand tokens per customer per tick
    private const double BaseThroughput     = 5.0;   // tokens per tick (no staff modifier yet)
    private const double BaseIncidentRate   = 0.15;  // incidents per work-product unit
    private const double RevenuePerWork     = 2.5;   // revenue per work-product unit per customer

    public SimulationEngine()
    {
        State = new GameState();
        _timer = new Timer(1000);
        _timer.Elapsed += (_, _) => Tick();
    }

    public void Start()  => _timer.Start();
    public void Stop()   => _timer.Stop();
    public void ManualTick() => Tick();

    public void LoadState(GameState state)
    {
        State = state;
        OnTick?.Invoke();
    }

    // ─── Main tick ───────────────────────────────────────────────────────────

    private void Tick()
    {
        try
        {
            // Snapshot the node list to avoid modification during iteration
            foreach (var node in State.Nodes.ToList())
                ProcessNode(node);

            UpdateGlobalMetrics();
            OnTick?.Invoke();
        }
        catch (Exception) { /* prevent timer crashes */ }
    }

    private void ProcessNode(Node node)
    {
        switch (node.NodeType)
        {
            case NodeType.Market:            ProcessMarket(node);       break;
            case NodeType.ProductManagement: ProcessTransform(node, TokenType.Opportunity, TokenType.Feature,        1.0); break;
            case NodeType.BusinessAnalysis:  ProcessTransform(node, TokenType.Feature,     TokenType.Feature,        1.0); break;
            case NodeType.Development:       ProcessDevelopment(node);  break;
            case NodeType.Operations:        ProcessTransform(node, TokenType.Code,         TokenType.ValidatedCode,  1.0); break;
            case NodeType.HostedCompute:
            case NodeType.UserWorkstation:   ProcessTransform(node, TokenType.ValidatedCode,TokenType.RunningSoftware,1.0); break;
            case NodeType.Users:             ProcessUsers(node);        break;
            case NodeType.Support:           ProcessSupport(node);      break;
        }
    }

    // ─── Node processors ─────────────────────────────────────────────────────

    /// Market: generates Opportunity tokens based on customer count.
    private void ProcessMarket(Node node)
    {
        double demand = State.CustomerCount * DemandPerCustomer;
        PushOutput(node, TokenType.Opportunity, demand);
        State.TotalDemandReceived += demand;
        node.LastThroughput = demand;
    }

    /// Generic 1-to-1 transform: consume inputType, produce outputType.
    private void ProcessTransform(Node node, TokenType input, TokenType output, double efficiency)
    {
        double consumed = Consume(node, input, BaseThroughput);
        if (consumed <= 0) return;
        PushOutput(node, output, consumed * efficiency);
        node.LastThroughput = consumed;
    }

    /// Development: Feature + Defect (rework) → Code + TechDebt.
    /// Technical debt slows throughput and worsens defect rate.
    private void ProcessDevelopment(Node node)
    {
        double debtPenalty = Math.Max(0.1, 1.0 - State.TechnicalDebt / 10_000.0);
        double capacity    = BaseThroughput * debtPenalty;

        double features = Consume(node, TokenType.Feature, capacity);
        double rework   = Consume(node, TokenType.Defect,  capacity * 0.5);
        double produced = features + rework;

        if (produced <= 0) return;

        double techDebtAccrued = produced * 0.05;
        State.TechnicalDebt += techDebtAccrued;
        PushOutput(node, TokenType.Code,     produced);
        PushOutput(node, TokenType.TechDebt, techDebtAccrued);
        State.TotalBugsGenerated += produced * 0.1;
        node.LastThroughput = produced;
    }

    /// Users: RunningSoftware → Revenue + Incidents.
    /// Incidents rate rises with TechnicalDebt (proxy for quality).
    private void ProcessUsers(Node node)
    {
        double work = ConsumeAll(node, TokenType.RunningSoftware);
        if (work <= 0) return;

        double revenue = work * State.CustomerCount * RevenuePerWork;
        State.Funds           += revenue;
        State.TotalRevenue    += revenue;
        State.TotalWorkDelivered += work;

        double incidentRate = BaseIncidentRate + State.TechnicalDebt / 50_000.0;
        double incidents    = work * incidentRate;
        PushOutput(node, TokenType.Incident, incidents);
        State.TotalIncidents += incidents;

        State.CustomerSatisfaction = Math.Min(1.0, State.CustomerSatisfaction + work * 0.001);
        node.LastThroughput = work;
    }

    /// Support: Incident → Defect (back to dev) + Opportunity (recovered demand).
    private void ProcessSupport(Node node)
    {
        double resolved = Consume(node, TokenType.Incident, BaseThroughput * 2);
        if (resolved <= 0) return;

        PushOutput(node, TokenType.Defect,      resolved * 0.6);
        PushOutput(node, TokenType.Opportunity, resolved * 0.2);

        State.CustomerSatisfaction -= resolved * 0.002;
        State.CustomerSatisfaction  = Math.Max(0.1, State.CustomerSatisfaction);
        node.LastThroughput = resolved;
    }

    // ─── Queue helpers ────────────────────────────────────────────────────────

    private double Consume(Node node, TokenType type, double max)
    {
        if (!node.Queue.TryGetValue(type, out double available) || available <= 0) return 0;
        double consumed = Math.Min(max, available);
        node.Queue[type] = available - consumed;
        if (node.Queue[type] < 0.0001) node.Queue.Remove(type);
        return consumed;
    }

    private double ConsumeAll(Node node, TokenType type)
        => Consume(node, type, double.MaxValue);

    /// Push tokens from a source node along all outgoing connections of the given type.
    private void PushOutput(Node source, TokenType type, double amount)
    {
        if (amount <= 0.0001) return;

        var targets = State.Connections
            .Where(c => c.SourceNodeId == source.Id && c.TokenType == type)
            .Select(c => State.Nodes.FirstOrDefault(n => n.Id == c.TargetNodeId))
            .Where(n => n != null)
            .ToList();

        if (targets.Count == 0) return; // no connection → tokens lost (useful backpressure signal)

        double share = amount / targets.Count;
        foreach (var target in targets!)
        {
            target!.Queue.TryGetValue(type, out double existing);
            target.Queue[type] = existing + share;
        }
    }

    // ─── Global metrics ───────────────────────────────────────────────────────

    private void UpdateGlobalMetrics()
    {
        // Salary burn
        State.Funds -= State.Employees.Sum(e => e.Salary) / 365.0;

        // Satisfaction-driven churn
        if (State.CustomerSatisfaction < 0.5)
            State.CustomerCount = Math.Max(0, (int)(State.CustomerCount - State.CustomerCount * 0.01));
    }
}
