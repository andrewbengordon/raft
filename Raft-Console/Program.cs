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

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

foreach (var node in nodes)
{
    node.Stop();
}