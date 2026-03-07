# Node Network Sandbox Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Transform the static value stream pipeline into an open, interactive 2D Factorio-style canvas where players manually place nodes, draw wired connections between outputs and inputs, and navigate a realistic Product/Market economic growth model.

**Architecture:** We will convert the UI to a grid-based canvas mapping Nodes via `X,Y` coordinates. We'll introduce a `Connection` model to replace hardcoded node processing logic. A new SVG overlay will render connections dynamically. The `SimulationEngine` will route tokens based exclusively on user-created `Connections`. 

**Tech Stack:** Blazor WebAssembly, C#, Vanilla CSS (Grid & SVG layers), xUnit.

---

### Task 1: Update Domain Models with Spatial & Connection Data

**Files:**
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/Models/Node.cs`
- Create: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/Models/Connection.cs`
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/GameEngine/GameState.cs`

**Step 1: Write the failing test**
In `Tests/EngineTests.cs`, add a test to verify a new connection can be established in the `GameState`.

```csharp
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
```

**Step 2: Run test to verify it fails**
Run: `dotnet test`
Expected: FAIL (Cannot resolve symbol 'X', 'Connection' does not exist).

**Step 3: Write minimal implementation**
- Create `Connection.cs` with `Id`, `SourceId`, and `TargetId`. 
- Update `Node.cs` with `public double X { get; set; } = 0;` and `public double Y { get; set; } = 0;`.
- Add `public List<Connection> Connections { get; set; } = new();` to `GameState.cs`.

**Step 4: Run test to verify it passes**
Run: `dotnet test`
Expected: PASS

**Step 5: Commit**
```bash
git add .
git commit -m "feat: Add spatial coordinates and connection models for open canvas"
```

---

### Task 2: Implement Dynamic Economic Model Constants

**Files:**
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/GameEngine/GameState.cs`
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Tests/EngineTests.cs`

**Step 1: Write the failing test**
```csharp
[Fact]
public void Economic_Model_Initial_Values()
{
    var state = new GameState();
    Assert.Equal(10000, state.TotalAddressableMarket);
    Assert.Equal(0, state.ProductCapability);
}
```

**Step 2: Run test to verify it fails**
Run: `dotnet test`
Expected: FAIL (TAM/Capability properties don't exist).

**Step 3: Write minimal implementation**
Add to `GameState.cs`:
```csharp
public int TotalAddressableMarket { get; set; } = 10000;
public double ProductCapability { get; set; } = 0.0;
```

**Step 4: Run test to verify it passes**
Run: `dotnet test`
Expected: PASS

**Step 5: Commit**
```bash
git commit -am "feat: Add TAM and Product Capability to GameState"
```

---

### Task 3: Refactor Engine Tick to Use Connections

**Files:**
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/GameEngine/SimulationEngine.cs`
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Tests/EngineTests.cs`

**Step 1: Write the failing test**
Create a test asserting that if `NodeA` connects to `NodeB`, tokens flow from A to B explicitly using `GameState.Connections`.

**Step 2: Run test to verify it fails**
Run: `dotnet test`
Expected: Flow fails because engine currently uses hardcoded `ProcessProduct()`, `ProcessBA()`, etc.

**Step 3: Write minimal implementation**
Rewrite `Tick()` in `SimulationEngine.cs` to loop through `GameState.Connections`. 
For each connection, read the Source Node output rate, and `ProduceToken` on the Target Node based on connection lines instead of hardcoded pathways. Remove `ProcessProduct()`, `ProcessBA()` hardcodes.

**Step 4: Run test to verify it passes**
Run: `dotnet test`
Expected: PASS

**Step 5: Commit**
```bash
git commit -am "refactor: Engine uses dynamic connection array for token routing"
```

---

### Task 4: Node Dragging Canvas UI

**Files:**
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/Pages/Home.razor`
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/wwwroot/app.css`

**Step 1: Implement minimal UI**
Change the `.vsm-container` to an absolute-positioned bounding box.
Update `<NodeDetails>` to use inline styles corresponding to `Node.X` and `Node.Y`.
Add `@onpointerdown`, `@onpointermove`, `@onpointerup` handlers to `Home.razor` to allow dragging a node to a new X/Y coordinate.

**Step 2: Test manually**
Run: `dotnet watch run`
Verify: User can drag Node UI cards freely around the screen, and they stay in their X/Y position when dropped.

**Step 3: Commit**
```bash
git commit -am "feat: Implement absolute positioning draggable canvas for nodes"
```

---

### Task 5: SVG Connection Drawing

**Files:**
- Modify: `c:/Users/james/source/repos/SoftwareVSMSimulator/Client/Pages/Home.razor`

**Step 1: Implement minimal UI**
Add an `<svg>` element spanning the entire screen `100vw 100vh` behind the nodes with a `z-index` of `-1`.
Draw `<line>` tags calculating the distance from `Connection.SourceId` Node to `Connection.TargetId` Node.
When a node moves (via Task 4 pointer events), trigger a state change so the SVG lines redraw attached to the nodes.

**Step 2: Test manually**
Run: `dotnet watch run`
Verify: Hardcoded connections draw visible white/blue strokes between node cards that snap and stretch when nodes are dragged.

**Step 3: Commit**
```bash
git commit -am "feat: Render SVG dynamic lines for established node connections"
```
