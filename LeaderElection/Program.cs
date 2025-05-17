using LeaderElection;

var queue = new QueueSimulator();
var cts = new CancellationTokenSource();

var pods = new List<Pod>
{
    new("Pod-A", queue),
    new("Pod-B", queue),
    new("Pod-C", queue),
};

var tasks = pods.Select(p => p.RunAsync(cts.Token)).ToList();

await Task.Delay(TimeSpan.FromSeconds(30));
cts.Cancel();

await Task.WhenAll(tasks);