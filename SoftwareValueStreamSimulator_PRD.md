
# PRD.md — Software Value Stream Simulator

## Product Vision
Software Value Stream Simulator is an idle/management simulation game that models a software company using Lean and Value Stream Mapping principles.

Players construct and optimize a value stream that transforms:

Customer Demand → Product Ideas → Requirements → Code → Running Software → Customer Value.

The system intentionally produces waste, delays, defects, and feedback loops. The player must balance staffing, process maturity, practices, and flow efficiency.

---

## Core Game Concept

The game world is a **Value Stream Map (VSM)** where nodes represent stages of the software delivery lifecycle.

Each node:

- consumes tokens
- processes them
- produces new tokens
- may generate waste or failure demand

Tokens continuously move through the pipeline.

---

## Platform

Blazor WebAssembly SPA.

Requirements:

- Runs fully in browser
- No backend required for MVP
- Save state using browser LocalStorage

---

## Core Token Types

| Token | Meaning |
|------|------|
Demand | customer requests |
Vision | product direction |
DetailedDemand | refined requirements |
Solutions | coded features |
OperationalSolutions | deployed features |
FailureDemand | bugs, incidents |
Revenue | money |
ChurnRisk | customer dissatisfaction |

---

## Core Value Stream Nodes

### Customers

Outputs:

Demand  
Revenue  
FailureDemand

Formula:

DemandRate = CustomerCount * DemandFactor

---

### Product Leadership

Input:

Demand

Output:

Vision

Effect:

Filters customer demand into product direction.

Vision = Demand * ProductClarity

---

### Business Analyst

Input:

Vision

Output:

DetailedDemand

RequirementQuality = AnalystSkill

DetailedDemand = Vision * RequirementQuality

---

### Developers

Input:

DetailedDemand

Outputs:

Solutions  
FailureDemand

Solutions = DetailedDemand * DevVelocity

FailureDemand = Solutions * DefectRate

DefectRate influenced by:

- requirement clarity
- developer skill
- technical debt

---

### QA

Input:

Solutions

Outputs:

ValidatedSolutions  
InternalFailureDemand

QA reduces defects but slows throughput.

ThroughputModifier = 1 - QABottleneck

---

### Operations

Input:

ValidatedSolutions

Outputs:

OperationalSolutions  
OperationalIncidents

DeploymentRate = OpsSkill

IncidentRate = DeploymentRate * OpsErrorRate

---

### Support

Input:

FailureDemand

Outputs:

ResolvedIssues

Reduces customer dissatisfaction but consumes staff capacity.

---

## Failure Demand System

Failure demand represents:

- bugs
- outages
- incidents
- support tickets

Feedback loops:

FailureDemand → Developers  
FailureDemand → Support  
FailureDemand → Customer dissatisfaction

---

## Staffing Model

Roles:

- ProductManager
- BusinessAnalyst
- Developer
- QAEngineer
- OperationsEngineer
- SupportAnalyst

Employee attributes:

SkillLevel  
Salary  
Productivity  
ErrorRate  
PracticeKnowledge

---

## Practices

### Agile

Effects:

- smaller batches
- faster iteration

FlowSpeed +

---

### TDD

DevSpeed -10%  
DefectRate -40%

Reduces QA dependency.

---

### Continuous Integration

FailureDemand -20%

---

### Continuous Delivery

DeploymentRate +50%  
OpsIncidentRate +10%

---

### DevOps Culture

QueueTime -40%

---

## Queue Model

Each node maintains a queue.

QueueTime = QueueSize / ProcessingRate

Queues represent Lean waste.

---

## Technical Debt

Debt += Solutions * ShortcutRate

Effects:

DevVelocity -= DebtFactor  
DefectRate += DebtFactor

---

## Customer Satisfaction

SatisfactionScore =

(FeatureDelivery * 0.4)
- (BugRate * 0.3)
- (IncidentRate * 0.2)
- (DelayPenalty * 0.1)

Low satisfaction increases churn.

---

## Simulation Engine

Game tick runs every second.

Pseudo code:

while(gameRunning)
{
GenerateCustomerDemand()
ProcessNodes()
MoveTokens()
GenerateFailureDemand()
UpdateRevenue()
UpdateQueues()
}

---

## Blazor Architecture

Project structure:

/Client
  /Components
  /Pages
  /GameEngine
  /Models
  /Services

---

## Domain Models

Node

Id  
NodeType  
InputTokens  
OutputTokens  
CycleTime  
ErrorRate  
StaffAssigned  
Queue

Employee

Id  
Role  
Skill  
Salary  
PracticeKnowledge  
AssignedNode

Token

TokenType  
Quantity  
SourceNode  
TargetNode

---

## UI

Main screen shows value stream map.

Each node displays:

- queue size
- throughput
- error rate
- assigned staff

Nodes connected by animated token flows.

---

## Save System

Game state stored as JSON.

Saved automatically every 30 seconds.

---

## Testing

Unit tests required for:

SimulationEngine  
TokenFlow  
NodeProcessing  
PracticeModifiers

---

## MVP Scope

Nodes:

Customers  
Product Leadership  
Business Analyst  
Developers  
QA  
Operations

Practices:

Agile  
TDD  
CI/CD

---

## Future Expansion

Security
Architecture teams
Technical debt management
Platform engineering
AI coding assistants
