using Raft_Console;

namespace Raft_UnitTests.ElectionTests;

public class RaftElectionTests
{
    List <Node> nodes = new List<Node>();
    
    [SetUp]
    public void Setup()
    {
        nodes = new List<Node>();
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
    public void LeaderGetsElectedWithTwoOutOfThreeNodesHealthy()
    {
        for (var i = 0; i < 3; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        nodes[2].IsHealthy = false;

        var leaderElected = new ManualResetEvent(false);
        Node electedLeader = null;

        foreach (var node in nodes)
        {
            node.OnLeadershipChanged += (sender, e) =>
            {
                if (e.IsLeader)
                {
                    electedLeader = (Node)sender;
                    leaderElected.Set();
                }
            };
        }

        foreach (var node in nodes)
        {
            node.Start();
        }

        var elected = leaderElected.WaitOne(TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {
            Assert.That(elected, Is.True, "A leader was not elected within the timeout period.");
            Assert.That(electedLeader, Is.Not.Null, "No leader was elected.");
            Assert.That(electedLeader.IsLeader, Is.True, "The elected node is not a leader.");
            Assert.That(electedLeader.IsHealthy, Is.True, "The elected leader is not healthy.");
        });
    }

    [Test]
    public void LeaderGetsElectedWithThreeOutOfFiveNodesHealthy()
    {
        for (var i = 0; i < 5; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        nodes[3].IsHealthy = false;
        nodes[4].IsHealthy = false;

        var leaderElected = new ManualResetEvent(false);
        Node electedLeader = null;

        foreach (var node in nodes)
        {
            node.OnLeadershipChanged += (sender, e) =>
            {
                if (!e.IsLeader) return;
                electedLeader = (Node)sender;
                leaderElected.Set();
            };
        }

        foreach (var node in nodes)
        {
            node.Start();
        }

        var elected = leaderElected.WaitOne(TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {

            Assert.That(elected, Is.True, "A leader was not elected within the timeout period.");
            Assert.That(electedLeader, Is.Not.Null, "No leader was elected.");
            Assert.That(electedLeader.IsLeader, Is.True, "The elected node is not a leader.");
            Assert.That(electedLeader.IsHealthy, Is.True, "The elected leader is not healthy.");
        });
    }

    [Test]
    public void LeaderNotElectedWithThreeOutOfFiveNodesUnhealthy()
    {
        for (var i = 0; i < 5; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        nodes[2].IsHealthy = false;
        nodes[3].IsHealthy = false;
        nodes[4].IsHealthy = false;

        var leaderElectionAttempted = new ManualResetEvent(false);

        foreach (var node in nodes)
        {
            node.OnElectionStarted += (sender, e) =>
            {
                leaderElectionAttempted.Set();
            };
        }

        foreach (var node in nodes)
        {
            node.Start();
        }

        Assert.That(nodes.Find(node => node.IsLeader), Is.Null, "A leader was incorrectly elected despite a majority of nodes being unhealthy.");
    }
    
    [Test]
    public void LeaderRemainsWithAllNodesHealthy()
    {
        for (var i = 0; i < 3; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        foreach (var node in nodes)
        {
            node.Start();
        }

        Thread.Sleep(2000);

        var initialLeader = nodes.FirstOrDefault(n => n.IsLeader);
        Assert.That(initialLeader, Is.Not.Null, "No initial leader was elected."); 
    }
    
    [Test]
    public void NodeCallsForElectionIfLeaderMessagesAreDelayedIndirectly()
    {
        for (var i = 0; i < 5; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        foreach (var node in nodes)
        {
            node.Start();
        }
        
        var currentLeader = nodes.FirstOrDefault(n => n.IsLeader);
        if (currentLeader != null)
        {
            currentLeader.IsHealthy = false;
        }

        var leaderElectionInitiated = new ManualResetEvent(false);
        foreach (var node in nodes.Where(n => n != currentLeader))
        {
            node.OnElectionStarted += (sender, args) => leaderElectionInitiated.Set();
        }

        bool electionCalled = leaderElectionInitiated.WaitOne(TimeSpan.FromSeconds(10));

        Assert.That(electionCalled, Is.True, "Node did not initiate an election after the leader became unhealthy.");
    }
    
    [Test]
    public void LeaderContinuesAsLeaderWhenTwoOfFiveNodesBecomeUnhealthy()
    {
        for (var i = 0; i < 5; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        foreach (var node in nodes)
        {
            node.Start();
        }

        Thread.Sleep(5000);

        var initialLeader = nodes.FirstOrDefault(n => n.IsLeader);
        Assert.That(initialLeader, Is.Not.Null, "No leader was elected initially.");

        foreach (var node in nodes.Where(n => !n.IsLeader).Take(2))
        {
            node.IsHealthy = false;
        }

        var currentLeader = nodes.FirstOrDefault(n => n.IsLeader);
        Assert.That(currentLeader, Is.EqualTo(initialLeader), "The initial leader has changed after making two nodes unhealthy.");
    }
}