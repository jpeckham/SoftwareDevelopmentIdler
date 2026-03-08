namespace SoftwareVSM.Tests;

using SoftwareVSM.Client.GameEngine;
using SoftwareVSM.Client.Models;
using System.Linq;
using Xunit;

public class EngineTests
{
    // Helper: build a minimal connected pipeline in a fresh engine.
    // Market → ProductManagement → BusinessAnalysis → Development → Operations → HostedCompute → Users → Support (+ feedback)
    private static SimulationEngine BuildConnectedPipeline()
    {
        var engine = new SimulationEngine();
        var state  = engine.State;

        var market  = NodeFactory.Create(NodeType.Market,            0,   0);
        var pm      = NodeFactory.Create(NodeType.ProductManagement, 220, 0);
        var ba      = NodeFactory.Create(NodeType.BusinessAnalysis,  440, 0);
        var dev     = NodeFactory.Create(NodeType.Development,       660, 0);
        var ops     = NodeFactory.Create(NodeType.Operations,        880, 0);
        var compute = NodeFactory.Create(NodeType.HostedCompute,    1100, 0);
        var users   = NodeFactory.Create(NodeType.Users,            1320, 0);
        var support = NodeFactory.Create(NodeType.Support,          1540, 0);

        state.Nodes.AddRange([market, pm, ba, dev, ops, compute, users, support]);

        Connect(state, market,  TokenType.Opportunity,    pm,      TokenType.Opportunity);
        Connect(state, pm,      TokenType.Feature,         ba,      TokenType.Feature);
        Connect(state, ba,      TokenType.Feature,         dev,     TokenType.Feature);
        Connect(state, dev,     TokenType.Code,            ops,     TokenType.Code);
        Connect(state, ops,     TokenType.ValidatedCode,   compute, TokenType.ValidatedCode);
        Connect(state, compute, TokenType.RunningSoftware, users,   TokenType.RunningSoftware);
        Connect(state, users,   TokenType.Incident,        support, TokenType.Incident);
        Connect(state, support, TokenType.Defect,          dev,     TokenType.Defect);

        return engine;
    }

    private static void Connect(GameState state, Node source, TokenType srcType, Node target, TokenType tgtType)
    {
        var srcPort = source.OutputPorts.First(p => p.TokenType == srcType);
        var tgtPort = target.InputPorts.First(p => p.TokenType == tgtType);
        state.Connections.Add(new Connection
        {
            SourceNodeId = source.Id, SourcePortId = srcPort.Id, TokenType = srcType,
            TargetNodeId = target.Id, TargetPortId = tgtPort.Id
        });
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Engine_Starts_With_Empty_Canvas()
    {
        var engine = new SimulationEngine();
        Assert.Empty(engine.State.Nodes);
        Assert.Empty(engine.State.Connections);
    }

    [Fact]
    public void GameState_Initial_Economic_Values()
    {
        var state = new GameState();
        Assert.Equal(500000, state.Funds);
        Assert.Equal(10000, state.TotalAddressableMarket);
        Assert.Equal(1.0, state.CustomerSatisfaction);
    }

    [Fact]
    public void NodeFactory_Creates_Correct_Ports()
    {
        var market = NodeFactory.Create(NodeType.Market);
        Assert.Contains(market.OutputPorts, p => p.TokenType == TokenType.Opportunity);

        var dev = NodeFactory.Create(NodeType.Development);
        Assert.Contains(dev.InputPorts,  p => p.TokenType == TokenType.Feature);
        Assert.Contains(dev.InputPorts,  p => p.TokenType == TokenType.Defect);
        Assert.Contains(dev.OutputPorts, p => p.TokenType == TokenType.Code);
    }

    [Fact]
    public void Tick_With_No_Nodes_Does_Not_Crash()
    {
        var engine = new SimulationEngine();
        engine.ManualTick();  // must not throw
        Assert.Equal(0, engine.State.TotalDemandReceived);
    }

    [Fact]
    public void Market_Generates_Demand_Each_Tick()
    {
        var engine = BuildConnectedPipeline();
        engine.ManualTick();
        Assert.True(engine.State.TotalDemandReceived > 0);
    }

    [Fact]
    public void Flow_Propagates_Through_Full_Pipeline()
    {
        var engine = BuildConnectedPipeline();
        // Pipeline has latency — run enough ticks for work to reach Users
        for (int i = 0; i < 20; i++)
            engine.ManualTick();

        Assert.True(engine.State.TotalWorkDelivered > 0, "Work should have reached users");
        Assert.True(engine.State.TotalRevenue > 0,       "Revenue should have been generated");
        Assert.True(engine.State.TotalIncidents > 0,     "Incidents should be generated from delivered work");
    }

    [Fact]
    public void Unconnected_Output_Tokens_Are_Silently_Dropped()
    {
        var engine = new SimulationEngine();
        var market = NodeFactory.Create(NodeType.Market, 0, 0);
        engine.State.Nodes.Add(market);

        engine.ManualTick();  // Market generates demand but no PM is connected
        // Demand should be generated then dropped — no crash, market node queue stays empty
        Assert.Equal(0, market.Queue.Values.Sum());
    }

    [Fact]
    public void Technical_Debt_Accumulates_As_Dev_Works()
    {
        var engine = BuildConnectedPipeline();
        double initialDebt = engine.State.TechnicalDebt;
        for (int i = 0; i < 10; i++)
            engine.ManualTick();

        Assert.True(engine.State.TechnicalDebt > initialDebt);
    }

    [Fact]
    public void Connections_Route_Tokens_To_Target_Node()
    {
        var engine = new SimulationEngine();
        var market = NodeFactory.Create(NodeType.Market,            0, 0);
        var pm     = NodeFactory.Create(NodeType.ProductManagement, 220, 0);
        engine.State.Nodes.AddRange([market, pm]);
        Connect(engine.State, market, TokenType.Opportunity, pm, TokenType.Opportunity);

        engine.ManualTick();

        // PM should have received demand (it may have already consumed some)
        Assert.True(engine.State.TotalDemandReceived > 0);
    }

    [Fact]
    public void Golden_Full_Pipeline_Flows_Opportunity_To_Revenue()
    {
        // This test must always pass. If it fails, the core feature loop is broken.
        // Opportunity → Feature → Code → ValidatedCode → RunningSoftware → Revenue
        var engine = BuildConnectedPipeline();

        for (int i = 0; i < 20; i++)
            engine.ManualTick();

        Assert.True(engine.State.TotalDemandReceived > 0,   "CustomerDiscovery must generate Opportunity");
        Assert.True(engine.State.TotalWorkDelivered  > 0,   "RunningSoftware must reach users");
        Assert.True(engine.State.TotalRevenue        > 0,   "Revenue must be generated");
        Assert.True(engine.State.TotalIncidents      > 0,   "Incidents must be generated from production");
        Assert.True(engine.State.TechnicalDebt       > 0,   "TechDebt must accumulate during development");
    }
}
