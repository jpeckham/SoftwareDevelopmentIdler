# Domain Evolution Design: Software VSM Simulator

**Date:** 2026-03-07
**Status:** Approved
**Approach:** Evolution (preserve engine, refactor semantics)

---

## Overview

Evolve the simulation from Version 0 infrastructure (9 nodes, 9 tokens) toward a realistic software value stream model (6 nodes, 9 tokens) across three independently shippable phases. Each phase compiles, runs, and passes tests before the next begins.

---

## Token Taxonomy (Final — 9 tokens)

| Token | Replaces | Meaning |
|---|---|---|
| `Opportunity` | `Demand` | Market signal: potential value to deliver |
| `Feature` | `Features`, `UserStories` | Defined work item ready for development |
| `Code` | `Software` | Output of development work |
| `ValidatedCode` | `DeployableArtifacts` | Code that has passed quality evaluation |
| `RunningSoftware` | `WorkProduct` | Deployed, running software in production |
| `Defect` | `FailureDemand`, `Dissatisfaction` | Quality issue requiring rework |
| `Incident` | `Incidents` | Production failure requiring ops response |
| `TechDebt` | *(scalar only)* | Explicit token representing accumulated shortcuts |
| `Revenue` | `Revenue` | Economic output from running software |

`BugReport` is not a token — Support produces `Defect` directly.

---

## Node Capability Model (Final — 6 nodes)

### Mapping from current system

| Current Node(s) | New Node | Notes |
|---|---|---|
| `Market` | `CustomerDiscovery` | Generates Opportunity from market demand |
| `Users` | *(environment actor)* | Removed as node; engine generates signals each tick |
| `ProductManagement` + `BusinessAnalysis` | `ProductManagement` | BA absorbed; single definition capability |
| `Development` | `Development` | Inputs now include Defect rework |
| `UserWorkstations` | `Quality` | New type; validates Code, surfaces defects |
| `Operations` | `Operations` | Keeps ValidatedCode → RunningSoftware flow |
| `HostedCompute` | `InfrastructureTier` modifier | Becomes capacity multiplier on Operations (Phase 3) |
| `Support` | `Support` | Generates both Defect and Opportunity |

### Node I/O specification

**CustomerDiscovery**
- Inputs: market signals (engine-driven)
- Outputs: `Opportunity`

**ProductManagement**
- Inputs: `Opportunity`
- Outputs: `Feature`

**Development**
- Inputs: `Feature`, `Defect`, `TechDebt`
- Outputs: `Code`, `TechDebt`
- Note: Does NOT output `Defect` — defects originate at Quality evaluation

**Quality**
- Inputs: `Code`
- Outputs: `ValidatedCode`, `Defect`
- Formula:
  ```
  DetectedDefects = Code × DetectionRate
  EscapedDefects  = Code × (1 − DetectionRate)
  ValidatedCode   = Code − DetectedDefects
  Defect output   = DetectedDefects        → routes back to Development
  EscapedDefects                           → becomes Incident
  ```

**Operations**
- Inputs: `ValidatedCode`
- Outputs: `RunningSoftware`, `Incident`
- Modifier: `InfrastructureTier` multiplies throughput

**Support**
- Inputs: `Incident`
- Outputs: `Defect` (bug fix rework), `Opportunity` (feature requests)

### Environment actor (Users — engine, not a node)

Each tick, the engine generates signals from `RunningSoftware`:

```
Revenue       = RunningSoftware × RevenueRate
UserDefects   = RunningSoftware × BugDiscoveryRate       → feeds Support as Incident
FeatureDemand = RunningSoftware × MarketExpansionRate    → feeds CustomerDiscovery as Opportunity
```

---

## Three Gameplay Loops

1. **Feature loop:** Opportunity → Feature → Code → ValidatedCode → RunningSoftware → Revenue
2. **Quality loop:** Code → Defect (detected) → Development backlog (more defects = slower delivery)
3. **Customer loop:** RunningSoftware → Incident/Opportunity → Support/ProductManagement backlogs

---

## Capacity Formula

```
NodeCapacity      = max(0,
                      BaseCapacity
                      × (1 + StaffCount × StaffMultiplier)
                      × PracticeModifier
                      × InfraModifier          // Operations only
                      − TechDebtPenalty        // Development only
                    )

EffectiveCapacity = NodeCapacity / (1 + QueueSize / FlowConstant)
```

`max(0, ...)` clamp prevents negative throughput from high tech debt.

Queue pressure implements the Lean principle: high WIP → slower throughput.

---

## Infrastructure Tiers (Operations modifier — Phase 3)

| Tier | Multiplier |
|---|---|
| `OnPrem` | ×1.0 |
| `CloudVM` | ×1.5 |
| `Autoscaling` | ×2.5 |
| `EdgeNetwork` | ×4.0 |

---

## Practice Effects

| Practice | Effects |
|---|---|
| `TDD` | Dev speed −10%, Defect rate −40%, TechDebt −25% |
| `ContinuousIntegration` | Deploy speed +30%, Incident rate −20% |
| `ContinuousDelivery` | ValidatedCode throughput +25% |
| `DevOpsCulture` | Incident recovery +40%, TechDebt decay +20% |

---

## Implementation Phases

### Phase 1 — Token Normalization (pure rename, no logic changes)

Changes:
- `TokenType` enum: rename all values to final taxonomy
- String labels in `NodeFactory`, `Port`, UI components
- Serialization (SaveService JSON keys)
- Tests updated to use new names
- Add golden integration test: Opportunity → Feature → Code → ValidatedCode → RunningSoftware → Revenue

**Result:** Game behaves identically, domain language is correct.

### Phase 2 — Node Restructure (logic changes)

Changes:
- Add `Quality` node type with detection formula
- Rename `Market` → `CustomerDiscovery`
- Merge `BusinessAnalysis` → `ProductManagement` (remove BA node type)
- Convert `Users` node → environment actor in engine tick
- Update `Support` to output both `Defect` and `Opportunity`
- Remove `Development` → `Defect` output path
- Update `NodeFactory` port configurations for all changed nodes
- Update engine processing for `Quality` detection formula

**Result:** Three gameplay loops are live. Simulation has realistic feedback dynamics.

### Phase 3 — Capacity Modifiers (gameplay depth)

Changes:
- Add `InfrastructureTier` enum to `GameState`
- Convert `HostedCompute` node → `InfrastructureTier` upgrade mechanic
- Implement capacity formula with staff multiplier, practice modifier, infra modifier, tech debt penalty, and queue pressure
- Wire practice effects table to node processing
- Add upgrade UI for infrastructure tiers

**Result:** Strategic depth — players manage WIP, staff, practices, and infrastructure to optimise flow.

---

## Golden Integration Test

```csharp
// After Phase 1: verify full pipeline flows
// Opportunity → Feature → Code → ValidatedCode → RunningSoftware → Revenue
// This test must always pass. If it fails, the core loop is broken.
```
