﻿using System.Dynamic;
using System.Reflection.Metadata;
using System.Text;
using BattleEngine.Equipments;
using BattleEngine.Spells;
using BattleEngine.SwordArts;

namespace BattleEngine;

public class Engine
{
    private readonly IRandomProvider _randomProvider;

    public Engine() : this(new GameRandomProvider())
    {
    }

    public Engine(IRandomProvider randomProvider) : this(new Party(),
        new Enemies(), randomProvider)
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
        return AttackInternal(attacker, target);
    }

    private AttackResult AttackInternal(
        Unit attacker,
        Unit target,
        bool isChargeAttack = false)
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
                result.ApplicableStatuses |=
                    attacker.Equipment.Weapon.StatusAffix;
            }
        }

        result.Damage = @base * bonus;

        // Charge! attack is a special case. Target can't counter attack
        // and Trance is not increased nor decreased.
        if (isChargeAttack)
        {
            return result;
        }

        // Calculate if the target will counter attack
        bool isCounterAttack = target.Statuses.HasFlag(Statuses.Eye4Eye)
            ? target.Spr * 2 >= rnd % 100
            : target.Spr >= rnd % 100;
        result.IsCounterAttack = isCounterAttack;

        if (attacker.Statuses.HasFlag(Statuses.Trance))
        {
            result.TranceDecrease =
                (int)(Math.Floor((300.0 - attacker.Lvl) / attacker.Spr)
                    * 10.0 % 256);
        }

        if (target.IsAi == false)
        {
            result.TranceIncrease = CalculateTranceIncrease(target);
        }

        return result;
    }

    /// <summary>
    ///     Calculate the increase of Trance gauge.
    ///     Don't increase the Trance if this is a Ai controlled unit.
    /// </summary>
    /// <param name="target">Unit for which trance bar should be increased.</param>
    /// <returns></returns>
    private int CalculateTranceIncrease(Unit target)
    {
        int rnd = _randomProvider.Next();
        return target.Statuses.HasFlag(Statuses.HighTide)
            ? target.Spr
            : rnd % target.Spr;
    }

    private static int ApplyMultiplier(Unit attacker, Unit target, int bonus,
        bool isCritical)
    {
        if (target.EnemyType.HasFlag(EnemyType.Human) &&
            attacker.Abilities.HasFlag(Sa.ManEater))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (attacker.Abilities.HasFlag(Sa.MpAttack))
        {
            bonus = (int)Math.Floor(bonus * 1.5);
        }

        if (attacker.Statuses.HasFlag(Statuses.Trance))
        {
            if (attacker.Name == "Stainer"
                && attacker.Equipment.Weapon.Name != "Blood Sword")
            {
                bonus = (int)Math.Floor(bonus * 3.0);
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

            if (attacker.Equipment.HasElemAtk(attacker.Equipment.Weapon
                    .ElementalAffix))
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

    private (int @base, int bonus) CalculateAttackDamageParts(Unit attacker,
        Unit target)
    {
        int @base;
        int rnd = _randomProvider.Next();
        if (attacker.IsAi)
        {
            @base = Math.Max(1, attacker.Atk - target.Def);
            var bonus = (int)(attacker.Str +
                              rnd % (Math.Floor((attacker.Lvl + attacker.Str) /
                                                4.0) + 1));
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
        // Also Ipsen's Castle thing does not apply to AI controlled units.
        @base = IsInIpsensCastle
            ? 60 - attacker.Atk - target.Def
            : attacker.Atk - target.Def;

        if (standardWeapons.Contains(attacker.Equipment.Weapon.WeaponType))
        {
            double bonus = attacker.Str + rnd %
                (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) + 1);
            return (@base, (int)bonus);
        }

        if (attacker.Equipment.Weapon.WeaponType is WeaponType.KnightSword
            or WeaponType.TheifSword)
        {
            var b1 = (int)Math.Floor((attacker.Str + attacker.Spr) / 2.0);
            var b2 = (int)(rnd %
                           (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) +
                            1));
            int bonus = b1 + b2;
            return (@base, bonus);
        }

        if (attacker.Equipment.Weapon.WeaponType is WeaponType.Hammer
            or WeaponType.Fork)
        {
            var bonus = (int)(rnd % (1 + attacker.Str - 1) + 1 +
                              rnd % Math.Floor((attacker.Lvl + attacker.Str) /
                                               8.0));
            return (@base, bonus);
        }

        if (attacker.Equipment.Weapon.WeaponType is WeaponType.Racket)
        {
            var bonus = (int)(Math.Floor((attacker.Str + attacker.Spd) / 2.0) +
                              rnd % (Math.Floor((attacker.Lvl + attacker.Str) /
                                                8.0) + 1));
            return (@base, bonus);
        }

        return (0, 0);
    }

    /// <summary>
    ///     Allows to cast any type of Magic onto multiple targets.
    /// </summary>
    /// <param name="attacker">Source of the spell</param>
    /// <param name="targets">Targets of the spell</param>
    /// <param name="spellName">Name of the spell to cast.</param>
    /// <returns>Result of the casting magic on the targets.</returns>
    public IEnumerable<AttackResult> Magic(Unit attacker,
        IEnumerable<Unit> targets, string spellName)
    {
        return targets.Select(
            target => Magic(attacker, target, spellName, true));
    }

    /// <summary>
    ///     Allows to cast any type of Magic onto target. Spell will be casted
    ///     on a single target.
    /// </summary>
    /// <param name="attacker">Source of the spell</param>
    /// <param name="target">Target of the spell</param>
    /// <param name="spellName">Name of the spell to cast.</param>
    /// <returns>Result of the casting magic on the target.</returns>
    public AttackResult Magic(Unit attacker, Unit target, string spellName)
    {
        return Magic(attacker, target, spellName, false);
    }

    private AttackResult Magic(Unit attacker,
        Unit target,
        string spellName,
        bool isMultiTarget)
    {
        int rnd = _randomProvider.Next();
        SpellBase spell = FindSpell(spellName);
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        // Determine if target has Reflect2x status before we do any changes
        // to the AttackResult instance.
        bool targetHasReflect2X = target.Statuses.HasFlag(Statuses.Reflect2x);

        // If target has Reflect status then the spell will be
        // reflected back to someone from opposing team.
        if ((target.Statuses.HasFlag(Statuses.Reflect) &&
             spell.IgnoresReflect == false) || targetHasReflect2X)
        {
            result.IsReflected = true;
            // If target reflects spell to a party/enemy group which has only
            // one alive member then is is no longer a multi target spell
            // and such penalty can be lifted.

            int aliveCount;
            IEnumerable<Unit> group;
            if (attacker.IsAi)
            {
                group = Enemies.Members;
                aliveCount = Enemies.AliveCount;
            }
            else
            {
                // This is player attacking own units.  
                if (attacker.IsAi == false && target.IsAi == false)
                {
                    group = Enemies.Members;
                    aliveCount = Enemies.AliveCount;
                }
                else // this is Ai attacking player controllable units.
                {
                    group = Party.Members;
                    aliveCount = Party.AliveCount;
                }
            }

            Unit reflectTo;
            if (aliveCount == 1)
            {
                reflectTo = group.First();
                isMultiTarget = false;
            }
            else
            {
                int index = rnd % aliveCount;
                reflectTo = group.ToArray()[index];
            }

            result.RefelectedTo = reflectTo;
            target = reflectTo;
        }

        spell.UpdateDamageParts(ref result, _randomProvider, isMultiTarget);
        if (targetHasReflect2X)
        {
            result.Bonus *= 2;
        }

        if (result.IsMiss)
        {
            result.IsMiss = true;
            result.Damage = 0;
        }
        else if (target.IsImmuneTo(spell.ElementalAffix))
        {
            result.Damage = 0;
        }
        // When target absorbs elemental damage is should
        // be healed by the amount indicated in the damage.
        else if (target.AbsorbsElemental(spell.ElementalAffix))
        {
            result.TargetAbsorbed = true;
            result.Damage = result.Base * result.Bonus;
        }
        else
        {
            if (spell.IsHealing)
            {
                result.HpRestored = result.Base * result.Bonus;
            }
            else
            {
                result.Damage = result.Base * result.Bonus;
            }
        }

        if (attacker.Statuses.HasFlag(Statuses.Trance))
        {
            result.TranceDecrease =
                (int)(Math.Floor((300.0 - attacker.Lvl) / attacker.Spr)
                    * 10.0 % 256);
        }

        if (target.IsAi == false)
        {
            result.TranceIncrease = CalculateTranceIncrease(target);
        }

        return result;
    }

    private static SpellBase FindSpell(string spellName)
    {
        var spellNameToSpell = new Dictionary<string, SpellBase>
        {
            { "Fire", new Fire() },
            { "Fira", new Fira() },
            { "Firaga", new Firaga() },
            { "Blizzard", new Blizzard() },
            { "Blizzara", new Blizzara() },
            { "Blizzaga", new Blizzaga() },
            { "Thunder", new Thunder() },
            { "Thundara", new Thundara() },
            { "Thundaga", new Thundaga() },
            { "Demi", new Demi() },
            { "Water", new Water() },
            { "Osmose", new Osmose() },
            { "Drain", new Drain() },
            { "Bio", new Bio() },
            { "Meteor", new Meteor() },
            { "Comet", new Comet() },
            { "Flare", new Flare() },
            { "Doomsday", new Doomsday() },
            { "Holy", new Holy() },
            { "Cure", new Cure() },
            { "Cura", new Cura() },
            { "Curaga", new Curaga() },
            { "Life", new Life()},
            { "Full-Life", new FullLife()},
            { "Might", new Might()},
            { "Jewel", new Jewel()}
        };

        if (spellNameToSpell.ContainsKey(spellName) == false)
        {
            throw new ArgumentException($"Spell {spellName} does not exist.");
        }

        return spellNameToSpell[spellName];
    }


    /// <summary>
    ///     Party attempts to flee from the battle. Chance of running away is affected
    ///     by party's
    ///     average level (KO included) and the average level of the enemies (dead
    ///     monsters not included).
    /// </summary>
    /// <returns>
    ///     <c>true</c> if attempt is successful and party can run away, otherwise
    ///     <c>false</c>
    /// </returns>
    public bool Escape()
    {
        var chance = (int)Math.Floor(Party.AvgLvl *
            Math.Floor(200.0 / Enemies.AliveAvgLvl) / 16.0);
        int rnd = _randomProvider.Next();
        return rnd % 100 < chance;
    }

    /// <summary>
    ///     Initializes the battle by setting the ATB of all units according to
    ///     formula.
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
            increment = CalculateAtbIncrement(increment, u);
            u.Atb = Math.Min(u.Atb + increment, u.AtbBarLength);
        }

        foreach (Unit u in Enemies.Members)
        {
            increment = CalculateAtbIncrement(increment, u);
            u.Atb = Math.Min(u.Atb + increment, u.AtbBarLength);
        }
    }

    private static int CalculateAtbIncrement(int increment, Unit u)
    {
        if (u.Statuses.HasFlag(Statuses.Slow))
        {
            increment = (int)Math.Floor(increment * 0.66);
        }

        if (u.Statuses.HasFlag(Statuses.Haste))
        {
            increment = (int)Math.Floor(increment * 1.5);
        }

        return increment;
    }

    public AttackResult Focus(Unit unit)
    {
        var result = new AttackResult
        {
            Attacker = unit,
            Target = unit,
            FocusUsed = true,
            MagIncrease = (int)(Math.Floor(unit.Mag * 125.0 / 100.0) - unit.Mag)
        };
        return result;
    }


    /// <summary>
    ///     Steal allows you to steal one of four items from an enemy if it has any.
    ///     If Attacker has Bandit or MasterTheif supporting statuses,
    ///     the chance of stealing will be increased. Mug and StealGil will
    ///     also be calculated.
    /// </summary>
    /// <param name="attacker">Unit which will attempt to steal.</param>
    /// <param name="target">Unit on which steal will be executed</param>
    /// <returns>
    ///     <c>AttackResult</c> object which is going to store
    ///     the result of the steal.
    /// </returns>
    public AttackResult Steal(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        // Step 1: If ATK is equal or greater than DEF, steal is a success.
        int rnd = _randomProvider.Next();

        if (attacker.Abilities.HasFlag(Sa.Bandit) == false)
        {
            int atk = rnd % (attacker.Lvl + attacker.Spr);
            int def = rnd % target.Lvl;

            if (atk < def)
            {
                result.StealSuccess = false;
                return result;
            }
        }

        // Step 2: Checks to see if you get an item,
        // they're 4 possible slots to choose from.
        int roll = rnd % 256;

        int rareSlotChance = attacker.Abilities.HasFlag(Sa.MasterTheif)
            ? 32
            : 0;
        int semiRareSlotChance = attacker.Abilities.HasFlag(Sa.MasterTheif)
            ? 32
            : 16;

        int slot;
        if (attacker.Abilities.HasFlag(Sa.MasterTheif))
        {
            slot = roll switch
            {
                _ when roll <= rareSlotChance &&
                       target.StealableItems[0] != "None" => 0,
                _ when roll <= semiRareSlotChance &&
                       target.StealableItems[1] != "None" => 1,
                <= 64 when target.StealableItems[2] != "None" => 2,
                <= 255 when target.StealableItems[3] != "None" => 3,
                _ => -1
            };
        }
        else
        {
            slot = roll switch
            {
                _ when roll <= rareSlotChance => 0,
                _ when roll <= semiRareSlotChance => 1,
                <= 64 => 2,
                <= 255 => 3,
                _ => -1
            };
        }

        string itemStolen = target.StealableItems[slot];
        if (slot == -1 || itemStolen == "None")
        {
            result.StealSuccess = false;
            return result;
        }

        result.StealSuccess = true;
        result.ItemStolen = itemStolen;

        // Step 3: Deals with Mug and Steal Gil.
        if (attacker.Abilities.HasFlag(Sa.Mug))
        {
            result.Damage =
                (int)(rnd % Math.Floor(attacker.Lvl * target.Lvl / 2.0));
        }

        if (attacker.Abilities.HasFlag(Sa.StealGil))
        {
            result.StolenGil =
                (int)(rnd % (Math.Floor(attacker.Lvl * target.Lvl / 4.0) + 1));
        }

        return result;
    }

    public AttackResult Flee(Unit attacker)
    {
        var result = new AttackResult
        {
            Attacker = attacker
        };

        // Are we fighting boss right now?
        if (Enemies.Members.Any(x => x.IsBoss))
        {
            result.Escaped = false;
            return result;
        }

        result.GilLost = (int)(Enemies.TotalGoldHeld * 0.1);
        result.Escaped = true;

        return result;
    }

    public AttackResult Detect(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target,
            StealableItems = target.StealableItems
        };
        return result;
    }

    public AttackResult WhatsThat(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target,
            TurnsTargetAround = true
        };

        return result;
    }

    public AttackResult SoulBlade(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (attacker.Equipment.Weapon.WeaponType.HasFlag(WeaponType.TheifSword))
        {
            if (target.StatusImmuneTo(attacker.Equipment.Weapon.StatusAffix))
            {
                result.StatusImmune = true;
                return result;
            }

            result.InflictStatus.Add((Statuses.Darkness, target));
        }

        return result;
    }

    /// <summary>
    ///     Inflicts Trouble on one enemy. 50% of missing.
    /// </summary>
    /// <param name="attacker">Source of the attack.</param>
    /// <param name="target">Target of the attack</param>
    /// <returns>Result of the attack.</returns>
    public AttackResult Annoy(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        int roll = _randomProvider.Next() % 2;
        if (roll == 1)
        {
            result.IsMiss = true;
            return result;
        }

        if (target.StatusImmuneTo(Statuses.Trouble))
        {
            result.StatusImmune = true;
            return result;
        }

        result.InflictStatus.Add((Statuses.Trouble, target));
        return result;
    }

    public AttackResult Sacrifice(Unit attacker)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = attacker,
            Sacrificed = true
        };

        return result;
    }

    public AttackResult LuckySeven(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (attacker.CurrentHp % 10 != 7)
        {
            result.Damage = 1;
            return result;
        }

        int[] damage = [7, 77, 777, 7777];

        int roll = _randomProvider.Next(0, 3);
        result.Damage = damage[roll];
        return result;
    }

    public AttackResult Thievery(Unit attacker, Unit target)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target,
            Damage =
                (int)Math.Floor(attacker.SuccessfulSteals * attacker.Spd / 2.0)
        };

        return result;
    }

    /// <summary>
    ///     Allows to cast sword art on multiple targets.
    /// </summary>
    /// <param name="attacker">Source of the attack.</param>
    /// <param name="targets">Targets of the attack.</param>
    /// <param name="name">Name of the attack.</param>
    /// <returns>Set of result for each target.</returns>
    public IEnumerable<AttackResult> SwordArt(
        Unit attacker,
        IEnumerable<Unit> targets,
        string name)
    {
        return targets.Select(target => SwordArt(attacker, target, name, true));
    }

    public AttackResult SwordArt(
        Unit attacker,
        Unit target,
        string name,
        bool isMultiTarget = false)
    {
        // In most cases, we are not going to change
        // who is an attacker and who is a target.
        // Only places where this can happen is here or
        // inside of the SwordArtBase class.
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (name == "Charge!")
        {
            // Get a set of units which are in Near Death state (1/8 of max HP).
            IEnumerable<Unit> nearDeathUnits = attacker.IsAi
                ? Enemies.Members.Where(x => x.NearDeath)
                : Party.Members.Where(x => x.NearDeath);

            // Get a set of units which can be attacked by Charge! skill.
            IEnumerable<Unit> chargeTargets = attacker.IsAi
                ? Party.Members.Where(x => x.IsAlive)
                : Enemies.Members.Where(x => x.IsAlive);

            // For each nearDeathUnit call Attack method against random target.
            // All of them can attack different target.
            foreach (Unit unit in nearDeathUnits)
            {
                // NOTE: Based on the one video which I have watched
                // Trance is not increased by units which are under
                // influence of Charge! skill.

                // Select random target from the chargeTargets.
                int index = _randomProvider.Next(0, chargeTargets.Count());
                Unit chargeTarget = chargeTargets.ToArray()[index];
                result.ChargeAttack.Add(Attack(unit, chargeTarget));
            }

            // Since there is no single target we have to remove the Target value.
            // TODO: Sometimes there is actually one target, I have to add this case as well.
            // I think, the same target should be in ChargeAttack property
            // as well as in Target.
            result.Target = null;

            return result;
        }

        SwordArtBase swordArt = FindSwordArt(name);
        swordArt.UpdateDamageParts(ref result, _randomProvider, false);

        if (result.IsMiss)
        {
            result.IsMiss = true;
            result.Damage = 0;
        }
        else if (target.IsImmuneTo(swordArt.ElementalAffix))
        {
            result.Damage = 0;
        }

        // When target absorbs elemental damage is should
        // be healed by the amount indicated in the damage.
        else if (target.AbsorbsElemental(swordArt.ElementalAffix))
        {
            result.TargetAbsorbed = true;
            result.Damage = result.Base * result.Bonus;
        }
        else
        {
            result.Damage = result.Base * result.Bonus;
        }

        if (attacker.Statuses.HasFlag(Statuses.Trance))
        {
            result.TranceDecrease =
                (int)(Math.Floor((300.0 - attacker.Lvl) / attacker.Spr)
                    * 10.0 % 256);
        }

        if (target.IsAi == false)
        {
            result.TranceIncrease = CalculateTranceIncrease(target);
        }

        return result;
    }

    private SwordArtBase FindSwordArt(string swordArtName)
    {
        var swordArtBases = new Dictionary<string, SwordArtBase>
        {
            { "Darkside", new Darkside() },
            { "Minus Strike", new MinusStrike() },
            { "Iai Strike", new IaiStrike() },
            { "Power Break", new PowerBreak() },
            { "Armour Break", new ArmourBreak() },
            { "Mental Break", new MentalBreak() },
            { "Magic Break", new MagicBreak() },
            { "Thunder Slash", new ThunderSlash() },
            { "Stock Break", new StockBreak() },
            { "Climhazzard", new Climhazzard() },
            { "Shock", new Shock() }
        };

        if (swordArtBases.ContainsKey(swordArtName) == false)
        {
            throw new ArgumentException(
                $"Sword art {swordArtName} does not exist.");
        }

        return swordArtBases[swordArtName];
    }

    public IEnumerable<AttackResult> SwordMagic(Unit attacker,
        IEnumerable<Unit> targets, string spellName)
    {
        return targets.Select(target =>
            SwordMagic(attacker, target, spellName));
    }

    public AttackResult SwordMagic(Unit attacker, Unit target, string spellName)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        SpellBase spell = FindSpell(spellName);

        int @base = attacker.Atk + spell.SwordMagicPower - target.Def;
        int bonus = (int)(attacker.Str + _randomProvider.Next() %
            (Math.Floor((attacker.Lvl + attacker.Str) / 8.0) + 1));

        if (target.IsWeakTo(spell.ElementalAffix))
        {
            bonus += (int)(bonus * 0.5);
        }

        result.Base = @base;
        result.Bonus = bonus;
        result.Damage = @base * bonus;

        return result;
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

    public int AliveCount
    {
        get => Members.Count(u => u.IsAlive);
    }

    public int TotalGoldHeld
    {
        get => Members.Sum(m => m.Gil);
    }
}

public class Party
{
    public IEnumerable<Unit> Members { get; set; } = new List<Unit>();

    public double AvgLvl
    {
        get => Members.Average(u => u.Lvl);
    }

    public int AliveCount
    {
        get => Members.Count(u => u.IsAlive);
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
    public readonly List<(Statuses Status, Unit Unit)> InflictStatus = [];
    public bool IsCritical { get; set; }
    public bool IsMiss { get; set; }
    public bool IsEvaded { get; set; }
    public int Damage { get; set; }
    public bool IsCounterAttack { get; set; }
    public Statuses ApplicableStatuses { get; set; }
    public bool TargetAbsorbed { get; set; }
    public Unit Attacker { get; set; }

    /// <summary>
    ///     Indicates the target of the attack. Some abilities can change
    ///     the target of the attack. For example, Reflect status. In such
    ///     cases, this property will be set to the new target.
    ///     If this property is set to <c>null</c> then the attack was
    ///     multi-target and there is no single target. 
    /// </summary>
    public Unit? Target { get; set; }

    public bool IsReflected { get; set; }
    public Unit? RefelectedTo { get; set; }

    /// <summary>
    ///     Indicates the amount of which Trance bar should be increased by.
    /// </summary>
    public int TranceIncrease { get; set; }

    /// <summary>
    ///     Indicate the amount of which Trance bar should be decreased by.
    /// </summary>
    public int TranceDecrease { get; set; }

    public int Bonus { get; set; }
    public int Base { get; set; }
    public bool IsMpRestored { get; set; }
    public int RestoredMp { get; set; }
    public bool IsHpRestored { get; set; }
    public int HpRestored { get; set; }

    /// <summary>
    ///     Indicates if Focus skill was used or not.
    /// </summary>
    public bool FocusUsed { get; set; }

    /// <summary>
    ///     Increase amount of Mag stat compared to previous value.
    /// </summary>
    public int MagIncrease { get; set; }

    /// <summary>
    ///     Indicates if the steal attempt was successfully or not.
    /// </summary>
    public bool StealSuccess { get; set; }

    /// <summary>
    ///     Name of the stolen item. If it is a <c>null</c>
    ///     then no item was stolen last turn.
    /// </summary>
    public string? ItemStolen { get; set; }

    /// <summary>
    ///     Indicates the amount of Gil stolen with <c>Sa.StealGil</c>
    /// </summary>
    public int StolenGil { get; set; }

    /// <summary>
    ///     Indicates if the party can escape from the battle.
    /// </summary>
    public bool Escaped { get; set; }

    /// <summary>
    ///     Indicates the amount of Gil lost during the escape.
    /// </summary>
    public int GilLost { get; set; }

    /// <summary>
    ///     Indicates detected stealable items from the target.
    ///     Items are listed from Rare to Common.
    /// </summary>
    public string[] StealableItems { get; set; }

    /// <summary>
    ///     Indicates if the target should be turned
    ///     around allowing for back attack.
    /// </summary>
    public bool TurnsTargetAround { get; set; }

    /// <summary>
    ///     Indicates that last actions would inflict a status on target
    ///     but failed due to target's immunity.
    /// </summary>
    public bool StatusImmune { get; set; }

    /// <summary>
    ///     Indicates if the attacker sacrificed for the others.
    ///     Other party members should have Hp and Mp fully restored.
    /// </summary>
    public bool Sacrificed { get; set; }

    /// <summary>
    ///     Indicates if the attacker's Hp should be decreased
    ///     based on the previous action. Used when using attacks like Darkside.
    /// </summary>
    public bool IsHpDecreased { get; set; }

    /// <summary>
    ///     Indicates the amount of Hp decreased based on the previous actions.
    /// </summary>
    public int HpDecreased { get; set; }

    /// <summary>
    ///     Indicates if the target's Str stat should be decreased.
    /// </summary>
    public bool IsStrReduced { get; set; }

    /// <summary>
    ///     Indicates the amount of Str stat decreased.
    /// </summary>
    public int StrReduced { get; set; }

    /// <summary>
    ///     Indicates if the target's Def stat should be decreased.
    /// </summary>
    public bool IsDefReduced { get; set; }

    /// <summary>
    ///     Indicates the amount of Def stat decreased.
    /// </summary>
    public int DefReduced { get; set; }

    /// <summary>
    ///     Indicates if the target's MagDef stat should be decreased.
    /// </summary>
    public bool IsMagDefReduced { get; set; }

    /// <summary>
    ///     Indicates the amount of MagDef stat decreased.
    /// </summary>
    public int MagDefReduced { get; set; }

    /// <summary>
    ///     Indicates if the target's Mag stat should be decreased.
    /// </summary>
    public bool IsMagReduced { get; set; }

    /// <summary>
    ///     Indicates the amount of Mag stat decreased.
    /// </summary>
    public int MagReduced { get; set; }

    /// <summary>
    ///     Set of attacks which will be executed after the current one
    ///     as an effect of Charge skill.
    /// </summary>
    public List<AttackResult> ChargeAttack { get; set; } = new();

    /// <summary>
    ///     When Stock Break sword art is used, then this property
    ///     will contain a list of attack results for each target.
    /// </summary>
    public List<AttackResult> StockBreakAttack { get; set; }
    
    /// <summary>
    ///     Indicates if the target should be revived.
    /// </summary>
    public bool IsRevived { get; set; }

    /// <summary>
    ///     Indicates if the target should have Str stat increased.
    /// </summary>
    public bool IsStrIncreased { get; set; }

    /// <summary>
    ///     Indicates the amount of Str stat increased.
    /// </summary>
    public int StrIncreased { get; set; }


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
    private readonly List<string> _stealableItems =
        ["None", "None", "None", "None"];

    private int _atk;

    private int _def;

    private int _eva;
    private Elements _immunities;
    private int _mag;
    private int _magDef;

    private int _magEva;
    private Elements _resistances;
    private int _spr;
    private Statuses _statusImmune;
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
    public EnemyType EnemyType { get; set; } = EnemyType.None;

    public Sa Abilities { get; private set; }
    public string Name { get; set; } = string.Empty;

    private Elements Absorbs { get; set; }

    /// <summary>
    ///     Maximum amount of Hp unit can have.
    /// </summary>
    public int Hp { get; set; }

    /// <summary>
    ///     Maximum amount of Mp unit can have.
    /// </summary>
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

    /// <summary>
    ///     Current amount of Hp unit has.
    /// </summary>
    public int CurrentHp { get; set; }

    /// <summary>
    ///     Indicates the length of the ATB bar. Once Atb is equal to this value
    ///     unit can perform an action.
    /// </summary>
    public int AtbBarLength
    {
        get => (60 - Spd) * 160;
        set => throw new NotImplementedException();
    }

    /// <summary>
    ///     Indicates the amount of ATB unit has.
    /// </summary>
    public int Atb { get; set; }

    public int TranceBarLength
    {
        get => 255;
    }

    /// <summary>
    ///     Trance accumulates as enemies attack the character,
    ///     filling the gauge.
    /// </summary>
    public int Trance { get; set; }

    public bool IsInTrance { get; set; }

    public string[] StealableItems
    {
        get => _stealableItems.ToArray();
    }

    /// <summary>
    ///     Indicates the amount of Gil unit is storing currently.
    /// </summary>
    public int Gil { get; set; }

    /// <summary>
    ///     Count of the number of times when steal
    ///     was successfully across entire play through.
    ///     Steals done by Blank, Marcus, or Cinna count.
    ///     Steals done before you get Thievery count.
    /// </summary>
    public int SuccessfulSteals { get; set; }

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

    public void AddStealableItem(string itemName, int slot)
    {
        _stealableItems[slot] = itemName;
    }

    public bool StatusImmuneTo(Statuses status)
    {
        if (status == Statuses.None || _statusImmune == Statuses.None)
        {
            return false;
        }

        return _statusImmune.HasFlag(status);
    }

    /// <summary>
    ///     Adds immunities to specified status meaning unit can't be
    ///     inflicted by any means by this effect.
    /// </summary>
    /// <param name="status">Status to which unit will be resistant.</param>
    public void AddStatusImmune(Statuses status)
    {
        _statusImmune |= status;
    }

    /// <summary>
    ///     Indicates if unit is near death state.
    /// </summary>
    public bool NearDeath => CurrentHp <= Hp / 8.0;
}

[Flags]
public enum EnemyType
{
    None = 0,
    Human = 1,
    Normal = 2,
    Undead = 2 << 1
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
    Haste = 2 << 15,
    Reflect = 2 << 16,
    Reflect2x = 2 << 17,
    HighTide = 2 << 18,
    Poison = 2 << 19,
    Trouble = 2 << 20,
    Death = 2 << 21
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
        get => Weapon.MagDef + Head.MagDef + Wrist.MagDef + Armor.MagDef +
               Accessory.MagDef;
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
        get => Weapon.MagEva + Head.MagEva + Wrist.MagEva + Armor.MagEva +
               Accessory.MagEva;
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
    Ice = 2,
    Thunder = 2 << 2,
    Shadow = 2 << 2,
    Water = 2 << 3,
    Death = 2 << 4,
    Holy = 2 << 5
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
    int Next(int minValue, int maxValue);
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
    AddStatus = 2 << 1,
    Bandit = 2 << 2,
    MasterTheif = 2 << 3,
    Mug = 2 << 4,
    StealGil = 2 << 5
}