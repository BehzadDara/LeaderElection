using System.Collections.Concurrent;

namespace RaftLeaderElection;

public class RaftNode(string id, ConcurrentDictionary<string, RaftNode> allNodes, CancellationToken token)
{
    public string Id { get; } = id;
    public NodeState State { get; private set; } = NodeState.Follower;
    public int CurrentTerm { get; private set; } = 0;

    // Thread-safe cluster node collection
    private readonly ConcurrentDictionary<string, RaftNode> _allNodes = allNodes;
    private readonly CancellationToken _token = token;
    private readonly object _lock = new();

    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private bool _isAlive = true;

    // SemaphoreSlim for async-safe AddNode
    private readonly SemaphoreSlim _addNodeLock = new(1, 1);

    public Task RunAsync() => Task.Run(async () =>
    {
        while (!_token.IsCancellationRequested && _isAlive)
        {
            if (State == NodeState.Leader)
            {
                await SendHeartbeats();
                await Task.Delay(1000);
            }
            else
            {
                await Task.Delay(Random.Shared.Next(1500, 3000)); // Simulate election timeout

                if ((DateTime.UtcNow - _lastHeartbeat).TotalMilliseconds > 2000)
                {
                    Console.WriteLine($"{Id} hasn't received heartbeat. Starting election.");
                    StartElection();
                }
            }
        }
    });

    private async Task SendHeartbeats()
    {
        foreach (var node in _allNodes.Values.Where(n => n.Id != Id && n._isAlive))
        {
            node.ReceiveHeartbeat(CurrentTerm, Id);
        }
        Console.WriteLine($"❤️ {Id} (leader) sent heartbeats.");
        await Task.CompletedTask;
    }

    public void ReceiveHeartbeat(int term, string leaderId)
    {
        lock (_lock)
        {
            if (term >= CurrentTerm)
            {
                CurrentTerm = term;
                State = NodeState.Follower;
                _lastHeartbeat = DateTime.UtcNow;
                Console.WriteLine($"{Id} received heartbeat from {leaderId} for term {term}.");
            }
        }
    }

    private void StartElection()
    {
        lock (_lock)
        {
            State = NodeState.Candidate;
            CurrentTerm++;
            int votes = 1; // Vote for self
            Console.WriteLine($"{Id} started election for term {CurrentTerm}");

            foreach (var node in _allNodes.Values.Where(n => n.Id != Id && n._isAlive))
            {
                if (node.RequestVote(CurrentTerm))
                    votes++;
            }

            if (votes > _allNodes.Values.Count(n => n._isAlive) / 2)
            {
                State = NodeState.Leader;
                Console.WriteLine($"✅ {Id} is elected leader for term {CurrentTerm} with {votes} votes.");
            }
            else
            {
                Console.WriteLine($"❌ {Id} failed to become leader (got {votes} votes).");
                State = NodeState.Follower;
            }
        }
    }

    public bool RequestVote(int term)
    {
        lock (_lock)
        {
            if (term > CurrentTerm)
            {
                CurrentTerm = term;
                State = NodeState.Follower;
                _lastHeartbeat = DateTime.UtcNow;
                Console.WriteLine($"{Id} voted for term {term}");
                return true;
            }
            return false;
        }
    }

    public void SimulateFailure()
    {
        _isAlive = false;
        Console.WriteLine($"💀 {Id} has FAILED.");
    }

    // Async-safe AddNode method
    public async Task AddNodeAsync(RaftNode newNode)
    {
        await _addNodeLock.WaitAsync();
        try
        {
            if (_allNodes.TryAdd(newNode.Id, newNode))
            {
                Console.WriteLine($"🧩 {Id} acknowledged new node {newNode.Id}.");
            }
        }
        finally
        {
            _addNodeLock.Release();
        }
    }
}