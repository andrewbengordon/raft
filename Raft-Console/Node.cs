namespace Raft_Console;

public class Node
{
    public Guid Id { get; }
    private int term = 0;
    public int Term
    {
        get => term;
        set
        {
            term = value;
            OnTermChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool isLeader = false;
    public bool IsLeader
    {
        get => isLeader;
        private set
        {
            isLeader = value;
            OnLeadershipChanged?.Invoke(this, new LeadershipChangedEventArgs { IsLeader = isLeader });
        }
    }
    public bool IsHealthy { get; set; } = true;
    private readonly List<Node> _nodes;
    private Thread? _thread;
    private bool _running = true;
    private readonly Random _random = new();
    private readonly string _logFilePath;
    private Dictionary<int, Guid> votedFor = new();

    public event EventHandler? OnTermChanged;
    public event EventHandler<LeadershipChangedEventArgs>? OnLeadershipChanged;
    public event EventHandler? OnElectionStarted;

    public Node(Guid id, List<Node> nodes)
    {
        Id = id;
        _nodes = nodes;
        _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"node_{Id}.log");
        File.WriteAllText(_logFilePath, "");
    }

    public void Start()
    {
        _running = true;
        _thread = new Thread(RunElection);
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join();
    }

    private void RunElection()
    {
        while (_running)
        {
            if (!IsHealthy) continue;

            Thread.Sleep(_random.Next(150, 300));

            if (!_running) break;

            Term++;
            
            OnElectionStarted?.Invoke(this, EventArgs.Empty);
            
            var votesReceived = 1;
            Log($"Starting election for term {Term}");

            foreach (var node in _nodes.Where(n => n != this && n.IsHealthy))
            {
                if (node.RequestVote(Id, Term))
                {
                    votesReceived++;
                }
            }

            if (votesReceived > _nodes.Count(n => n.IsHealthy) / 2)
            {
                IsLeader = true;
                Log($"Elected as leader for term {Term}");
                Thread.Sleep(1000);
            }
            else
            {
                IsLeader = false;
                Log("Lost the election");
            }
        }
    }

    public bool RequestVote(Guid candidateId, int term)
    {
        if (!IsHealthy) return false;

        if (votedFor.TryGetValue(term, out var votedCandidateId) && votedCandidateId != candidateId)
        {
            Log($"Already voted for another candidate in term {term}");
            return false;
        }

        var willVote = _random.Next(0, 2) == 0;
        if (willVote)
        {
            votedFor[term] = candidateId;
            Log($"Voted for {candidateId} in term {term}");
            return true;
        }

        return false;
    }

    private void Log(string message)
    {
        var logMessage = $"{DateTime.UtcNow}: {message}\n";
        File.AppendAllText(_logFilePath, logMessage);
    }

    public class LeadershipChangedEventArgs : EventArgs
    {
        public bool IsLeader { get; set; }
    }
}