namespace Raft_Console;

public class Node
{
    private readonly Guid _id;
    private readonly List<Node> _nodes;
    private Thread? _thread;
    private bool _running = true;
    private readonly Random _random = new();
    private int _votesReceived;
    private readonly string _logFilePath;

    public Node(Guid id, List<Node> nodes)
    {
        _id = id;
        _nodes = nodes;
        _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"node_{_id}.log");
        File.WriteAllText(_logFilePath, "");
    }

    public void Start()
    {
        _thread = new Thread(RunElection);
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread.Join();
    }

    private void RunElection()
    {
        while (_running)
        {
            Thread.Sleep(_random.Next(150, 300));

            if (!_running) break;

            _votesReceived = 1;
            Log("Starting election");

            foreach (var node in _nodes.Where(n => n != this))
            {
                if (node.RequestVote(_id))
                {
                    _votesReceived++;
                }
            }

            if (_votesReceived > _nodes.Count / 2)
            {
                Log("Elected as leader");
                Thread.Sleep(1000);
            }
            else
            {
                Log("Lost the election");
            }
        }
    }

    private bool RequestVote(Guid candidateId)
    {
        var willVote = _random.Next(0, 2) == 0;
        if (!willVote) return false;
        Log($"Voted for {candidateId}");
        return true;
    }

    private void Log(string message)
    {
        var logMessage = $"{DateTime.UtcNow}: {message}\n";
        File.AppendAllText(_logFilePath, logMessage);
    }
}