using Moq;
using Raft_Console;

namespace Raft_UnitTests.GatewayTests;

[TestFixture]
public class StrongGet
{
    private Gateway _gateway;
    private List<Node> _nodes;

    [SetUp]
    public void SetUp()
    {
        _nodes = new List<Node>();
        for (var i = 0; i < 3; i++)
        {
            _nodes.Add(new Node(Guid.NewGuid(), _nodes));
        }
        _gateway = new Gateway(_nodes);
    }
    
    [Test]
    public void WhenLeaderHasValue_ShouldReturnLogEntry()
    {
        _nodes[0].AddLogEntry(1, "one");
        _nodes[0].Start();
        Thread.Sleep(1000);
        
        var result = _gateway.StrongGet(1);
        
        Assert.That(result, Is.EqualTo(("one", 0)));
    }
    
    [Test]
    public void WhenLeaderDoesNotHaveValue_ShouldThrowException()
    {
        _nodes[0].Start();
        Thread.Sleep(1000);
        
        Assert.Throws<Exception>(() => _gateway.StrongGet(1));
    }
    
    [Test]
    public void WhenNoLeader_ShouldThrowException()
    {
        Assert.Throws<Exception>(() => _gateway.StrongGet(1));
    }
    
    [TearDown]
    public void TearDown()
    {
        foreach (var node in _nodes)
        {
            node.Stop();
        }
    }
}