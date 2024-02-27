using Raft_Console;

namespace Raft_UnitTests.NetworkPartitionTests;

public class SingleLeaderAfterPartition
{
    private List<Node> allNodes;
    private List<Node> partition1;
    private List<Node> partition2;

    [SetUp]
    public void SetUp()
    {
        allNodes = new List<Node>();
        for (var i = 0; i < 5; i++)
        {
            var newNode = new Node(Guid.NewGuid(), allNodes);
            allNodes.Add(newNode);
        }

        partition1 = allNodes.GetRange(0, 2);
        partition2 = allNodes.GetRange(2, 3);
    }
    
    [TearDown]
    public void TearDown()
    {
        foreach (var node in allNodes)
        {
            node.Stop();
        }
    }
    
    [Test]
    public void EnsureSingleLeaderAfterPartitionHeals()
    {
        foreach (var node in allNodes)
        {
            node.Start();
        }
        
        partition1.ForEach(n => n.Stop());
        
        Thread.Sleep(5000);
        
        var leader = partition2.First(n => n.IsLeader);
        
        Assert.That(leader, Is.Not.Null);
    }
}