using Raft_Console;

namespace Raft_UnitTests.DoubleVotingTests;

public class NoVotesAfterReboot
{
    private List<Node> nodes;
    private Guid nodeEId;
    private const int totalNodes = 5;
    
    [SetUp]
    public void SetUp()
    {
        nodes = [];
        for (var i = 0; i < totalNodes; i++)
        {
            var newNode = new Node(Guid.NewGuid(), nodes);
            nodes.Add(newNode);
        }

        nodeEId = nodes[4].Id;
        
        foreach (var node in nodes)
        {
            node.Start();
        }
    }
    
    [Test]
    public void NodeEHasNoVotesAfterReboot()
    {
        foreach (var node in nodes.Skip(1).Take(3))
        {
            node.VoteFor(nodes[0].Id);
        }
        
        nodes[4].Reboot();
        
        var votesForE = nodes.Skip(1).Count(node => node.HasVotedFor(n => n == nodeEId));
        
        Assert.That(votesForE, Is.EqualTo(0));
        
    }
}