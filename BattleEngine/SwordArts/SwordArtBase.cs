namespace BattleEngine.SwordArts;

public abstract class SwordArtBase
{
    public string Name { get; set; } = string.Empty;
    public int Power { get; set; }
    public Elements ElementalAffix { get; set; }
    public bool IgnoresReflect { get; set; } = false;
    
    public abstract void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget);
}

public class Darkside : SwordArtBase
{
    public Darkside()
    {
        Name = "Darkside";
        Power = 1;
        ElementalAffix = Elements.Shadow;
    }

    /// <summary>
    /// A Shadow element attack to one enemy. Each use costs 1/8 of Steiner's Max HP.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    /// <remarks>Ignores target's <see cref="Statuses.Protect"/> and
    /// attacker's <see cref="Statuses.Mini"/> statuses.</remarks>
    public override void UpdateDamageParts(
        ref AttackResult result, 
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int @base = (int)(Math.Floor((result.Attacker.Atk + 14) / 10.0) -
                          result.Target.Def);
        @base = Math.Max(1, @base);

        int bonus = (int)(result.Attacker.Str + rnd.Next() %
            (Math.Floor((result.Attacker.Lvl + result.Attacker.Str) / 8.0) + 1));

<<<<<<< HEAD
=======
        int damage = @base + bonus;

>>>>>>> origin/master
        result.IsHpDecreased = true;
        result.HpDecreased = (int)(result.Attacker.Hp / 8.0);
        result.Base = @base;
        result.Bonus = bonus;
    }
}