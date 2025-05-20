using RaftLeaderElection;

var cts = new CancellationTokenSource();
var nodes = new List<RaftNode>();

// Initialize nodes
for (int i = 1; i <= 3; i++)
{
    nodes.Add(new RaftNode($"Node-{i}", nodes, cts.Token));
}

var tasks = nodes.Select(n => n.RunAsync()).ToList();

Console.WriteLine("🚀 Cluster started with 3 nodes.");
await Task.Delay(5000);

// Add 2 more nodes dynamically
for (int i = 4; i <= 5; i++)
{
    var newNode = new RaftNode($"Node-{i}", nodes, cts.Token);
    nodes.Add(newNode);

    Console.WriteLine($"➕ Node-{i} added to the cluster.");

    // Start the new node asynchronously
    tasks.Add(newNode.RunAsync());
}

Console.WriteLine("⏳ Running with new nodes. Simulating failure in 7s.");
await Task.Delay(7000);

// Simulate a leader failure
var currentLeader = nodes.FirstOrDefault(n => n.State == NodeState.Leader);
currentLeader?.SimulateFailure();

// Let system recover
await Task.Delay(10000);
cts.Cancel();
await Task.WhenAll(tasks);

Console.WriteLine("Simulation ended.");