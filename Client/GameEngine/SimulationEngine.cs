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
            case NodeType.CustomerDiscovery: ProcessCustomerDiscovery(node); break;
            case NodeType.ProductManagement: ProcessTransform(node, TokenType.Opportunity, TokenType.Feature, 1.0); break;
            case NodeType.Development:       ProcessDevelopment(node);       break;
            case NodeType.Quality:           ProcessQuality(node);           break;
            case NodeType.Operations:        ProcessOperations(node);        break;
            case NodeType.Support:           ProcessSupport(node);           break;
        }
    }

    // ─── Node processors ─────────────────────────────────────────────────────

    /// CustomerDiscovery: generates Opportunity from customer base each tick.
    private void ProcessCustomerDiscovery(Node node)
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

    private const double BaseDetectionRate = 0.70; // Quality catches 70% of defects by default

    /// Quality: validates Code → ValidatedCode + Defect (detected).
    /// Escaped defects become Incidents via Operations.
    private void ProcessQuality(Node node)
    {
        double code = Consume(node, TokenType.Code, BaseThroughput);
        if (code <= 0) return;

        double defectDensity   = State.TechnicalDebt / 50_000.0;
        double totalDefects    = code * defectDensity;
        double detectedDefects = totalDefects * BaseDetectionRate;
        double validatedCode   = code - detectedDefects;

        PushOutput(node, TokenType.ValidatedCode, Math.Max(0, validatedCode));
        if (detectedDefects > 0.0001)
            PushOutput(node, TokenType.Defect, detectedDefects);

        State.TotalBugsGenerated += totalDefects;
        node.LastThroughput = code;
    }

    private const double BaseEscapedDefectRate = 0.05; // 5% of deployed code causes incidents

    /// Operations: deploys ValidatedCode → RunningSoftware + Incidents from escaped defects.
    private void ProcessOperations(Node node)
    {
        double validated = Consume(node, TokenType.ValidatedCode, BaseThroughput);
        if (validated <= 0) return;

        double incidents = validated * BaseEscapedDefectRate * (1.0 + State.TechnicalDebt / 100_000.0);
        PushOutput(node, TokenType.RunningSoftware, validated);
        if (incidents > 0.0001)
            PushOutput(node, TokenType.Incident, incidents);

        node.LastThroughput = validated;
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

    private const double RevenueRate          = 2.5;   // revenue per RunningSoftware unit per customer
    private const double BugDiscoveryRate     = 0.05;  // user-found bugs per RunningSoftware unit
    private const double MarketExpansionRate  = 0.01;  // new opportunities per RunningSoftware unit

    private void UpdateGlobalMetrics()
    {
        // Salary burn
        State.Funds -= State.Employees.Sum(e => e.Salary) / 365.0;

        // Satisfaction-driven churn
        if (State.CustomerSatisfaction < 0.5)
            State.CustomerCount = Math.Max(0, (int)(State.CustomerCount - State.CustomerCount * 0.01));

        // Users as environment actor: RunningSoftware generates economic signals each tick
        double totalRunning = State.Nodes
            .Sum(n => n.Queue.TryGetValue(TokenType.RunningSoftware, out var q) ? q : 0);

        if (totalRunning > 0)
        {
            double revenue = totalRunning * State.CustomerCount * RevenueRate;
            State.Funds           += revenue;
            State.TotalRevenue    += revenue;
            State.TotalWorkDelivered += totalRunning;

            // User-discovered bugs feed Support queues
            double userBugs     = totalRunning * BugDiscoveryRate;
            var supportNodes    = State.Nodes.Where(n => n.NodeType == NodeType.Support).ToList();
            int supportCount    = Math.Max(1, supportNodes.Count);
            foreach (var support in supportNodes)
            {
                support.Queue.TryGetValue(TokenType.Incident, out double existing);
                support.Queue[TokenType.Incident] = existing + userBugs / supportCount;
            }
            State.TotalIncidents += userBugs;

            // Running software generates new feature opportunities back to CustomerDiscovery
            double newOpportunities   = totalRunning * MarketExpansionRate;
            var discoveryNodes        = State.Nodes.Where(n => n.NodeType == NodeType.CustomerDiscovery).ToList();
            int discoveryCount        = Math.Max(1, discoveryNodes.Count);
            foreach (var discovery in discoveryNodes)
            {
                discovery.Queue.TryGetValue(TokenType.Opportunity, out double existing);
                discovery.Queue[TokenType.Opportunity] = existing + newOpportunities / discoveryCount;
            }

            State.CustomerSatisfaction = Math.Min(1.0, State.CustomerSatisfaction + totalRunning * 0.001);

            // Consume RunningSoftware from node queues (it was sampled above)
            foreach (var n in State.Nodes)
                n.Queue.Remove(TokenType.RunningSoftware);
        }
    }
}
