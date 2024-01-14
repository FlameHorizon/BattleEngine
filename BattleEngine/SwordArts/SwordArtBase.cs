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
    ///     A Shadow element attack to one enemy. Each use costs 1/8 of Steiner's Max HP.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    /// <remarks>
    ///     Ignores target's <see cref="Statuses.Protect" /> and
    ///     attacker's <see cref="Statuses.Mini" /> statuses.
    /// </remarks>
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        var @base = (int)(Math.Floor((result.Attacker.Atk + 14) / 10.0) -
                          result.Target.Def);
        @base = Math.Max(1, @base);

        var bonus = (int)(result.Attacker.Str + rnd.Next() %
            (Math.Floor((result.Attacker.Lvl + result.Attacker.Str) / 8.0) +
             1));

        result.IsHpDecreased = true;
        result.HpDecreased = (int)(result.Attacker.Hp / 8.0);
        result.Base = @base;
        result.Bonus = bonus;
    }
}

public class MinusStrike : SwordArtBase
{
    public MinusStrike()
    {
        Name = "Minus Strike";
        Power = 0;
        ElementalAffix = Elements.None;
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        result.Base = result.Target.Hp - result.Target.CurrentHp;
        result.Bonus = 1;
    }
}

public class IaiStrike : SwordArtBase
{
    public IaiStrike()
    {
        Name = "Iai Strike";
        Power = 0;
        ElementalAffix = Elements.Death;
    }

    /// <summary>
    ///     Causes Death to one enemy.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    /// <remarks>30% Accuracy.</remarks>
    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int roll = rnd.Next(1, 11);
        if (roll > 3)
        {
            result.IsMiss = true;
            return;
        }

        result.InflictStatus.Add((Statuses.Death, result.Target));
    }
}

public class PowerBreak : SwordArtBase
{
    public PowerBreak()
    {
        Name = "Power Break";
        Power = 0;
        ElementalAffix = Elements.None;
    }

    /// <summary>
    ///     Reduces the Strength of one enemy.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    /// <remarks>50% Accuracy. Can be used repeatedly to lower the stat even more.</remarks>
    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int roll = rnd.Next(1, 3);
        if (roll == 2)
        {
            return;
        }

        result.IsStrReduced = true;
        result.StrReduced = (int)Math.Floor(result.Target.Str * 75.0 / 100.0);
    }
}

public class ArmourBreak : SwordArtBase
{
    public ArmourBreak()
    {
        Name = "Armour Break";
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int roll = rnd.Next(1, 3);
        if (roll == 2)
        {
            return;
        }

        result.IsDefReduced = true;
        result.DefReduced = (int)(result.Target.Def / 2.0);
    }
}

public class MentalBreak : SwordArtBase
{
    public MentalBreak()
    {
        Name = "Mental Break";
    }

    /// <summary>
    ///     Reduces the Magic Defense of one enemy.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    /// <remarks>50% Accuracy. Can be used repeatedly to lower the stat even more.</remarks>
    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int roll = rnd.Next(1, 3);
        if (roll == 2)
        {
            return;
        }

        result.IsMagDefReduced = true;
        result.MagDefReduced = (int)(result.Target.MagDef / 2.0);
    }
}

public class MagicBreak : SwordArtBase
{
    public MagicBreak()
    {
        Name = "MagicBreak";
    }

    /// <summary>
    ///     Reduces the Magic of one enemy.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    /// <remarks>50% Accuracy. Can be used repeatedly to lower the stat even more.</remarks>
    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int roll = rnd.Next(1, 3);
        if (roll == 2)
        {
            return;
        }

        result.IsMagReduced = true;
        result.MagReduced = (int)(result.Target.Mag / 2.0);
    }
}

public class ThunderSlash : SwordArtBase
{
    public ThunderSlash()
    {
        Name = "Thunder Slash";
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        result.Base = (int)Math.Floor(19 * result.Target!.Hp / 100.0);
        result.Bonus = 1;
    }
}

public class StockBreak : SwordArtBase
{
    public StockBreak()
    {
        Name = "Stock Break";
    }

    /// <summary>
    /// When used via Swd Art or Seiken, Stock Break does 150% the user's normal
    /// Attack damage to all enemies, taking into account the element of the user's weapon.
    /// It cannot be empowered with Elem-Atk gear.
    /// Damage is halved by the enemy being under Protect,
    /// and the damage is minuscule when Steiner has Mini.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int @base = (int)(Math.Floor((result.Attacker.Atk * 15.0) / 10.0) -
                          result.Target!.Def);
        var bonus = (int)(result.Attacker.Str + rnd.Next() %
            (Math.Floor((result.Attacker.Lvl + result.Attacker.Str) / 8.0) +
             1));

        if (result.Target.IsWeakTo(result.Attacker.Equipment.Weapon
                .ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (result.Target.Statuses.HasFlag(Statuses.Protect))
        {
            bonus /= 2;
        }

        if (result.Attacker.Statuses.HasFlag(Statuses.Mini))
        {
            @base = 1;
            bonus = 1;
        }

        result.Base = @base;
        result.Bonus = bonus;
    }
}

public class Climhazzard : SwordArtBase
{
    public Climhazzard()
    {
        Name = "Climhazzard";
    }

    /// <summary>
    ///     A Magical attack to all enemies.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="rnd"></param>
    /// <param name="isMultiTarget"></param>
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        Unit a = result.Attacker;
        Unit t = result.Target!;

        int @base = a.Atk * 2 - t.MagDef;
        int bonus =
            (int)(a.Str + rnd.Next() % (Math.Floor((a.Lvl + a.Str) / 8.0) + 1));

        if (t.Statuses.HasFlag(Statuses.Shell))
        {
            bonus /= 2;
        }

        if (a.Statuses.HasFlag(Statuses.Mini))
        {
            bonus /= 2;
        }

        result.Base = @base;
        result.Bonus = bonus;
    }
}

public class Shock : SwordArtBase
{
    public Shock()
    {
        Name = "Shock";
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        Unit a = result.Attacker;
        Unit t = result.Target!;

        int @base = a.Atk * 3 - t.Def;
        int bonus =
            (int)(a.Str + rnd.Next() % (Math.Floor((a.Lvl + a.Str) / 8.0) + 1));

        if (result.Target!.IsWeakTo(result.Attacker.Equipment.Weapon
                .ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (t.Statuses.HasFlag(Statuses.Protect))
        {
            bonus /= 2;
        }

        if (a.Statuses.HasFlag(Statuses.Mini))
        {
            @base = 1;
            bonus = 1;
        }

        result.Base = @base;
        result.Bonus = bonus;
    }
}