namespace Raft_Console;

public class Gateway
{
    private List<Node> _nodes;
    private Random _random = new();

    public Gateway(List<Node> nodes)
    {
        _nodes = nodes;
    }

    public (string value, int logIndex) EventualGet(int key)
    {
        var nodeIndex = _random.Next(0, _nodes.Count);
        var selectedNode = _nodes[nodeIndex];

        if (selectedNode.TryGetLogValue(key, out var logEntry))
        {
            return logEntry;
        }
        else
        {
            return ("", -1);
        }
    }

    public (string value, int logIndex) StrongGet(int key)
    {
        var leader = FindLeader();
        if (leader != null)
        {
            if (leader.TryGetLogValue(key, out var logEntry))
            {
                return logEntry;
            }
            else
            {
                throw new Exception("Key not found");
            }
        }
        else
        {
            throw new Exception("No leader found");
        }
    }

    public bool CompareVersionAndSwap(int key, string value, int expectedVersion)
    {
        var leader = FindLeader();
        if (leader != null)
        {
            return leader.CompareVersionAndSwap(key, value, expectedVersion, _nodes);
        }
        else
        {
            throw new Exception("No leader found");
        }
    }

    private Node? FindLeader()
    {
        foreach (var node in _nodes)
        {
            if (node.IsLeader)
            {
                return node;
            }
        }

        return null;
    }
}
