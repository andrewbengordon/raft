namespace Raft_Console;

public class Node
{
    public Guid Id { get; }
    private int _term = 0;
    private Dictionary<int, (string value, int logIndex)> _log = new();
    public static Node? CurrentLeader { get; private set; }

    public int Term
    {
        get => _term;
        set
        {
            _term = value;
            OnTermChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private bool _isLeader = false;
    public bool IsLeader
    {
        get => _isLeader;
        private set
        {
            _isLeader = value;
            OnLeadershipChanged?.Invoke(this, new LeadershipChangedEventArgs { IsLeader = _isLeader });
        }
    }
    public bool IsHealthy { get; set; } = true;
    private readonly List<Node> _nodes;
    private Thread? _thread;
    private bool _running = true;
    private readonly Random _random = new();
    private readonly string _logFilePath;
    private readonly Dictionary<int, Guid> _votedFor = new();

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

    public void RunElection()
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

    private bool RequestVote(Guid candidateId, int term)
    {
        if (!IsHealthy) return false;

        if (_votedFor.TryGetValue(term, out var votedCandidateId) && votedCandidateId != candidateId)
        {
            Log($"Already voted for another candidate in term {term}");
            return false;
        }

        var willVote = _random.Next(0, 2) == 0;
        if (willVote)
        {
            _votedFor[term] = candidateId;
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
    
    public void VoteFor(Guid candidateId)
    {
        _votedFor[Term] = candidateId;
    }
    
    public bool HasVotedFor(Func<Guid, bool> predicate)
    {
        return _votedFor.Any(kvp => predicate(kvp.Value));
    }
    
    public void Reboot()
    {
        Term = 0;
        IsLeader = false;
    }
    
    public void UpdateLeader(Node leader)
    {
        CurrentLeader = leader;
    }
    
    public bool TryGetLogValue(int key, out (string value, int logIndex) logEntry)
    {
        if (_log.TryGetValue(key, out logEntry))
        {
            return true;
        }
        else
        {
            logEntry = ("", -1);
            return false;
        }
    }
    
    public bool CompareVersionAndSwap(int key, string newValue, int expectedVersion, List<Node> nodes)
    {
        if (!IsLeader)
        {
            return false;
        }

        if (_log.TryGetValue(key, out var logEntry) && logEntry.logIndex == expectedVersion)
        {
            _log[key] = (newValue, logEntry.logIndex + 1);

            var successfulReplications = 1;
            foreach (var node in nodes.Where(n => n != this))
            {
                if (node.ReplicateLog(key, newValue, logEntry.logIndex + 1))
                {
                    successfulReplications++;
                }
            }

            if (successfulReplications >= nodes.Count / 2)
            {
                return true;
            }
            else
            {
                _log[key] = (logEntry.value, logEntry.logIndex);
                return false;
            }
        }

        return false;
    }
    
    private bool ReplicateLog(int key, string value, int logIndex)
    {
        if (!IsHealthy) return false;

        if (_log.TryGetValue(key, out var logEntry) && logEntry.logIndex == logIndex - 1)
        {
            _log[key] = (value, logIndex);
            return true;
        }

        return false;
    }
    
    public void AddLogEntry(int key, string value)
    {
        _log[key] = (value, _log.Count);
    }
    
    public bool CanReachConsensus()
    {
        return IsLeader;
    }

    public class LeadershipChangedEventArgs : EventArgs
    {
        public bool IsLeader { get; set; }
    }
}