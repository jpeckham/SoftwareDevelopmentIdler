namespace SoftwareVSM.Tests;

using SoftwareVSM.Client.GameEngine;
using SoftwareVSM.Client.Models;
using System.Linq;
using Xunit;

public class EngineTests
{
    // Helper: build a minimal connected pipeline in a fresh engine.
    // CustomerDiscovery → ProductManagement → Development → Quality → Operations → Support (+ feedback loops)
    private static SimulationEngine BuildConnectedPipeline()
    {
        var engine = new SimulationEngine();
        var state  = engine.State;

        var discovery = NodeFactory.Create(NodeType.CustomerDiscovery, 0,    0);
        var pm        = NodeFactory.Create(NodeType.ProductManagement, 220,  0);
        var dev       = NodeFactory.Create(NodeType.Development,       440,  0);
        var quality   = NodeFactory.Create(NodeType.Quality,           660,  0);
        var ops       = NodeFactory.Create(NodeType.Operations,        880,  0);
        var support   = NodeFactory.Create(NodeType.Support,           1100, 0);

        state.Nodes.AddRange([discovery, pm, dev, quality, ops, support]);

        Connect(state, discovery, TokenType.Opportunity,    pm,      TokenType.Opportunity);
        Connect(state, pm,        TokenType.Feature,        dev,     TokenType.Feature);
        Connect(state, dev,       TokenType.Code,           quality, TokenType.Code);
        Connect(state, quality,   TokenType.ValidatedCode,  ops,     TokenType.ValidatedCode);
        Connect(state, quality,   TokenType.Defect,         dev,     TokenType.Defect);
        Connect(state, ops,       TokenType.Incident,       support, TokenType.Incident);
        Connect(state, support,   TokenType.Defect,         dev,     TokenType.Defect);

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
        var discovery = NodeFactory.Create(NodeType.CustomerDiscovery);
        Assert.Contains(discovery.OutputPorts, p => p.TokenType == TokenType.Opportunity);

        var dev = NodeFactory.Create(NodeType.Development);
        Assert.Contains(dev.InputPorts,  p => p.TokenType == TokenType.Feature);
        Assert.Contains(dev.InputPorts,  p => p.TokenType == TokenType.Defect);
        Assert.Contains(dev.OutputPorts, p => p.TokenType == TokenType.Code);

        var quality = NodeFactory.Create(NodeType.Quality);
        Assert.Contains(quality.InputPorts,  p => p.TokenType == TokenType.Code);
        Assert.Contains(quality.OutputPorts, p => p.TokenType == TokenType.ValidatedCode);
        Assert.Contains(quality.OutputPorts, p => p.TokenType == TokenType.Defect);

        var support = NodeFactory.Create(NodeType.Support);
        Assert.Contains(support.InputPorts,  p => p.TokenType == TokenType.Incident);
        Assert.Contains(support.OutputPorts, p => p.TokenType == TokenType.Defect);
        Assert.Contains(support.OutputPorts, p => p.TokenType == TokenType.Opportunity);
    }

    [Fact]
    public void Tick_With_No_Nodes_Does_Not_Crash()
    {
        var engine = new SimulationEngine();
        engine.ManualTick();  // must not throw
        Assert.Equal(0, engine.State.TotalDemandReceived);
    }

    [Fact]
    public void CustomerDiscovery_Generates_Demand_Each_Tick()
    {
        var engine = BuildConnectedPipeline();
        engine.ManualTick();
        Assert.True(engine.State.TotalDemandReceived > 0);
    }

    [Fact]
    public void Flow_Propagates_Through_Full_Pipeline()
    {
        var engine = BuildConnectedPipeline();
        // Pipeline has latency — run enough ticks for work to propagate
        for (int i = 0; i < 20; i++)
            engine.ManualTick();

        Assert.True(engine.State.TotalDemandReceived > 0, "CustomerDiscovery must generate Opportunity");
        Assert.True(engine.State.TechnicalDebt       > 0, "TechDebt must accumulate during development");
    }

    [Fact]
    public void Unconnected_Output_Tokens_Are_Silently_Dropped()
    {
        var engine    = new SimulationEngine();
        var discovery = NodeFactory.Create(NodeType.CustomerDiscovery, 0, 0);
        engine.State.Nodes.Add(discovery);

        engine.ManualTick();
        // CustomerDiscovery generates Opportunity but no PM is connected — tokens dropped
        Assert.Equal(0, discovery.Queue.Values.Sum());
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
        var engine    = new SimulationEngine();
        var discovery = NodeFactory.Create(NodeType.CustomerDiscovery, 0,   0);
        var pm        = NodeFactory.Create(NodeType.ProductManagement, 220, 0);
        engine.State.Nodes.AddRange([discovery, pm]);
        Connect(engine.State, discovery, TokenType.Opportunity, pm, TokenType.Opportunity);

        engine.ManualTick();

        // TotalDemandReceived is set before routing — check PM actually received tokens
        Assert.True(engine.State.TotalDemandReceived > 0, "CustomerDiscovery must generate Opportunity");
        Assert.True(pm.Queue.ContainsKey(TokenType.Opportunity) || pm.LastThroughput > 0,
            "PM must have received Opportunity tokens via the connection");
    }

    [Fact]
    public void Golden_Full_Pipeline_Core_Loops_Active()
    {
        // Core diagnostic: if this fails, the main simulation loop is broken.
        // Note: TotalRevenue is not asserted here — revenue requires RunningSoftware to reach
        // the environment actor via a connected sink node (not present in this test pipeline).
        var engine = BuildConnectedPipeline();

        for (int i = 0; i < 20; i++)
            engine.ManualTick();

        Assert.True(engine.State.TotalDemandReceived > 0,   "CustomerDiscovery must generate Opportunity");
        Assert.True(engine.State.TechnicalDebt       > 0,   "TechDebt must accumulate during development");
        Assert.True(engine.State.TotalBugsGenerated  > 0,   "Dev must generate bugs tracking quality issues");
    }

    [Fact]
    public void Quality_Detects_Defects_And_Routes_Back_To_Development()
    {
        var engine  = new SimulationEngine();
        var state   = engine.State;
        state.TechnicalDebt = 5000; // ensure some defect density

        var dev     = NodeFactory.Create(NodeType.Development, 0,   0);
        var quality = NodeFactory.Create(NodeType.Quality,     220, 0);
        state.Nodes.AddRange([dev, quality]);

        // Seed dev with features
        dev.Queue[TokenType.Feature] = 10.0;

        Connect(state, dev,     TokenType.Code,   quality, TokenType.Code);
        Connect(state, quality, TokenType.Defect, dev,     TokenType.Defect);

        engine.ManualTick(); // dev produces Code
        engine.ManualTick(); // quality processes Code, produces Defect + ValidatedCode

        Assert.True(state.TotalBugsGenerated > 0, "Quality should have detected defects");
        Assert.True(dev.Queue.ContainsKey(TokenType.Defect) && dev.Queue[TokenType.Defect] > 0,
            "Detected defects must be routed back to Development queue");
    }
}
