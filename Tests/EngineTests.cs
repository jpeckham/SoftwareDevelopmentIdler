namespace SoftwareVSM.Tests;

using SoftwareVSM.Client.GameEngine;
using SoftwareVSM.Client.Models;
using System.Linq;
using Xunit;

public class EngineTests
{
    [Fact]
    public void Engine_Initializes_With_Correct_DefaultState()
    {
        var engine = new SimulationEngine();
        Assert.NotNull(engine.State);
        Assert.Equal(500000, engine.State.Funds);
        Assert.Equal(7, engine.State.Nodes.Count);
        Assert.Contains(engine.State.Nodes, n => n.NodeType == NodeType.Customers);
    }

    [Fact]
    public void Tick_Generates_Demand_And_Flows()
    {
        var engine = new SimulationEngine();
        var prodNode = engine.State.Nodes.First(n => n.NodeType == NodeType.ProductLeadership);
        
        // Assert initial demand is 0
        bool hasDemand = prodNode.Queue.TryGetValue(TokenType.Demand, out double demand);
        Assert.False(hasDemand && demand > 0);

        engine.ManualTick();

        // After one tick, Customer node generated demand to Product node
        prodNode.Queue.TryGetValue(TokenType.Demand, out demand);
        Assert.True(demand > 0);
        Assert.True(engine.State.TotalDemandReceived > 0);
    }

    [Fact]
    public void Tick_Reduces_Funds_Due_To_Salaries()
    {
        var engine = new SimulationEngine();
        double initialFunds = engine.State.Funds;
        
        engine.ManualTick();
        
        // Salary should deduct, revenue should add. We verify Funds changed.
        Assert.NotEqual(initialFunds, engine.State.Funds);
    }

    [Fact]
    public void GameState_Holds_Connections_And_Coordinates()
    {
        var state = new GameState();
        var node = new Node { X = 100, Y = 200 };
        state.Nodes.Add(node);
        state.Connections.Add(new Connection { SourceId = "1", TargetId = "2" });
        
        Assert.Equal(100, node.X);
        Assert.Single(state.Connections);
    }
}
