using Raft_Console;

var nodes = new List<Node>();
for (var i = 0; i < 3; i++)
{
    nodes.Add(new Node(Guid.NewGuid(), nodes));
}

foreach (var node in nodes)
{
    node.Start();
}

Thread.Sleep(5000);

foreach (var node in nodes)
{
    node.Stop();
}