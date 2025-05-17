namespace LeaderElection;

public class Pod
{
    private readonly string Name;
    private readonly QueueSimulator Queue;
    private readonly bool IsLeader;
    private readonly QueueProcessor? Processor;

    public Pod(string name, QueueSimulator queue)
    {
        Name = name;
        Queue = queue;
        IsLeader = queue.TryToBecomeLeader(name);

        if (IsLeader)
        {
            Processor = new QueueProcessor(name, queue);
        }
    }

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            int value = Random.Shared.Next(1, 10);

            Queue.Push(value);
            Console.WriteLine($"{Name} pushed value {value} to queue");

            if (IsLeader)
            {
                Processor?.ProcessQueue();
            }

            await Task.Delay(Random.Shared.Next(2000, 4000), token);
        }
    }
}
