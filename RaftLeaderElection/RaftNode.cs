namespace RaftLeaderElection;

public class RaftNode(string id, List<RaftNode> allNodes, CancellationToken token)
{
    public string Id { get; } = id;
    public NodeState State { get; private set; } = NodeState.Follower;
    public int CurrentTerm { get; private set; } = 0;

    private readonly List<RaftNode> _allNodes = allNodes;
    private readonly Random _random = new();
    private readonly CancellationToken _token = token;
    private readonly object _lock = new();

    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private bool _isAlive = true;

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
                await Task.Delay(_random.Next(1500, 3000)); // Simulate election timeout

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
        foreach (var node in _allNodes.Where(n => n.Id != Id && n._isAlive))
        {
            node.ReceiveHeartbeat(CurrentTerm, Id);
        }
        Console.WriteLine($"❤️ {Id} (leader) sent heartbeats.");
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

            foreach (var node in _allNodes.Where(n => n.Id != Id && n._isAlive))
            {
                if (node.RequestVote(CurrentTerm))
                    votes++;
            }

            if (votes > _allNodes.Count(n => n._isAlive) / 2)
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

    public void AddNode(RaftNode newNode)
    {
        lock (_lock)
        {
            _allNodes.Add(newNode);
            Console.WriteLine($"🧩 {Id} acknowledged new node {newNode.Id}.");
        }
    }
}
