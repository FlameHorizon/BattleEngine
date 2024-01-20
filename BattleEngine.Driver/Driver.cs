// This project uses BattleEngine to run a battle between two parties
// and implements actual game rules such as who can use which skills
// and turn orders.

namespace BattleEngine.Driver;

public class Driver
{
    private readonly Engine _engine;

    public Driver(Party party, Enemies enemies)
    {
        _engine = new Engine(party, enemies, new GameRandomProvider());
    }

    /// <summary>
    ///     Returns set of units that are ready to act.
    /// </summary>
    public IEnumerable<Unit> ReadyUnits =>
        _engine.Party.Members.Concat(_engine.Enemies.Members)
            .Where(u => u.AtbBarLength == u.Atb);

    /// <summary>
    ///     Moves the battle forward by one tick.
    /// </summary>
    public void Tick() => Tick(1);
    
    /// <summary>
    /// Moves the battle forward by <paramref name="ticks"/> ticks.
    /// </summary>
    /// <param name="ticks">Numbers of ticks.</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws when tick is lower than 1.</exception>
    public void Tick(int ticks)
    {
        if (ticks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        for (var i = 0; i < ticks; i++)
        {
            _engine.Tick();
        }
    }
}