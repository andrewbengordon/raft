using Raft_Console;

namespace Raft_UnitTests.DoubleVotingTests;

public class VotesAfterReboot
{
    private List<Node> nodes;
    private Guid nodeAId;
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

        nodeAId = nodes[0].Id;
        
        foreach (var node in nodes)
        {
            node.Start();
        }
    }
    
    [TearDown]
    public void TearDown()
    {
        foreach (var node in nodes)
        {
            node.Stop();
        }
    }

    [Test]
    public void NodeAMaintainsVotesAfterOtherNodesReboot()
    {
        foreach (var node in nodes.Skip(1).Take(3))
        {
            node.VoteFor(nodes[0].Id);
        }
        
        foreach (var node in nodes.Skip(1).Take(3))
        {
            node.Reboot();
        }
        
        var votesForA = nodes.Skip(1).Count(node => node.HasVotedFor(n => n == nodeAId));
        
        Assert.That(votesForA, Is.EqualTo(3));
        
    }
}