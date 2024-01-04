namespace BattleEngine;

public class GameRandomProvider : IRandomProvider
{
    public int Next()
    {
        return new Random().Next(1, ushort.MaxValue);
    }
}