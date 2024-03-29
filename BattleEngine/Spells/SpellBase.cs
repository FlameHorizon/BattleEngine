﻿namespace BattleEngine.Spells;

public abstract class SpellBase
{
    public string Name { get; set; } = string.Empty;
    public int Power { get; set; }
    public Elements ElementalAffix { get; set; }
    public bool IgnoresReflect { get; set; } = false;
    public int SwordMagicPower { get; set; } = 0;
    
    /// <summary>
    ///     Indicates if the spell is healing.
    /// </summary>
    public bool IsHealing { get; set; }

    public virtual void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int @base = Power - result.Target!.MagDef;
        @base = Math.Max(1, @base);
        result.Base = @base;

        double floor =
            Math.Floor((result.Attacker.Lvl + result.Attacker.Mag) / 8.0);
        var bonus = (int)(result.Attacker.Mag + rnd.Next() % (floor + 1));

        if (isMultiTarget)
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (result.Target.Statuses.HasFlag(Statuses.Shell))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (result.Attacker.Statuses.HasFlag(Statuses.Mini))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (result.Target.IsWeakTo(ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (result.Attacker.Equipment.HasElemAtk(ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (result.Target.IsResistantTo(ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        // Bonus value never can be 0.
        bonus = Math.Max(1, bonus);
        result.Bonus = bonus;
    }
}

public class Fire : SpellBase
{
    public Fire()
    {
        Name = GetType().Name;
        Power = 14;
        ElementalAffix = Elements.Fire;
        SwordMagicPower = 5;
    }
}

public class Fira : SpellBase
{
    public Fira()
    {
        Name = GetType().Name;
        Power = 29;
        ElementalAffix = Elements.Fire;
        SwordMagicPower = 10;
    }
}

public class Firaga : SpellBase
{
    public Firaga()
    {
        Name = GetType().Name;
        Power = 72;
        ElementalAffix = Elements.Fire;
        SwordMagicPower = 30;
    }
}

public class Blizzard : SpellBase
{
    public Blizzard()
    {
        Name = GetType().Name;
        Power = 14;
        ElementalAffix = Elements.Ice;
        SwordMagicPower = 5;
    }
}

public class Blizzara : SpellBase
{
    public Blizzara()
    {
        Name = GetType().Name;
        Power = 29;
        ElementalAffix = Elements.Ice;
        SwordMagicPower = 10;
    }
}

public class Blizzaga : SpellBase
{
    public Blizzaga()
    {
        Name = GetType().Name;
        Power = 72;
        ElementalAffix = Elements.Fire;
        SwordMagicPower = 30;
    }
}

public class Thunder : SpellBase
{
    public Thunder()
    {
        Name = GetType().Name;
        Power = 14;
        ElementalAffix = Elements.Thunder;
        SwordMagicPower = 5;
    }
}

public class Thundara : SpellBase
{
    public Thundara()
    {
        Name = GetType().Name;
        Power = 29;
        ElementalAffix = Elements.Thunder;
        SwordMagicPower = 10;
    }
}

public class Thundaga : SpellBase
{
    public Thundaga()
    {
        Name = GetType().Name;
        Power = 72;
        ElementalAffix = Elements.Thunder;
        SwordMagicPower = 30;
    }
}

public class Osmose : SpellBase
{
    public Osmose()
    {
        Name = GetType().Name;
        Power = 15;
        ElementalAffix = Elements.None;
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        Unit a = result.Attacker;
        Unit t = result.Target;
        int @base = Power - t.MagDef;
        var bonus =
            (int)(a.Mag + rnd.Next() % (Math.Floor((a.Lvl + a.Mag) / 8.0) + 1));

        result.IsMpRestored = true;
        result.RestoredMp = (int)Math.Floor(@base * bonus / 4.0);
    }
}

public class Drain : SpellBase
{
    public Drain()
    {
        Name = GetType().Name;
        Power = 32;
        ElementalAffix = Elements.None;
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        Unit a = result.Attacker;
        Unit t = result.Target;
        int @base = Power - t.MagDef;
        var bonus =
            (int)(a.Mag + rnd.Next() % (Math.Floor((a.Lvl + a.Mag) / 8.0) + 1));

        if (t.Statuses.HasFlag(Statuses.Shell))
        {
            bonus /= 2;
        }

        if (a.Statuses.HasFlag(Statuses.Mini))
        {
            bonus /= 2;
        }

        if (t.EnemyType.HasFlag(EnemyType.Undead))
        {
            // Spell's effect is reversed: Vivi loses HP while the enemy gains it.
            result.IsReflected = true;
            result.RefelectedTo = a;
        }

        result.IsHpRestored = true;
        result.HpRestored = @base * bonus;

        result.Base = @base;
        bonus = Math.Max(1, bonus);
        result.Bonus = bonus;
    }
}

public class Demi : SpellBase
{
    public Demi()
    {
        Name = GetType().Name;
        Power = 30;
        ElementalAffix = Elements.None;
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        if (result.Target.IsBoss)
        {
            result.IsMiss = true;
        }

        if (60 > rnd.Next() % 100)
        {
            result.IsMiss = true;
        }

        result.Bonus = 1;
        result.Base = (int)Math.Floor(30.0 * result.Target.Hp / 100.0);
    }
}

public class Bio : SpellBase
{
    public Bio()
    {
        Name = GetType().Name;
        Power = 42;
        ElementalAffix = Elements.None;
        SwordMagicPower = 20;
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        bool success = 20 > rnd.Next() % 100;

        if (success)
        {
            result.InflictStatus.Add((Statuses.Poison, result.Target));
        }
    }
}

public class Meteor : SpellBase
{
    public Meteor()
    {
        Name = GetType().Name;
        Power = 88;
        ElementalAffix = Elements.None;
    }

    public override void UpdateDamageParts(ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int @base = Power;
        int bonus = rnd.Next(1, result.Attacker.Lvl + result.Attacker.Mag - 1);
        if (result.Target.Statuses.HasFlag(Statuses.Shell))
        {
            bonus /= 2;
        }

        if (result.Attacker.Statuses.HasFlag(Statuses.Mini))
        {
            bonus /= 2;
        }

        result.IsMiss =
            !(Math.Floor(result.Attacker.Lvl / 2.0) + result.Attacker.Spr >=
              rnd.Next() % 100);

        result.Base = @base;
        result.Bonus = bonus;
        result.Damage = @base * bonus;
    }
}

public class Comet : SpellBase
{
    public Comet()
    {
        Name = GetType().Name;
        Power = 56;
        ElementalAffix = Elements.None;
    }

    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int @base = Power;
        int bonus = rnd.Next(1, result.Attacker.Lvl + result.Attacker.Mag - 1);

        if (result.Target.Statuses.HasFlag(Statuses.Shell))
        {
            bonus /= 2;
        }

        if (result.Attacker.Statuses.HasFlag(Statuses.Mini))
        {
            bonus /= 2;
        }

        result.IsMiss = 171 > rnd.Next() % 256;

        result.Base = @base;
        result.Bonus = bonus;
        result.Damage = @base * bonus;
    }
}

public class Flare : SpellBase
{
    public Flare()
    {
        Name = GetType().Name;
        Power = 119;
        ElementalAffix = Elements.None;
        IgnoresReflect = true;
        SwordMagicPower = 60;
    }
}

public class Doomsday : SpellBase
{
    public Doomsday()
    {
        Name = GetType().Name;
        Power = 112;
        ElementalAffix = Elements.Shadow;
        IgnoresReflect = true;
        SwordMagicPower = 40;
    }
}

public class Water : SpellBase
{
    public Water()
    {
        Name = GetType().Name;
        Power = 64;
        ElementalAffix = Elements.Water;
        SwordMagicPower = 25;
    }
}

public class Holy : SpellBase
{
    public Holy()
    {
        Name = GetType().Name;
        Power = 113;
        ElementalAffix = Elements.Holy;
    }
}

public class Cure : SpellBase
{
    public Cure()
    {
        Name = GetType().Name;
        Power = 16;
        ElementalAffix = Elements.None;
        IsHealing = true;
    }

    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        // By calling this here we will get right value for bonus.
        // We will need to recalculate base value.
        base.UpdateDamageParts(ref result, rnd, isMultiTarget);
        result.Base = Power;
        result.IsHpRestored = true;
    }
}

public class Cura : SpellBase
{
    public Cura()
    {
        Name = GetType().Name;
        Power = 38;
        ElementalAffix = Elements.None;
        IsHealing = true;
    }

    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
       base.UpdateDamageParts(ref result, rnd, isMultiTarget);
        result.Base = Power;
        result.IsHpRestored = true;
    }
}

public class Curaga : SpellBase
{
    public Curaga()
    {
        Name = GetType().Name;
        Power = 107;
        ElementalAffix = Elements.None;
        IsHealing = true;
    }

    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
       base.UpdateDamageParts(ref result, rnd, isMultiTarget);
        result.Base = Power;
        result.IsHpRestored = true;
    }
}

public class Life : SpellBase
{
    public Life()
    {
        Name = GetType().Name;
        Power = 0;
        ElementalAffix = Elements.None;
        IsHealing = true;
    }
    
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        result.Base =(int)Math.Floor(((result.Target.Spr + 5) * result.Target.Hp) / 100.0);
        result.Bonus = 1;
        result.IsHpRestored = true;
        result.IsRevived = true;
    }
}

public class FullLife: SpellBase
{
    public FullLife()
    {
        Name = GetType().Name;
        Power = 0;
        ElementalAffix = Elements.None;
        IsHealing = true;
    }
    
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        result.Base =(int)Math.Floor(((result.Target.Spr + 100) * result.Target.Hp) / 100.0);
        result.Bonus = 1;
        result.IsHpRestored = true;
        result.IsRevived = true;
    }
}


public class Might : SpellBase
{
    public Might()
    {
        Name = GetType().Name;
    }
    
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        result.IsStrIncreased= true;
        result.StrIncreased = (int)Math.Floor((result.Target.Str * 125) / 100.0);
    }
}

public class Jewel : SpellBase
{
    public Jewel()
    {
        Name = GetType().Name;
    }
    
    public override void UpdateDamageParts(
        ref AttackResult result,
        IRandomProvider rnd,
        bool isMultiTarget)
    {
        int roll = rnd.Next(1, 3);
        if (roll == 1)
        {
            result.StealSuccess= true;
            result.ItemStolen = "Ore";
            return;
        }
        
        result.StealSuccess = false;
    }
}