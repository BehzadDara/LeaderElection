namespace LeaderElection;

public class QueueProcessor(string podName, QueueSimulator queue)
{
    private int Counter = 0;

    public void ProcessQueue()
    {
        while (queue.TryPop(out int value))
        {
            Counter += value;
            Console.WriteLine($"{podName} processed {value}, Counter: {Counter}");
        }
    }
}
