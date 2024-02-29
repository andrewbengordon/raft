using Raft_Console;

namespace Raft_UnitTests.GatewayTests;

[TestFixture]
public class EventualGet
{
    private List<Node> _nodes;
    private Gateway _gateway;
    
    [SetUp]
    public void Setup()
    {
        _nodes = new List<Node>();
        for (var i = 0; i < 3; i++)
        {
            _nodes.Add(new Node(Guid.NewGuid(), _nodes));
        }
        _gateway = new Gateway(_nodes);
        
    }
    
    [Test]
    public void ShouldReturnEmptyLogEntry()
    {
        var result = _gateway.EventualGet(1);
        
        Assert.That(result, Is.EqualTo(("", -1)));
    }
    
    [Test]
    public void ShouldReturnLogEntry()
    {
        _nodes[0].AddLogEntry(1, "one");
        
        _nodes[0].Start();
        _nodes[1].Start();
        Thread.Sleep(1000);
        
        var result = _gateway.EventualGet(1);
        
        Assert.That(result, Is.EqualTo(("one", 0)));
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