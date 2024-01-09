namespace BattleEngine;

public class GameRandomProvider : IRandomProvider
{
    public int Next()
    {
        return Random.Shared.Next(1, ushort.MaxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        return Random.Shared.Next(minValue, maxValue);
    }
}