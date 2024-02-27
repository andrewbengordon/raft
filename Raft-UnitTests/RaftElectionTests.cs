using Raft_Console;

namespace Raft_UnitTests;

public class RaftElectionTests
{
    [Test]
    public void LeaderGetsElectedWithTwoOutOfThreeNodesHealthy()
    {
        var nodes = new List<Node>();
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
        foreach (var node in nodes)
        {
            node.Stop();
        }
    }

    [Test]
    public void LeaderGetsElectedWithThreeOutOfFiveNodesHealthy()
    {
        var nodes = new List<Node>();
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

        foreach (var node in nodes)
        {
            node.Stop();
        }
    }

    [Test]
    public void LeaderNotElectedWithThreeOutOfFiveNodesUnhealthy()
    {
        var nodes = new List<Node>();
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

        foreach (var node in nodes)
        {
            node.Stop();
        }
    }
    
    [Test]
    public void LeaderRemainsWithAllNodesHealthy()
    {
        var nodes = new List<Node>();
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

        Thread.Sleep(5000);

        var currentLeader = nodes.FirstOrDefault(n => n.IsLeader);
        Assert.That(currentLeader, Is.EqualTo(initialLeader), "The leader has changed despite all nodes remaining healthy.");

        foreach (var node in nodes)
        {
            node.Stop();
        }
    }
    
    [Test]
    public void NodeCallsForElectionIfLeaderMessagesAreDelayedIndirectly()
    {
        var nodes = new List<Node>();
        for (var i = 0; i < 5; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        foreach (var node in nodes)
        {
            node.Start();
        }

        Thread.Sleep(5000);

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

        Assert.IsTrue(electionCalled, "Node did not initiate an election after the leader became unhealthy.");

        foreach (var node in nodes)
        {
            node.Stop();
        }
    }
    
    [Test]
    public void LeaderContinuesAsLeaderWhenTwoOfFiveNodesBecomeUnhealthy()
    {
        var nodes = new List<Node>();
        for (var i = 0; i < 5; i++)
        {
            nodes.Add(new Node(Guid.NewGuid(), nodes));
        }

        // Start all nodes to initiate the election process.
        foreach (var node in nodes)
        {
            node.Start();
        }

        Thread.Sleep(5000);

        var initialLeader = nodes.FirstOrDefault(n => n.IsLeader);
        Assert.IsNotNull(initialLeader, "No leader was elected initially.");

        foreach (var node in nodes.Where(n => !n.IsLeader).Take(2))
        {
            node.IsHealthy = false;
        }

        Thread.Sleep(5000);
        var currentLeader = nodes.FirstOrDefault(n => n.IsLeader);
        Assert.AreEqual(initialLeader, currentLeader, "The initial leader has changed after making two nodes unhealthy.");

        foreach (var node in nodes)
        {
            node.Stop();
        }
    }
    
    
}