# VSM Domain Evolution Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Evolve the simulation's domain language from vague workflow stages to a realistic software value stream with meaningful feedback loops, without changing the engine architecture.

**Architecture:** Three sequential phases — token rename (pure semantics), node restructure (new logic), capacity modifiers (gameplay depth). Each phase compiles and passes all tests before the next begins. The engine's tick loop, connection-based routing, and queue mechanics are preserved throughout.

**Tech Stack:** .NET 10, C# 13, Blazor WebAssembly, xUnit 2.9.3. Test command: `dotnet test` from repo root.

---

## Phase 1 — Token Normalization

> Pure rename. No engine logic changes. After this phase the game runs identically but speaks the right domain language.

### Task 1: Update TokenType enum

**Files:**
- Modify: `Client/Models/Token.cs`

**Step 1: Replace the enum body**

Old enum values → new values:
- `Demand` → `Opportunity`
- `Features` → `Feature`
- `UserStories` → `Feature` *(merge — UserStories is now just Feature)*
- `Software` → `Code`
- `DeployableArtifacts` → `ValidatedCode`
- `WorkProduct` → `RunningSoftware`
- `Incidents` → `Incident`
- `FailureDemand` → `Defect`
- `Dissatisfaction` → *(remove — Support will feed Defect directly)*
- *(add)* `TechDebt`
- *(add)* `Revenue`

Replace the entire enum in `Client/Models/Token.cs`:

```csharp
public enum TokenType
{
    Opportunity,     // Market signal: customer demand for value
    Feature,         // Defined work item ready for development
    Code,            // Output of development work
    ValidatedCode,   // Code that has passed quality evaluation
    RunningSoftware, // Deployed, running software in production
    Defect,          // Quality issue requiring rework
    Incident,        // Production failure requiring ops response
    TechDebt,        // Accumulated shortcuts slowing future work
    Revenue,         // Economic output from running software
}
```

**Step 2: Run the build to see all compile errors**

```bash
dotnet build
```

Expected: many CS0117 errors listing every usage of old enum values. These are your work list for Tasks 2–5.

**Step 3: Commit the enum change alone**

```bash
git add Client/Models/Token.cs
git commit -m "refactor: rename TokenType enum to canonical software domain vocabulary"
```

---

### Task 2: Update NodeFactory to use new token types

**Files:**
- Modify: `Client/Models/NodeFactory.cs`

**Step 1: Replace ConfigurePorts switch body**

The factory configures ports on nodes. Update every `TokenType.*` reference to use new names. The node types themselves don't change yet — that's Phase 2.

Replace the `ConfigurePorts` method body:

```csharp
private static void ConfigurePorts(Node node)
{
    switch (node.NodeType)
    {
        case NodeType.Market:
            Out(node, TokenType.Opportunity, "Opportunity");
            break;

        case NodeType.ProductManagement:
            In(node,  TokenType.Opportunity, "Opportunity");
            Out(node, TokenType.Feature,     "Feature");
            break;

        case NodeType.BusinessAnalysis:
            In(node,  TokenType.Feature, "Feature");
            Out(node, TokenType.Feature, "Feature");
            break;

        case NodeType.Development:
            In(node,  TokenType.Feature,  "Feature");
            In(node,  TokenType.Defect,   "Defect");
            Out(node, TokenType.Code,     "Code");
            Out(node, TokenType.TechDebt, "Tech Debt");
            break;

        case NodeType.Operations:
            In(node,  TokenType.Code,            "Code");
            Out(node, TokenType.ValidatedCode,   "Validated Code");
            break;

        case NodeType.HostedCompute:
        case NodeType.UserWorkstation:
            In(node,  TokenType.ValidatedCode,   "Validated Code");
            Out(node, TokenType.RunningSoftware, "Running Software");
            break;

        case NodeType.Users:
            In(node,  TokenType.RunningSoftware, "Running Software");
            Out(node, TokenType.Incident,        "Incident");
            break;

        case NodeType.Support:
            In(node,  TokenType.Incident,    "Incident");
            Out(node, TokenType.Defect,      "Defect");
            Out(node, TokenType.Opportunity, "Opportunity");
            break;
    }
}
```

Also update `GetDisplayName` — Market stays "Market" for now since node rename is Phase 2.

**Step 2: Run build**

```bash
dotnet build
```

Expected: NodeFactory errors resolved. Other files still failing.

**Step 3: Commit**

```bash
git add Client/Models/NodeFactory.cs
git commit -m "refactor: update NodeFactory ports to use new token taxonomy"
```

---

### Task 3: Update SimulationEngine to use new token types

**Files:**
- Modify: `Client/GameEngine/SimulationEngine.cs`

**Step 1: Update ProcessNode switch**

Replace the `ProcessNode` switch in `SimulationEngine.cs`:

```csharp
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
```

**Step 2: Update ProcessMarket**

```csharp
private void ProcessMarket(Node node)
{
    double demand = State.CustomerCount * DemandPerCustomer;
    PushOutput(node, TokenType.Opportunity, demand);
    State.TotalDemandReceived += demand;
    node.LastThroughput = demand;
}
```

Note: `Dissatisfaction` input removed — churn is handled by `UpdateGlobalMetrics` in Phase 2.

**Step 3: Update ProcessDevelopment**

```csharp
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
```

**Step 4: Update ProcessUsers**

```csharp
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
```

**Step 5: Update ProcessSupport**

```csharp
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
```

**Step 6: Run build**

```bash
dotnet build
```

Expected: clean build (or only test file errors remaining).

**Step 7: Commit**

```bash
git add Client/GameEngine/SimulationEngine.cs
git commit -m "refactor: update engine node processors to use new token types"
```

---

### Task 4: Update tests to use new token types

**Files:**
- Modify: `Tests/EngineTests.cs`

**Step 1: Update BuildConnectedPipeline helper**

Replace the entire `BuildConnectedPipeline` method and `Connect` calls inside it:

```csharp
private static SimulationEngine BuildConnectedPipeline()
{
    var engine = new SimulationEngine();
    var state  = engine.State;

    var market  = NodeFactory.Create(NodeType.Market,            0,    0);
    var pm      = NodeFactory.Create(NodeType.ProductManagement, 220,  0);
    var ba      = NodeFactory.Create(NodeType.BusinessAnalysis,  440,  0);
    var dev     = NodeFactory.Create(NodeType.Development,       660,  0);
    var ops     = NodeFactory.Create(NodeType.Operations,        880,  0);
    var compute = NodeFactory.Create(NodeType.HostedCompute,    1100,  0);
    var users   = NodeFactory.Create(NodeType.Users,            1320,  0);
    var support = NodeFactory.Create(NodeType.Support,          1540,  0);

    state.Nodes.AddRange([market, pm, ba, dev, ops, compute, users, support]);

    Connect(state, market,  TokenType.Opportunity,    pm,      TokenType.Opportunity);
    Connect(state, pm,      TokenType.Feature,        ba,      TokenType.Feature);
    Connect(state, ba,      TokenType.Feature,        dev,     TokenType.Feature);
    Connect(state, dev,     TokenType.Code,           ops,     TokenType.Code);
    Connect(state, ops,     TokenType.ValidatedCode,  compute, TokenType.ValidatedCode);
    Connect(state, compute, TokenType.RunningSoftware,users,   TokenType.RunningSoftware);
    Connect(state, users,   TokenType.Incident,       support, TokenType.Incident);
    Connect(state, support, TokenType.Defect,         dev,     TokenType.Defect);

    return engine;
}
```

**Step 2: Update NodeFactory_Creates_Correct_Ports test**

```csharp
[Fact]
public void NodeFactory_Creates_Correct_Ports()
{
    var market = NodeFactory.Create(NodeType.Market);
    Assert.Contains(market.OutputPorts, p => p.TokenType == TokenType.Opportunity);

    var dev = NodeFactory.Create(NodeType.Development);
    Assert.Contains(dev.InputPorts,  p => p.TokenType == TokenType.Feature);
    Assert.Contains(dev.InputPorts,  p => p.TokenType == TokenType.Defect);
    Assert.Contains(dev.OutputPorts, p => p.TokenType == TokenType.Code);

    var support = NodeFactory.Create(NodeType.Support);
    Assert.Contains(support.InputPorts,  p => p.TokenType == TokenType.Incident);
    Assert.Contains(support.OutputPorts, p => p.TokenType == TokenType.Defect);
    Assert.Contains(support.OutputPorts, p => p.TokenType == TokenType.Opportunity);
}
```

**Step 3: Update Connections_Route_Tokens_To_Target_Node test**

```csharp
[Fact]
public void Connections_Route_Tokens_To_Target_Node()
{
    var engine = new SimulationEngine();
    var market = NodeFactory.Create(NodeType.Market,            0,   0);
    var pm     = NodeFactory.Create(NodeType.ProductManagement, 220, 0);
    engine.State.Nodes.AddRange([market, pm]);
    Connect(engine.State, market, TokenType.Opportunity, pm, TokenType.Opportunity);

    engine.ManualTick();

    Assert.True(engine.State.TotalDemandReceived > 0);
}
```

**Step 4: Add golden integration test**

Add this new test at the bottom of the class:

```csharp
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
```

**Step 5: Run all tests**

```bash
dotnet test
```

Expected: all tests pass (12 existing + 1 new = 13 total).

**Step 6: Commit**

```bash
git add Tests/EngineTests.cs
git commit -m "test: update tests to use new token taxonomy, add golden integration test"
```

---

## Phase 2 — Node Restructure

> Logic changes. Add Quality node, rename Market→CustomerDiscovery, remove BusinessAnalysis/HostedCompute/UserWorkstation/Users as nodes, convert Users to environment actor.

### Task 5: Update NodeType enum

**Files:**
- Modify: `Client/Models/Node.cs`

**Step 1: Replace the NodeType enum**

```csharp
public enum NodeType
{
    CustomerDiscovery,  // Generates Opportunity from market demand
    ProductManagement,  // Converts Opportunity → Feature
    Development,        // Converts Feature + Defect → Code + TechDebt
    Quality,            // Validates Code → ValidatedCode; detects Defects
    Operations,         // Deploys ValidatedCode → RunningSoftware
    Support,            // Routes Incident → Defect + Opportunity
}
```

**Step 2: Run build to see compile errors**

```bash
dotnet build
```

Expected: errors in NodeFactory, SimulationEngine, and tests wherever old NodeType values are used.

**Step 3: Commit**

```bash
git add Client/Models/Node.cs
git commit -m "refactor: replace NodeType enum with 6 capability nodes"
```

---

### Task 6: Update NodeFactory for new node types

**Files:**
- Modify: `Client/Models/NodeFactory.cs`

**Step 1: Replace GetDisplayName**

```csharp
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
```

**Step 2: Replace ConfigurePorts**

```csharp
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
            In(node,  TokenType.TechDebt,       "Tech Debt");
            Out(node, TokenType.Code,           "Code");
            Out(node, TokenType.TechDebt,       "Tech Debt");
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
```

**Step 3: Run build**

```bash
dotnet build
```

Expected: NodeFactory errors resolved. Engine and test errors remain.

**Step 4: Commit**

```bash
git add Client/Models/NodeFactory.cs
git commit -m "refactor: update NodeFactory for 6 capability node types with Quality node"
```

---

### Task 7: Update SimulationEngine for new node types

**Files:**
- Modify: `Client/GameEngine/SimulationEngine.cs`

**Step 1: Update ProcessNode switch**

```csharp
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
```

**Step 2: Replace ProcessMarket with ProcessCustomerDiscovery**

```csharp
/// CustomerDiscovery: generates Opportunity from customer base each tick.
private void ProcessCustomerDiscovery(Node node)
{
    double demand = State.CustomerCount * DemandPerCustomer;
    PushOutput(node, TokenType.Opportunity, demand);
    State.TotalDemandReceived += demand;
    node.LastThroughput = demand;
}
```

**Step 3: Add ProcessQuality method**

Quality detects defects in code. Detected defects route back to Development. Escaped defects become Incidents via Operations.

```csharp
private const double BaseDetectionRate = 0.70; // Quality catches 70% of defects by default

/// Quality: validates Code → ValidatedCode + Defect (detected).
/// Escaped defects are handled by Operations generating Incidents.
private void ProcessQuality(Node node)
{
    double code = Consume(node, TokenType.Code, BaseThroughput);
    if (code <= 0) return;

    double defectDensity     = State.TechnicalDebt / 50_000.0;   // higher debt = more defects in code
    double totalDefects      = code * defectDensity;
    double detectedDefects   = totalDefects * BaseDetectionRate;
    double validatedCode     = code - detectedDefects;

    PushOutput(node, TokenType.ValidatedCode, Math.Max(0, validatedCode));
    if (detectedDefects > 0.0001)
        PushOutput(node, TokenType.Defect, detectedDefects);

    State.TotalBugsGenerated += totalDefects;
    node.LastThroughput = code;
}
```

**Step 4: Add ProcessOperations method**

Operations deploys ValidatedCode. Some escaped defects become Incidents in production.

```csharp
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
```

**Step 5: Update UpdateGlobalMetrics to handle Users as environment actor**

Replace the existing `UpdateGlobalMetrics` method:

```csharp
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

    // Users as environment actor: RunningSoftware generates economic signals
    double totalRunning = State.Nodes
        .Sum(n => n.Queue.TryGetValue(TokenType.RunningSoftware, out var q) ? q : 0);

    if (totalRunning > 0)
    {
        double revenue = totalRunning * State.CustomerCount * RevenueRate;
        State.Funds          += revenue;
        State.TotalRevenue   += revenue;
        State.TotalWorkDelivered += totalRunning;

        // User-discovered bugs feed Support queues
        double userBugs = totalRunning * BugDiscoveryRate;
        var supportNodes = State.Nodes.Where(n => n.NodeType == NodeType.Support);
        foreach (var support in supportNodes)
        {
            support.Queue.TryGetValue(TokenType.Incident, out double existing);
            support.Queue[TokenType.Incident] = existing + userBugs / Math.Max(1, supportNodes.Count());
        }
        State.TotalIncidents += userBugs;

        // Running software generates new feature opportunities back to CustomerDiscovery
        double newOpportunities = totalRunning * MarketExpansionRate;
        var discoveryNodes = State.Nodes.Where(n => n.NodeType == NodeType.CustomerDiscovery);
        foreach (var discovery in discoveryNodes)
        {
            discovery.Queue.TryGetValue(TokenType.Opportunity, out double existing);
            discovery.Queue[TokenType.Opportunity] = existing + newOpportunities / Math.Max(1, discoveryNodes.Count());
        }

        State.CustomerSatisfaction = Math.Min(1.0, State.CustomerSatisfaction + totalRunning * 0.001);

        // Consume RunningSoftware from nodes (it was sampled above)
        foreach (var n in State.Nodes)
            n.Queue.Remove(TokenType.RunningSoftware);
    }
}
```

**Step 6: Remove ProcessUsers — it's now the environment actor above**

Delete the `ProcessUsers` method entirely.

**Step 7: Remove the old RevenuePerWork and BaseIncidentRate constants (now replaced)**

Remove:
```csharp
private const double BaseIncidentRate   = 0.15;
private const double RevenuePerWork     = 2.5;
```

**Step 8: Run build**

```bash
dotnet build
```

Expected: clean build (or only test errors).

**Step 9: Commit**

```bash
git add Client/GameEngine/SimulationEngine.cs
git commit -m "refactor: implement 6-node engine with Quality detection formula and Users as environment actor"
```

---

### Task 8: Update tests for new node structure

**Files:**
- Modify: `Tests/EngineTests.cs`

**Step 1: Replace BuildConnectedPipeline with new 6-node pipeline**

```csharp
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
```

**Step 2: Update NodeFactory_Creates_Correct_Ports**

```csharp
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
```

**Step 3: Update Unconnected_Output_Tokens_Are_Silently_Dropped test**

```csharp
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
```

**Step 4: Update Connections_Route_Tokens_To_Target_Node test**

```csharp
[Fact]
public void Connections_Route_Tokens_To_Target_Node()
{
    var engine    = new SimulationEngine();
    var discovery = NodeFactory.Create(NodeType.CustomerDiscovery, 0,   0);
    var pm        = NodeFactory.Create(NodeType.ProductManagement, 220, 0);
    engine.State.Nodes.AddRange([discovery, pm]);
    Connect(engine.State, discovery, TokenType.Opportunity, pm, TokenType.Opportunity);

    engine.ManualTick();

    Assert.True(engine.State.TotalDemandReceived > 0);
}
```

**Step 5: Add a Quality loop test**

```csharp
[Fact]
public void Quality_Detects_Defects_And_Routes_Back_To_Development()
{
    // Set up a mini pipeline: Dev → Quality → Dev (defect feedback loop)
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
}
```

**Step 6: Run all tests**

```bash
dotnet test
```

Expected: all tests pass.

**Step 7: Commit**

```bash
git add Tests/EngineTests.cs
git commit -m "test: update all tests for 6-node capability model, add Quality loop test"
```

---

## Phase 3 — Capacity Modifiers

> Gameplay depth. Queue pressure, infrastructure tiers, practice effect wiring.

### Task 9: Add InfrastructureTier to GameState

**Files:**
- Modify: `Client/GameEngine/GameState.cs`

**Step 1: Add InfrastructureTier enum and property**

Add a new enum above the `GameState` class (same file is fine):

```csharp
public enum InfrastructureTier
{
    OnPrem      = 0,  // ×1.0 Operations throughput
    CloudVM     = 1,  // ×1.5
    Autoscaling = 2,  // ×2.5
    EdgeNetwork = 3,  // ×4.0
}
```

Add a property to `GameState`:

```csharp
public InfrastructureTier Infrastructure { get; set; } = InfrastructureTier.OnPrem;
```

**Step 2: Run build — should be clean**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add Client/GameEngine/GameState.cs
git commit -m "feat: add InfrastructureTier enum and GameState property"
```

---

### Task 10: Implement capacity formula in SimulationEngine

**Files:**
- Modify: `Client/GameEngine/SimulationEngine.cs`

**Step 1: Add a GetNodeCapacity helper**

Add this method to `SimulationEngine`:

```csharp
private const double FlowConstant = 20.0; // queue size at which throughput halves

private double GetNodeCapacity(Node node)
{
    int staffCount = State.Employees.Count(e => e.AssignedNodeId == node.Id);

    double practiceModifier = GetPracticeModifier(node);
    double infraModifier    = node.NodeType == NodeType.Operations
                              ? GetInfraModifier()
                              : 1.0;
    double debtPenalty      = node.NodeType == NodeType.Development
                              ? State.TechnicalDebt / 10_000.0
                              : 0.0;

    double totalQueueSize   = node.Queue.Values.Sum();

    double nodeCapacity     = Math.Max(0,
                                BaseThroughput
                                * (1.0 + staffCount * 0.5)
                                * practiceModifier
                                * infraModifier
                                - debtPenalty);

    double effectiveCapacity = nodeCapacity / (1.0 + totalQueueSize / FlowConstant);

    return Math.Max(0, effectiveCapacity);
}

private double GetInfraModifier() => State.Infrastructure switch
{
    InfrastructureTier.OnPrem      => 1.0,
    InfrastructureTier.CloudVM     => 1.5,
    InfrastructureTier.Autoscaling => 2.5,
    InfrastructureTier.EdgeNetwork => 4.0,
    _ => 1.0
};

private double GetPracticeModifier(Node node)
{
    double modifier = 1.0;
    foreach (var practice in State.Practices)
    {
        double adoption = practice.AdoptionLevel;
        if (adoption <= 0) continue;

        modifier += practice.Type switch
        {
            // TDD: Dev gets speed penalty but defect rate drops (handled in ProcessDevelopment)
            PracticeType.TDD when node.NodeType == NodeType.Development
                => -0.10 * adoption,

            // CI: Operations throughput boost
            PracticeType.ContinuousIntegration when node.NodeType == NodeType.Operations
                => 0.30 * adoption,

            // CD: Quality throughput boost
            PracticeType.ContinuousDelivery when node.NodeType == NodeType.Quality
                => 0.25 * adoption,

            // DevOpsCulture: Support throughput boost (faster incident resolution)
            PracticeType.DevOpsCulture when node.NodeType == NodeType.Support
                => 0.40 * adoption,

            _ => 0.0
        };
    }
    return Math.Max(0.1, modifier); // never reduce to zero
}
```

**Step 2: Update ProcessDevelopment to use GetNodeCapacity and TDD defect modifier**

```csharp
private void ProcessDevelopment(Node node)
{
    double capacity = GetNodeCapacity(node);

    // TDD reduces defect rate
    double tddAdoption   = State.Practices.FirstOrDefault(p => p.Type == PracticeType.TDD)?.AdoptionLevel ?? 0;
    double defectRate    = 0.05 * (1.0 - 0.40 * tddAdoption);
    double techDebtRate  = 0.05 * (1.0 - 0.25 * tddAdoption);

    double features = Consume(node, TokenType.Feature,  capacity);
    double rework   = Consume(node, TokenType.Defect,   capacity * 0.5);
    _ =              ConsumeAll(node, TokenType.TechDebt); // consume but don't transform
    double produced = features + rework;

    if (produced <= 0) return;

    double techDebtAccrued = produced * techDebtRate;
    State.TechnicalDebt += techDebtAccrued;
    PushOutput(node, TokenType.Code,     produced);
    PushOutput(node, TokenType.TechDebt, techDebtAccrued);
    State.TotalBugsGenerated += produced * defectRate;
    node.LastThroughput = produced;
}
```

**Step 3: Update ProcessTransform to use GetNodeCapacity**

```csharp
private void ProcessTransform(Node node, TokenType input, TokenType output, double efficiency)
{
    double capacity = GetNodeCapacity(node);
    double consumed = Consume(node, input, capacity);
    if (consumed <= 0) return;
    PushOutput(node, output, consumed * efficiency);
    node.LastThroughput = consumed;
}
```

**Step 4: Update ProcessQuality to use GetNodeCapacity**

```csharp
private void ProcessQuality(Node node)
{
    double capacity = GetNodeCapacity(node);
    double code     = Consume(node, TokenType.Code, capacity);
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
```

**Step 5: Update ProcessOperations to use GetNodeCapacity**

```csharp
private void ProcessOperations(Node node)
{
    double capacity  = GetNodeCapacity(node); // includes InfraModifier
    double validated = Consume(node, TokenType.ValidatedCode, capacity);
    if (validated <= 0) return;

    // CI practice reduces incident rate
    double ciAdoption    = State.Practices.FirstOrDefault(p => p.Type == PracticeType.ContinuousIntegration)?.AdoptionLevel ?? 0;
    double incidentRate  = BaseEscapedDefectRate * (1.0 - 0.20 * ciAdoption) * (1.0 + State.TechnicalDebt / 100_000.0);
    double incidents     = validated * incidentRate;

    PushOutput(node, TokenType.RunningSoftware, validated);
    if (incidents > 0.0001)
        PushOutput(node, TokenType.Incident, incidents);

    node.LastThroughput = validated;
}
```

**Step 6: Run build**

```bash
dotnet build
```

**Step 7: Run all tests**

```bash
dotnet test
```

Expected: all tests pass.

**Step 8: Commit**

```bash
git add Client/GameEngine/SimulationEngine.cs
git commit -m "feat: implement capacity formula with queue pressure, infra tiers, and practice modifiers"
```

---

### Task 11: Add capacity formula tests

**Files:**
- Modify: `Tests/EngineTests.cs`

**Step 1: Add infrastructure tier test**

```csharp
[Fact]
public void Higher_Infrastructure_Tier_Increases_Operations_Throughput()
{
    var engineBase  = BuildConnectedPipeline();
    var engineCloud = BuildConnectedPipeline();
    engineCloud.State.Infrastructure = InfrastructureTier.CloudVM;

    // Seed both with ValidatedCode at Operations
    var opsBase  = engineBase.State.Nodes.First(n => n.NodeType == NodeType.Operations);
    var opsCloud = engineCloud.State.Nodes.First(n => n.NodeType == NodeType.Operations);
    opsBase.Queue[TokenType.ValidatedCode]  = 50.0;
    opsCloud.Queue[TokenType.ValidatedCode] = 50.0;

    engineBase.ManualTick();
    engineCloud.ManualTick();

    Assert.True(opsCloud.LastThroughput > opsBase.LastThroughput,
        "CloudVM should process more ValidatedCode per tick than OnPrem");
}
```

**Step 2: Add queue pressure test**

```csharp
[Fact]
public void High_Queue_Reduces_Effective_Throughput()
{
    var engine = new SimulationEngine();
    var dev    = NodeFactory.Create(NodeType.Development, 0, 0);
    engine.State.Nodes.Add(dev);

    // Small queue
    dev.Queue[TokenType.Feature] = 5.0;
    engine.ManualTick();
    double throughputSmallQueue = dev.LastThroughput;

    // Large queue (reset)
    dev.Queue[TokenType.Feature] = 200.0;
    engine.ManualTick();
    double throughputLargeQueue = dev.LastThroughput;

    Assert.True(throughputLargeQueue < throughputSmallQueue * 1.5,
        "Large queue should reduce effective capacity via queue pressure formula");
}
```

**Step 3: Run all tests**

```bash
dotnet test
```

Expected: all tests pass.

**Step 4: Commit**

```bash
git add Tests/EngineTests.cs
git commit -m "test: add infrastructure tier and queue pressure tests"
```

---

## Completion Checklist

- [ ] Phase 1: `TokenType` enum has 9 canonical values
- [ ] Phase 1: `NodeFactory` ports use new token types
- [ ] Phase 1: Engine processors use new token types
- [ ] Phase 1: Tests updated + golden integration test passes
- [ ] Phase 2: `NodeType` enum has 6 capability nodes
- [ ] Phase 2: `Quality` node with detection formula implemented
- [ ] Phase 2: `Users` replaced by environment actor in `UpdateGlobalMetrics`
- [ ] Phase 2: `Support` outputs `Defect` + `Opportunity`
- [ ] Phase 2: All tests updated and passing
- [ ] Phase 3: `InfrastructureTier` enum and `GameState` property added
- [ ] Phase 3: `GetNodeCapacity` formula implemented with staff, practice, infra, debt, queue pressure
- [ ] Phase 3: All tests pass
- [ ] All commits are clean and build passes after each one
