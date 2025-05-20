using RaftLeaderElection;
using System.Collections.Concurrent;

var cts = new CancellationTokenSource();

// Use ConcurrentDictionary for thread-safe node management
var nodes = new ConcurrentDictionary<string, RaftNode>();

// Initialize nodes
for (int i = 1; i <= 3; i++)
{
    var node = new RaftNode($"Node-{i}", nodes, cts.Token);
    nodes.TryAdd(node.Id, node);
}

var tasks = nodes.Values.Select(n => n.RunAsync()).ToList();

Console.WriteLine("🚀 Cluster started with 3 nodes.");
await Task.Delay(5000);

// Add 2 more nodes dynamically
for (int i = 4; i <= 5; i++)
{
    var newNode = new RaftNode($"Node-{i}", nodes, cts.Token);
    nodes.TryAdd(newNode.Id, newNode);

    Console.WriteLine($"➕ Node-{i} added to the cluster.");

    // Start the new node asynchronously
    tasks.Add(newNode.RunAsync());
}

Console.WriteLine("⏳ Running with new nodes. Simulating failure in 7s.");
await Task.Delay(7000);

// Simulate a leader failure
var currentLeader = nodes.Values.FirstOrDefault(n => n.State == NodeState.Leader);
currentLeader?.SimulateFailure();

// Let system recover
await Task.Delay(10000);
cts.Cancel();
await Task.WhenAll(tasks);

Console.WriteLine("Simulation ended.");

