using System.Text;
using BattleEngine.Equipments;

namespace BattleEngine;

public class Engine
{
    private readonly IRandomProvider _randomProvider;

    public Engine() : this(new GameRandomProvider())
    {
    }

    public Engine(IRandomProvider randomProvider) : this(new Party(), new Enemies(), randomProvider)
    {
    }

    public Engine(Party party, Enemies enemies, IRandomProvider randomProvider)
    {
        Party = party;
        Enemies = enemies;
        _randomProvider = randomProvider;

        InitializeAtb();
    }

    public Party Party { get; set; }
    public Enemies Enemies { get; set; }
    public BattleSpeed BattleSpeed { get; set; }
    public bool IsInIpsensCastle { get; set; }

    public AttackResult Attack(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        int rnd = _randomProvider.Next();

        // Calculate if the attacker has critical hit
        bool isCritical = rnd % Math.Floor(attacker.Spr / 4.0) > rnd % 100;
        result.IsCritical = isCritical;

        // Check 1: attacker has to hit a target to deal damage
        // This is where Confuse, Darkness, Defend, Distract and Vanish get
        // involved.
        var acc = 100;
        acc -= attacker.Statuses.HasFlag(Statuses.Confuse) ? acc / 2 : 0;
        acc -= attacker.Statuses.HasFlag(Statuses.Darkness) ? acc / 2 : 0;
        acc -= target.Statuses.HasFlag(Statuses.Defend) ? acc / 2 : 0;
        acc -= target.Statuses.HasFlag(Statuses.Distract) ? acc / 2 : 0;
        acc -= target.Statuses.HasFlag(Statuses.Vanish) ? acc : 0;

        // Normalize just in case we get negative value
        acc = Math.Max(acc, 0);

        result.IsMiss = rnd % 100 >= acc;
        if (result.IsMiss)
        {
            return result;
        }

        // Check 2: target can evade the attack
        result.IsEvaded = rnd % 100 < target.Eva;
        if (result.IsEvaded)
        {
            return result;
        }

        // The Sword Magic deals no damage if the enemy nullifies that element,
        // and heals the enemy if it absorbs that element.
        if (target.IsImmuneTo(attacker.Equipment.Weapon.ElementalAffix))
        {
            return result;
        }

        (int @base, int bonus) = CalculateAttackDamageParts(attacker, target);

        if (target.AbsorbsElemental(attacker.Equipment.Weapon.ElementalAffix))
        {
            result.TargetAbsorbed = true;
            result.Damage = @base * bonus;
            return result;
        }

        bonus = ApplyMultiplier(attacker, target, bonus, isCritical);
        bonus = CalculateElementalBonus(bonus, attacker, target);

        if (attacker.Statuses.HasFlag(Statuses.Mini))
        {
            bonus = 1;
        }

        if (attacker.Equipment.Weapon.CanInflictStatuses)
        {
            int accuracy = attacker.Equipment.Weapon.StatusAccuracy;
            if (accuracy > rnd % 100)
            {
                result.ApplicableStatuses |= attacker.Equipment.Weapon.StatusAffix;
            }
        }

        result.Damage = @base * bonus;

        // Calculate if the target will counter attack
        bool isCounterAttack = target.Statuses.HasFlag(Statuses.Eye4Eye)
            ? target.Spr * 2 >= rnd % 100
            : target.Spr >= rnd % 100;
        result.IsCounterAttack = isCounterAttack;

        return result;
    }

    private static int ApplyMultiplier(Unit attacker, Unit target, int bonus, bool isCritical)
    {
        if (target.EnemyType.HasFlag(EnemyType.Human) && attacker.Abilities.HasFlag(Sa.ManEater))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (attacker.Abilities.HasFlag(Sa.MpAttack))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (attacker.Statuses.HasFlag(Statuses.Trance))
        {
            if (attacker.Name == "Stainer")
            {
                bonus = attacker.Equipment.Weapon.Name != "Blood Sword"
                    ? (int)Math.Floor((double)(bonus * 3))
                    : (int)Math.Floor(bonus * 1.5);
            }
            else
            {
                bonus = (int)Math.Floor(bonus * 1.5);
            }
        }

        if (attacker.Statuses.HasFlag(Statuses.Berserk))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (target.Statuses.HasFlag(Statuses.FacingBackwards))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (target.Statuses.HasFlag(Statuses.Sleep))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (target.Statuses.HasFlag(Statuses.Mini))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (isCritical)
        {
            bonus *= 2;
        }

        if (attacker.Statuses.HasFlag(Statuses.Backrow))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        if (target.Statuses.HasFlag(Statuses.Protect))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        return bonus;
    }

    private int CalculateElementalBonus(int bonus, Unit attacker, Unit target)
    {
        // At the moment I'm limiting number of elemental affixes attacker can have
        // in any given time to 1.

        if (target.IsWeakTo(attacker.Equipment.Weapon.ElementalAffix))
        {
            bonus = (int)(bonus * 1.5);

            if (attacker.Equipment.HasElemAtk(attacker.Equipment.Weapon.ElementalAffix))
            {
                bonus = (int)(bonus * 1.5);
            }
        }

        if (target.IsResistantTo(attacker.Equipment.Weapon.ElementalAffix))
        {
            bonus /= 2;
        }

        return bonus;
    }

    private (int @base, int bonus) CalculateAttackDamageParts(Unit attacker, Unit target)
    {
        int @base;
        int rnd = _randomProvider.Next();
        if (attacker.IsAi)
        {
            @base = Math.Max(1, attacker.Atk - target.Def);
            var bonus = (int)(attacker.Str + rnd % (Math.Floor((attacker.Lvl + attacker.Str) / 4.0) + 1));
            return (@base, bonus);
        }

        var standardWeapons = new[]
        {
            WeaponType.Dagger,
            WeaponType.Sword,
            WeaponType.Staff,
            WeaponType.Rod,
            WeaponType.Spear,
            WeaponType.Claw,
            WeaponType.Flute
        };

        // Because Save The Queen is also a KnightSword we need to check it first
        // to avoid calculating the wrong damage. Beatrix.
        if (attacker.Equipment.Weapon.Name == "Save The Queen")
        {
            @base = attacker.Atk + attacker.Lvl - target.Def;
            var bonus = (int)(attacker.Str + rnd %
                (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) + 1));
            return (@base, bonus);
        }

        // Since Beatrix will never be in Ipsen's Castle we don't have to
        // invert her base damage calculation which make whole process that much easier.
        // Also Ipsen' Castle thing does not apply to AI controlled units.
        @base = IsInIpsensCastle
            ? 60 - attacker.Atk - target.Def
            : attacker.Atk - target.Def;

        if (standardWeapons.Contains(attacker.Equipment.Weapon.WeaponType))
        {
            double bonus = attacker.Str + rnd %
                (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) + 1);
            return (@base, (int)bonus);
        }

        if (attacker.Equipment.Weapon.WeaponType is WeaponType.KnightSword or WeaponType.TheifSword)
        {
            var b1 = (int)Math.Floor((attacker.Str + attacker.Spr) / 2.0);
            var b2 = (int)(rnd % (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) + 1));
            int bonus = b1 + b2;
            return (@base, bonus);
        }

        if (attacker.Equipment.Weapon.WeaponType is WeaponType.Hammer or WeaponType.Fork)
        {
            var bonus = (int)(rnd % (1 + attacker.Str - 1) + 1 +
                              rnd % Math.Floor((attacker.Lvl + attacker.Str) / 8.0));
            return (@base, bonus);
        }

        if (attacker.Equipment.Weapon.WeaponType is WeaponType.Racket)
        {
            var bonus = (int)(Math.Floor((attacker.Str + attacker.Spd) / 2.0) +
                              rnd % (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) + 1));
            return (@base, bonus);
        }

        return (0, 0);
    }

    public AttackResult Magic(Unit attacker, Unit target, string spellName)
    {
        return Magic(attacker, target, spellName, false);
    }

    private AttackResult Magic(Unit attacker, Unit target, string spellName, bool isMultiTarget)
    {
        int rnd = _randomProvider.Next();
        Spell spell = GetSpell(spellName);

        if (target.IsImmuneTo(spell.ElementalAffix))
        {
            return new AttackResult
            {
                Attacker = attacker,
                Target = target
            };
        }

        int @base = spell.Power - target.MagDef;
        var bonus = (int)(attacker.Mag + rnd % (Math.Floor((attacker.Lvl + attacker.Mag) / 8.0) + 1));

        if (target.AbsorbsElemental(spell.ElementalAffix))
        {
            return new AttackResult
            {
                Attacker = attacker,
                Target = target,
                TargetAbsorbed = true,
                Damage = @base * bonus
            };
        }

        if (spell.Name == "Demi")
        {
            // Because Demi is a special spell we need to handle it differently.
            return SpellDemi(attacker, target, rnd);
        }

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

        if (target.IsWeakTo(spell.ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (attacker.Equipment.HasElemAtk(spell.ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (target.IsResistantTo(spell.ElementalAffix))
        {
            bonus = (int)Math.Floor(bonus / 2.0);
        }

        // Bonus value never can be 0.
        bonus = Math.Max(1, bonus);

        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target,
            Damage = @base * bonus
        };

        return result;
    }

    private static AttackResult SpellDemi(Unit attacker, Unit target, int rnd)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (target.IsBoss)
        {
            return result;
        }

        if (60 > rnd % 100) // 40% chance to hit
        {
            result.IsMiss = true;
            return result;
        }

        result.Damage = (int)Math.Floor(30.0 * target.Hp / 100.0);
        return result;
    }

    private static Spell GetSpell(string spellName)
    {
        var spellNameToSpell = new Dictionary<string, Spell>
        {
            {
                "Fire", new Spell("Fire", 14, Elements.Fire)
            },
            {
                "Demi", new Spell("Demi", 30, Elements.None)
            }
        };

        if (spellNameToSpell.ContainsKey(spellName) == false)
        {
            throw new ArgumentException($"Spell {spellName} does not exist.");
        }

        return spellNameToSpell[spellName];
    }

    public IEnumerable<AttackResult> Magic(Unit attacker, IEnumerable<Unit> targets, string spellName)
    {
        return targets.Select(target => Magic(attacker, target, spellName, true));
    }

    /// <summary>
    ///     Party attempts to flee from the battle. Chance of running away is affected by party's
    ///     average level (KO included) and the average level of the enemies (dead monsters not included).
    /// </summary>
    /// <returns><c>true</c> if attempt is successful and party can run away, otherwise <c>false</c></returns>
    public bool Escape()
    {
        var chance = (int)Math.Floor(Party.AvgLvl * Math.Floor(200.0 / Enemies.AliveAvgLvl) / 16.0);
        int rnd = _randomProvider.Next();
        return rnd % 100 < chance;
    }

    /// <summary>
    ///     Initializes the battle by setting the ATB of all units according to formula.
    /// </summary>
    private void InitializeAtb()
    {
        foreach (Unit u in Party.Members)
        {
            u.Atb = CalcInitialAtb(u.AtbBarLength);
        }

        foreach (Unit u in Enemies.Members)
        {
            u.Atb = CalcInitialAtb(u.AtbBarLength);
        }
    }

    private int CalcInitialAtb(int atbBarLength)
    {
        int rnd = _randomProvider.Next();
        double floor = Math.Floor((double)(rnd / atbBarLength));
        floor = Math.Max(1, floor);
        return (int)(rnd % (floor * atbBarLength));
    }

    /// <summary>
    ///     Advances the battle by one tick increasing the ATB of all units.
    /// </summary>
    public void Tick()
    {
        int increment = BattleSpeed switch
        {
            BattleSpeed.Slow => 8,
            BattleSpeed.Medium => 10,
            BattleSpeed.Fast => 14,
            _ => throw new ArgumentOutOfRangeException()
        };

        foreach (Unit u in Party.Members)
        {
            increment = u.Statuses.HasFlag(Statuses.Slow) ? (int)Math.Floor(increment * 0.66) : increment;
            increment = u.Statuses.HasFlag(Statuses.Haste) ? (int)Math.Floor(increment * 1.5) : increment;
            u.Atb += increment;
        }

        foreach (Unit u in Enemies.Members)
        {
            u.Atb += increment;
        }
    }
}

public enum BattleSpeed
{
    Slow,
    Medium,
    Fast
}

public class Enemies
{
    public IEnumerable<Unit> Members { get; set; } = new List<Unit>();

    public double AliveAvgLvl
    {
        get => Members.Where(u => u.IsAlive).Average(u => u.Lvl);
    }
}

public class Party
{
    public IEnumerable<Unit> Members { get; set; } = new List<Unit>();

    public double AvgLvl
    {
        get => Members.Average(u => u.Lvl);
    }
}

public class Spell
{
    public Spell(string name, int power, Elements element)
    {
        Name = name;
        Power = power;
        ElementalAffix = element;
    }

    public string Name { get; set; }
    public int Power { get; set; }
    public Elements ElementalAffix { get; set; }
}

public class AttackResult
{
    public bool IsCritical { get; set; }
    public bool IsMiss { get; set; }
    public bool IsEvaded { get; set; }
    public int Damage { get; set; }
    public bool IsCounterAttack { get; set; }
    public Statuses ApplicableStatuses { get; set; }
    public bool TargetAbsorbed { get; set; }
    public Unit Attacker { get; init; }
    public Unit Target { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Is miss: {IsMiss}");
        sb.AppendLine($"Is evaded: {IsEvaded}");
        sb.AppendLine($"Damage: {Damage}");
        sb.AppendLine($"Is critical: {IsCritical}");
        sb.AppendLine($"Will target counter-attack: {IsCounterAttack}");
        sb.AppendLine($"Applicable statuses: {ApplicableStatuses}");
        sb.AppendLine($"Has target absorbed damage: {TargetAbsorbed}");

        return sb.ToString();
    }
}

public class Unit
{
    private int _atk;

    private int _def;

    private int _eva;
    private Elements _immunities;
    private int _mag;
    private int _magDef;

    private int _magEva;
    private Elements _resistances;
    private int _spr;
    private Elements _weaknesses;

    public int Atk
    {
        get => _atk + Equipment.TotalAtkBonus;
        set => _atk = value;
    }

    public int Def
    {
        get => _def + Equipment.TotalDefBonus;
        set => _def = value;
    }

    public int Str { get; set; }
    public int Lvl { get; set; }
    public Equipment Equipment { get; init; } = new();

    /// <summary>
    ///     Spirit stat of a character which is combined with equipment bonuses.
    /// </summary>
    public int Spr
    {
        get => _spr + Equipment.TotalSprBonus;
        set => _spr = value;
    }

    public int Spd { get; set; }

    public int Eva
    {
        get => _eva + Equipment.TotalEvaBonus;
        set => _eva = value;
    }

    public Statuses Statuses { get; private set; }
    public EnemyType EnemyType { get; init; } = EnemyType.None;

    public Sa Abilities { get; private set; }
    public string Name { get; set; } = string.Empty;

    private Elements Absorbs { get; set; }
    public int Hp { get; set; }
    public int Mp { get; set; }

    public int Mag
    {
        get => _mag + Equipment.TotalMagBonus;
        set => _mag = value;
    }

    public int MagDef
    {
        get => _magDef + Equipment.TotalMagDefBonus;
        set => _magDef = value;
    }

    public int MagEva
    {
        get => _magEva + Equipment.TotalMagEvaBonus;
        set => _magEva = value;
    }

    public bool IsAi { get; set; }
    public bool IsBoss { get; set; }

    public bool IsAlive
    {
        get => CurrentHp > 0;
    }

    public int CurrentHp { get; set; }

    public int AtbBarLength
    {
        get => (60 - Spd) * 160;
    }

    public int Atb { get; set; }

    public void AddStatus(Statuses status)
    {
        Statuses |= status;
    }

    public void AddAbility(Sa ability)
    {
        Abilities |= ability;
    }

    public void AddWeakness(Elements element)
    {
        _weaknesses |= element;
    }

    public bool IsWeakTo(Elements element)
    {
        if (element == Elements.None || _weaknesses == Elements.None)
        {
            return false;
        }

        return _weaknesses.HasFlag(element);
    }

    public void AddResistance(Elements element)
    {
        _resistances |= element;
    }

    public bool IsResistantTo(Elements element)
    {
        if (element == Elements.None || _resistances == Elements.None)
        {
            return false;
        }

        return _resistances.HasFlag(element);
    }

    public void AddImmunities(Elements elements)
    {
        _immunities |= elements;
    }

    public bool IsImmuneTo(Elements element)
    {
        if (element == Elements.None || _immunities == Elements.None)
        {
            return false;
        }

        return _immunities.HasFlag(element);
    }

    public void AddAbsorb(Elements element)
    {
        Absorbs |= element;
    }

    public bool AbsorbsElemental(Elements element)
    {
        if (element == Elements.None || Absorbs == Elements.None)
        {
            return false;
        }

        return Absorbs.HasFlag(element);
    }
}

[Flags]
public enum EnemyType
{
    None = 0,
    Human = 1,
    Normal = 2
}

[Flags]
public enum Statuses
{
    None = 0,
    Confuse = 1,
    Darkness = 2,
    Defend = 2 << 1,
    Distract = 2 << 2,
    Vanish = 2 << 3,
    Eye4Eye = 2 << 4,
    Trance = 2 << 5,
    Berserk = 2 << 6,
    FacingBackwards = 2 << 7,
    Sleep = 2 << 8,
    Mini = 2 << 9,
    Backrow = 2 << 10,
    Protect = 2 << 11,
    Silence = 2 << 12,
    Shell = 2 << 13,
    Slow = 2 << 14,
    Haste = 2 << 15
}

public class Equipment
{
    public Weapon Weapon { get; set; } = new();
    public Head Head { get; init; } = new();
    public Wrist Wrist { get; set; } = new();
    public Armor Armor { get; set; } = new();
    public Accessory Accessory { get; set; } = new();

    /// <summary>
    ///     Returns the total Spirit bonus from all equipment .
    /// </summary>
    public int TotalSprBonus
    {
        get
        {
            var result = 0;
            result += Weapon.Spr;
            result += Head.Spr;
            result += Wrist.Spr;
            result += Armor.Spr;
            result += Accessory.Spr;
            return result;
        }
    }

    public int TotalMagBonus
    {
        get => Weapon.Mag + Head.Mag + Wrist.Mag + Armor.Mag + Accessory.Mag;
    }

    public int TotalMagDefBonus
    {
        get => Weapon.MagDef + Head.MagDef + Wrist.MagDef + Armor.MagDef + Accessory.MagDef;
    }

    public int TotalAtkBonus
    {
        get => Weapon.Atk + Head.Atk + Wrist.Atk + Armor.Atk + Accessory.Atk;
    }

    public int TotalEvaBonus
    {
        get => Weapon.Eva + Head.Eva + Wrist.Eva + Armor.Eva + Accessory.Eva;
    }

    public int TotalMagEvaBonus
    {
        get => Weapon.MagEva + Head.MagEva + Wrist.MagEva + Armor.MagEva + Accessory.MagEva;
    }

    public int TotalDefBonus
    {
        get => Weapon.Def + Head.Def + Wrist.Def + Armor.Def + Accessory.Def;
    }

    public bool HasElemAtk(Elements element)
    {
        if (Weapon.ElemAtk == Elements.None || element == Elements.None)
        {
            return false;
        }

        return Weapon.ElemAtk.HasFlag(element);
    }
}

[Flags]
public enum Elements
{
    None = 0,
    Fire = 1,
    Ice = 2
}

public enum WeaponType
{
    None = 0,
    Dagger = 1,
    Sword = 2,
    Staff = 2 << 1,
    Rod = 2 << 2,
    Spear = 2 << 3,
    Claw = 2 << 4,
    Flute = 2 << 5,
    KnightSword = 2 << 6,
    TheifSword = 2 << 7,
    Fork = 2 << 8,
    Hammer = 2 << 9,
    Racket = 2 << 10
}

public interface IRandomProvider
{
    int Next();
}

/// <summary>
///     Support Abilities
/// </summary>
[Flags]
public enum Sa
{
    None = 0,
    ManEater = 1,
    MpAttack = 2,
    AddStatus = 2 << 1
}