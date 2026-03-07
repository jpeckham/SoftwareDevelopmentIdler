namespace SoftwareVSM.Client.GameEngine;

using SoftwareVSM.Client.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Timers;
using Timer = System.Timers.Timer;

public class SimulationEngine
{
    public GameState State { get; private set; }
    private Timer _timer;

    public event Action? OnTick;

    public SimulationEngine()
    {
        State = new GameState();
        InitializeDefaultState();

        _timer = new Timer(1000);
        _timer.Elapsed += (sender, e) => Tick();
    }

    public void LoadState(GameState state)
    {
        State = state;
        OnTick?.Invoke();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    public void ManualTick() => Tick();

    private void InitializeDefaultState()
    {
        State.Nodes.Add(new Node { Id = "customers", NodeType = NodeType.Customers, Name = "Customers" });
        State.Nodes.Add(new Node { Id = "product", NodeType = NodeType.ProductLeadership, Name = "Product Leadership" });
        State.Nodes.Add(new Node { Id = "ba", NodeType = NodeType.BusinessAnalyst, Name = "Business Analyst" });
        State.Nodes.Add(new Node { Id = "dev", NodeType = NodeType.Developers, Name = "Developers" });
        State.Nodes.Add(new Node { Id = "qa", NodeType = NodeType.QA, Name = "QA" });
        State.Nodes.Add(new Node { Id = "ops", NodeType = NodeType.Operations, Name = "Operations" });
        State.Nodes.Add(new Node { Id = "support", NodeType = NodeType.Support, Name = "Support" });

        State.Funds = 500000;
        State.CustomerCount = 1000;
        
        State.Employees.Add(new Employee { Role = EmployeeRole.ProductManager, Name = "Prod Manager 1", AssignedNodeId = "product", Salary = 120000 });
        State.Employees.Add(new Employee { Role = EmployeeRole.BusinessAnalyst, Name = "BA 1", AssignedNodeId = "ba", Salary = 100000 });
        State.Employees.Add(new Employee { Role = EmployeeRole.Developer, Name = "Dev 1", AssignedNodeId = "dev", Salary = 130000 });
        State.Employees.Add(new Employee { Role = EmployeeRole.QAEngineer, Name = "QA 1", AssignedNodeId = "qa", Salary = 90000 });
        State.Employees.Add(new Employee { Role = EmployeeRole.OperationsEngineer, Name = "Ops 1", AssignedNodeId = "ops", Salary = 110000 });
        State.Employees.Add(new Employee { Role = EmployeeRole.SupportAnalyst, Name = "Support 1", AssignedNodeId = "support", Salary = 70000 });

        // Add some unassigned staff for drag and drop interactions
        State.Employees.Add(new Employee { Role = EmployeeRole.Developer, Name = "Trainee Dev", AssignedNodeId = "", Salary = 60000, SkillLevel = 0.5 });
        State.Employees.Add(new Employee { Role = EmployeeRole.QAEngineer, Name = "Sr QA", AssignedNodeId = "", Salary = 120000, SkillLevel = 1.5 });
    }

    private void Tick()
    {
        try
        {
            GenerateCustomerDemand();
            ProcessProduct();
            ProcessBA();
            ProcessDevs();
            ProcessQA();
            ProcessOps();
            ProcessSupport();
            
            UpdateMetrics();
            OnTick?.Invoke();
        }
        catch(Exception)
        {
            // Catch all to prevent timer crashes during iteration
        }
    }

    private double GetNodeThroughput(NodeType type)
    {
        var node = State.Nodes.FirstOrDefault(n => n.NodeType == type);
        if (node == null) return 0;
        var staff = State.Employees.Where(e => e.AssignedNodeId == node.Id).ToList();
        return staff.Sum(e => Math.Max(0.1, e.Productivity * e.SkillLevel));
    }

    private double ConsumeQueue(Node node, TokenType tokenType, double maxAmount)
    {
        if (!node.Queue.ContainsKey(tokenType)) return 0;
        double available = node.Queue[tokenType];
        double consumed = Math.Min(maxAmount, available);
        node.Queue[tokenType] -= consumed;
        return consumed;
    }

    private void ProduceToken(Node target, TokenType tokenType, double amount)
    {
        if (target == null) return;
        if (!target.Queue.ContainsKey(tokenType)) target.Queue[tokenType] = 0;
        target.Queue[tokenType] += amount;
    }

    private void GenerateCustomerDemand()
    {
        var prodNode = State.Nodes.First(n => n.NodeType == NodeType.ProductLeadership);
        double demandRate = State.CustomerCount * 0.05; 
        ProduceToken(prodNode, TokenType.Demand, demandRate);
        State.TotalDemandReceived += demandRate;
    }

    private void ProcessProduct()
    {
        var prodNode = State.Nodes.First(n => n.NodeType == NodeType.ProductLeadership);
        var baNode = State.Nodes.First(n => n.NodeType == NodeType.BusinessAnalyst);
        
        double throughput = GetNodeThroughput(NodeType.ProductLeadership) * 10;
        double consumedDemand = ConsumeQueue(prodNode, TokenType.Demand, throughput);
        
        double vision = consumedDemand * 1.0; 
        ProduceToken(baNode, TokenType.Vision, vision);
        prodNode.LastThroughput = vision;
    }

    private void ProcessBA()
    {
        var baNode = State.Nodes.First(n => n.NodeType == NodeType.BusinessAnalyst);
        var devNode = State.Nodes.First(n => n.NodeType == NodeType.Developers);
        
        double throughput = GetNodeThroughput(NodeType.BusinessAnalyst) * 8;
        double consumedVision = ConsumeQueue(baNode, TokenType.Vision, throughput);
        
        double detailedDemand = consumedVision * 1.0;
        ProduceToken(devNode, TokenType.DetailedDemand, detailedDemand);
        baNode.LastThroughput = detailedDemand;
    }

    private void ProcessDevs()
    {
        var devNode = State.Nodes.First(n => n.NodeType == NodeType.Developers);
        var qaNode = State.Nodes.First(n => n.NodeType == NodeType.QA);
        var supportNode = State.Nodes.First(n => n.NodeType == NodeType.Support);
        
        double throughput = GetNodeThroughput(NodeType.Developers) * 5; 
        
        double debtFactor = Math.Max(0.1, 1 - (State.TechnicalDebt / 10000.0));
        throughput *= debtFactor;

        double consumed = ConsumeQueue(devNode, TokenType.DetailedDemand, throughput);
        
        double defectRate = 0.15 + (State.TechnicalDebt / 20000.0);
        double solutions = consumed;
        double failureDemand = solutions * defectRate;
        
        ProduceToken(qaNode, TokenType.Solutions, solutions);
        ProduceToken(supportNode, TokenType.FailureDemand, failureDemand);

        State.TotalBugsGenerated += failureDemand;
        State.TechnicalDebt += solutions * 0.05;

        devNode.LastThroughput = solutions;
    }

    private void ProcessQA()
    {
        var qaNode = State.Nodes.First(n => n.NodeType == NodeType.QA);
        var opsNode = State.Nodes.First(n => n.NodeType == NodeType.Operations);
        var devNode = State.Nodes.First(n => n.NodeType == NodeType.Developers);
        
        double throughput = GetNodeThroughput(NodeType.QA) * 6;
        double consumed = ConsumeQueue(qaNode, TokenType.Solutions, throughput);
        
        double failureRate = 0.10; 
        double validSolutions = consumed * (1 - failureRate);
        double internalFailure = consumed * failureRate;
        
        ProduceToken(opsNode, TokenType.ValidatedSolutions, validSolutions);
        ProduceToken(devNode, TokenType.DetailedDemand, internalFailure); 
        
        qaNode.LastThroughput = validSolutions;
    }

    private void ProcessOps()
    {
        var opsNode = State.Nodes.First(n => n.NodeType == NodeType.Operations);
        var supportNode = State.Nodes.First(n => n.NodeType == NodeType.Support);
        
        double throughput = GetNodeThroughput(NodeType.Operations) * 7;
        double consumed = ConsumeQueue(opsNode, TokenType.ValidatedSolutions, throughput);
        
        double deployErrorRate = 0.05;
        double operationalSolutions = consumed * (1 - deployErrorRate);
        double operationalIncidents = consumed * deployErrorRate;

        ProduceToken(opsNode, TokenType.OperationalSolutions, operationalSolutions);
        State.TotalFeaturesDelivered += operationalSolutions;

        ProduceToken(supportNode, TokenType.FailureDemand, operationalIncidents);
        State.TotalIncidents += operationalIncidents;
        
        opsNode.LastThroughput = operationalSolutions;
    }

    private void ProcessSupport()
    {
        var supportNode = State.Nodes.First(n => n.NodeType == NodeType.Support);
        
        double throughput = GetNodeThroughput(NodeType.Support) * 4;
        double consumed = ConsumeQueue(supportNode, TokenType.FailureDemand, throughput);
        
        ProduceToken(supportNode, TokenType.ResolvedIssues, consumed);
        supportNode.LastThroughput = consumed;
    }

    private void UpdateMetrics()
    {
        double dailySalaries = State.Employees.Sum(e => e.Salary) / 365.0;
        State.Funds -= dailySalaries;

        State.Funds += (State.CustomerCount * 2.5); 
        
        var supportNode = State.Nodes.First(n => n.NodeType == NodeType.Support);
        double activeBugs = supportNode.Queue.ContainsKey(TokenType.FailureDemand) ? supportNode.Queue[TokenType.FailureDemand] : 0;
        
        State.CustomerSatisfaction -= (activeBugs * 0.001);
        if(State.CustomerSatisfaction < 0.1) State.CustomerSatisfaction = 0.1;
        if(activeBugs == 0) State.CustomerSatisfaction += 0.005;
        if(State.CustomerSatisfaction > 1.0) State.CustomerSatisfaction = 1.0;
        
        if (State.CustomerSatisfaction < 0.5)
        {
            State.CustomerCount -= (int)(State.CustomerCount * 0.01); 
        }
    }
}
