using Raft_Console;

namespace Raft_UnitTests.GatewayTests;

[TestFixture]
public class CompareVersionAndSwap
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
    public void WhenLeaderHasValueAndVersionMatches_ShouldReturnTrue()
    {
        _nodes[0].AddLogEntry(1, "one");
        _nodes[0].Start();
        Thread.Sleep(1000);
        
        var result = _gateway.CompareVersionAndSwap(1, "two", 0);
        
        Assert.That(result, Is.True);
    }
    
    [Test]
    public void WhenLeaderHasValueAndVersionDoesNotMatch_ShouldReturnFalse()
    {
        _nodes[0].AddLogEntry(1, "one");
        _nodes[0].Start();
        Thread.Sleep(1000);
        
        var result = _gateway.CompareVersionAndSwap(1, "two", 1);
        
        Assert.That(result, Is.False);
    }
    
    [Test]
    public void WhenLeaderDoesNotHaveValue_ShouldBeFalse()
    {
        _nodes[0].Start();
        Thread.Sleep(1000);
        
        var result = _gateway.CompareVersionAndSwap(1, "two", 0);
        
        Assert.That(result, Is.False);
    }
    
    [Test]
    public void WhenNoLeader_ShouldThrowException()
    {
        Assert.Throws<Exception>(() => _gateway.CompareVersionAndSwap(1, "two", 0));
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