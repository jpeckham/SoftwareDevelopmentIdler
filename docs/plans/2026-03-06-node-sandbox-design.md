# Software Process Architect Design

## Concept Summary
The game transforms from a static, linear value stream pipeline into an open, interactive **Node Network Sandbox** (akin to Factorio). The player starts with just a Market/Customer node and must design, purchase, place, wire together, and staff an entire software development lifecycle process. 

## 1. Sandbox Canvas & Architecture
Instead of rendering nodes in a flat list:
- The UI becomes an open area with a grid background.
- Nodes are positioned via `X` and `Y` coordinates on the grid.
- Nodes can be dragged around the layout for visual organization.
- Unassigned staff remain in the "Bench" roster.
- Players drag staff onto the specific nodes on the grid to power them.

## 2. Dynamic Connections (Wiring)
- Nodes do not implicitly push to the "next logical node." 
- The player must explicitly create `Connection`s (wires) between a Source Node and a Target Node.
- The `SimulationEngine` will read the Connections array to determine where tokens flow during the `ProcessNodes` tick.
- This creates ultimate freedom: you can build branching paths (Bugs routing to Support, Features to QA), bottlenecks, parallel processing lines, and cyclic feedback loops.

## 3. Product-Market Economics
The economic model reflects realistic software growth:
- **Product Capability Score:** Ticked up whenever a "Feature" token successfully hits the final Market Node.
- **TAM (Total Addressable Market):** The absolute maximum number of potential users.
- **Acquisition (Customers):** You passively gain customers over time, capped by your current Product Capability. 
- **Revenue:** Active Customers * Subscription Fee = Constant Tick Revenue.
- **Operational Drag:** Customers passively generate "Operational Demand" and "Bug Reports" on your system.
- **Churn:** If Customer Satisfaction drops (due to unaddressed Bugs in Support or Operational Incidents), active customers Churn (leave), slashing your recurring revenue.

## 4. Unlocks & Progression
- You earn initial funds slowly by manual labor (e.g., fulfilling basic customer requests by hand).
- As revenue increases, you afford more expensive Nodes (e.g., DevOps tools, Automated Testing environments).
- Assigning Staff to nodes costs Salary per tick. If costs outpace Revenue, the company fails.

## Implementation Plan Next Steps
1. Refactor `Node` and `GameState` to support `X,Y` positions and a `List<Connection>` map.
2. Build an SVG layer in Blazor to draw the visual "wires" between connected `<NodeDetails>`.
3. Update `SimulationEngine.cs` tick logic to route tokens strictly along defined `Connection` paths.
4. Adapt the economic constants to support the new "TAM / Product Capability" mathematical curve.
