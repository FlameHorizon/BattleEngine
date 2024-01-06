namespace BattleEngine.Spells;

public abstract class SpellBase
{
    public string Name { get; set; } = string.Empty;
    public int Power { get; set; }
    public Elements ElementalAffix { get; set; }

    public virtual (int bonus, bool isMiss) CalculateBonus(
        Unit attacker, 
        Unit target,
        bool isMultiTarget, 
        int rnd)
    {
        var bonus = (int)(attacker.Mag +
                          rnd % (Math.Floor((attacker.Lvl + attacker.Mag) /
                                            8.0) + 1));
        if (isMultiTarget)
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (target.Statuses.HasFlag(Statuses.Shell))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (attacker.Statuses.HasFlag(Statuses.Mini))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (target.IsWeakTo(ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (attacker.Equipment.HasElemAtk(ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (target.IsResistantTo(ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        // Bonus value never can be 0.
        bonus = Math.Max(1, bonus);
        return (bonus, false);
    }
}

public class Fire : SpellBase
{
    public Fire()
    {
        Name = "Fire";
        Power = 14;
        ElementalAffix = Elements.Fire;
    }
}

public class Demi : SpellBase
{
    public override (int bonus, bool isMiss) CalculateBonus(
        Unit attacker, 
        Unit target,
        bool isMultiTarget, 
        int rnd)
    {
        if (target.IsBoss)
        {
            return (0, false);
        }

        return 60 > rnd % 100 // 40% chance to hit
            ?  (0, true) 
            : ((int)Math.Floor(30.0 * target.Hp / 100.0), false);
    }
}