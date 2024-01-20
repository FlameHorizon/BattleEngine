using FluentAssertions;

namespace BattleEngine.Driver.Tests;

public class DriverTests
{
    [Fact]
    public void Ctor_Should_SetupDriverCorrectly()
    {
        var p = new Party { Members = new[] { new Unit() } };
        var en = new Enemies { Members = new[] { new Unit() } };

        var d = new Driver(p, en);
        d.ReadyUnits.Should().BeEmpty();
    }
    
    [Fact]
    public void Tick_Should_AddUnitsToReadyUnits()
    {
        var p = new Party { Members = new[] { new Unit() } };
        var en = new Enemies { Members = new[] { new Unit() } };

        var d = new Driver(p, en);
        p.Members.First().Atb = 99;
        p.Members.First().AtbBarLength = 100;
        d.ReadyUnits.Should().BeEmpty();
        d.Tick();
        d.ReadyUnits.Should().HaveCount(1);
    }
}