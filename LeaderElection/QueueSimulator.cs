using System.Collections.Concurrent;

namespace LeaderElection;

public class QueueSimulator
{
    private readonly ConcurrentQueue<int> _queue = new();

    private string? _currentLeader;
    private readonly object _lock = new();

    public bool TryToBecomeLeader(string podName)
    {
        lock (_lock)
        {
            if (_currentLeader == null)
            {
                _currentLeader = podName;
                Console.WriteLine($"{podName} is now the leader.");
                return true;
            }
            else
            {
                Console.WriteLine($"{podName} could not become the leader. Current leader is {_currentLeader}.");
                return false;
            }
        }
    }

    public void Push(int value) => _queue.Enqueue(value);

    public bool TryPop(out int value) => _queue.TryDequeue(out value);
}
