using BattleEngine.Equipments;
using FluentAssertions;

namespace BattleEngine.Tests;

public class EngineTests
{
    [Theory]
    [InlineData(WeaponType.Dagger, 11)]
    [InlineData(WeaponType.Sword, 11)]
    [InlineData(WeaponType.Staff, 11)]
    [InlineData(WeaponType.Rod, 11)]
    [InlineData(WeaponType.Spear, 11)]
    [InlineData(WeaponType.Claw, 11)]
    [InlineData(WeaponType.Flute, 11)]
    [InlineData(WeaponType.TheifSword, 11)]
    [InlineData(WeaponType.KnightSword, 11)]
    [InlineData(WeaponType.Hammer, 3)]
    [InlineData(WeaponType.Fork, 3)]
    [InlineData(WeaponType.Racket, 11)]
    public void Attack_Should_Return_Damage_With_WeaponType(
        WeaponType weaponType, int expected)
    {
        var rnd = new MockRandomProvider(1);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit(weaponType);
        Unit target = DefaultUnit();

        e.Attack(attacker, target).Damage.Should().Be(expected);
    }

    private static Unit DefaultUnit(WeaponType weaponType)
    {
        Unit unit = DefaultUnit();
        unit.Equipment.Weapon.WeaponType = weaponType;
        return unit;
    }

    private static Unit DefaultUnit()
    {
        return new Unit
        {
            Hp = 100,
            CurrentHp = 100,
            Str = 10,
            Lvl = 10,
            Spr = 10,
            Spd = 10,
            Mag = 10,
            EnemyType = EnemyType.Human,
            Equipment = new Equipment
            {
                Weapon = new Weapon
                {
                    Atk = 10,
                    WeaponType = WeaponType.Dagger
                },
                Armor = new Armor
                {
                    Def = 9
                }
            }
        };
    }

    [Fact]
    public void Attack_Should_Return_Damage_With_SaveTheQueen()
    {
        var rnd = new MockRandomProvider(1);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        attacker.Equipment.Weapon.WeaponType = WeaponType.KnightSword;
        attacker.Equipment.Weapon.Name = "Save The Queen";
        Unit target = DefaultUnit();

        e.Attack(attacker, target).Damage.Should().Be(121);
    }

    [Fact]
    public void Attack_Should_Return_Damage_When_IsCritical()
    {
        var rnd = new MockRandomProvider(102);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        attacker.Spr = 36;
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.IsCritical.Should().BeTrue();
        attackInfo.Damage.Should().Be(20);
    }

    [Fact]
    public void Attack_Returns_NoDamage_When_IsEvaded()
    {
        var rnd = new MockRandomProvider(1);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.Equipment.Wrist = new Wrist { Eva = 10 };

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.IsEvaded.Should().BeTrue();
        attackInfo.Damage.Should().Be(0);
    }

    [Theory]
    [InlineData(Statuses.Confuse, true)]
    [InlineData(Statuses.Darkness, true)]
    [InlineData(Statuses.Defend, false)]
    [InlineData(Statuses.Distract, false)]
    [InlineData(Statuses.Vanish, false)]
    public void Attack_Returns_NoDamage_When_IsMiss(Statuses status,
        bool applyToAttacker)
    {
        // Each status lowers the chance of hitting by 50%
        // therefore, by giving random number of 60
        // results always in miss
        var rnd = new MockRandomProvider(60);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        if (applyToAttacker)
        {
            attacker.AddStatus(status);
        }
        else
        {
            target.AddStatus(status);
        }

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.IsMiss.Should().BeTrue();
        attackInfo.Damage.Should().Be(0);
    }


    [Fact]
    public void Attack_Returns_Damage_And_IsCounterAttack()
    {
        var rnd = new MockRandomProvider(1);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.IsCounterAttack.Should().BeTrue();
        attackInfo.Damage.Should().Be(11);
    }

    [Theory]
    [InlineData(Sa.ManEater, 16)]
    [InlineData(Sa.MpAttack, 16)]
    public void Attack_Damage_Alteration_SupportingAbility(Sa ability,
        int damage)
    {
        var rnd = new MockRandomProvider(1);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        attacker.AddAbility(ability);
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.IsCounterAttack.Should().BeTrue();
        attackInfo.Damage.Should().Be(damage);
    }

    [Theory]
    [InlineData(Statuses.Trance, 16, "Zidane")]
    [InlineData(Statuses.Trance, 33, "Stainer")]
    public void Attack_Damage_Alteration_Statuses(Statuses status, int damage,
        string unitName)
    {
        var rnd = new MockRandomProvider(1);

        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        attacker.AddStatus(status);
        attacker.Name = unitName;
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(damage);
    }

    [Fact]
    public void Attack_AddStatus_Should_ApplyStatus()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();

        var weaponWithStatus = new Weapon
        {
            Atk = 10,
            Name = "Mage Masher",
            StatusAccuracy = 20,
            StatusAffix = Statuses.Silence,
            WeaponType = WeaponType.Dagger
        };

        attacker.Equipment.Weapon = weaponWithStatus;

        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.ApplicableStatuses.Should().Be(Statuses.Silence);
    }

    [Fact]
    public void Attack_InflictElemental_Should_IncreaseDamage_When_HasWeakness()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        var weaponWithElement = new Weapon
        {
            Atk = 10,
            WeaponType = WeaponType.Sword,
            ElementalAffix = Elements.Fire
        };

        attacker.Equipment.Weapon = weaponWithElement;

        Unit target = DefaultUnit();
        target.AddWeakness(Elements.Fire);

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(16);
    }

    [Fact]
    public void Attack_InflictElemental_Should_DecreaseDamage_When_Resistant()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        var weaponWithElement = new Weapon
        {
            Atk = 10,
            WeaponType = WeaponType.Sword,
            ElementalAffix = Elements.Fire
        };

        attacker.Equipment.Weapon = weaponWithElement;

        Unit target = DefaultUnit();
        target.AddResistance(Elements.Fire);

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(5);
    }

    [Fact]
    public void Attack_ElemAtk_Should_IncreaseDamage()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        var weaponWithElement = new Weapon
        {
            Atk = 10,
            WeaponType = WeaponType.Sword,
            ElementalAffix = Elements.Fire,
            ElemAtk = Elements.Fire
        };

        attacker.Equipment.Weapon = weaponWithElement;

        Unit target = DefaultUnit();
        target.AddWeakness(Elements.Fire);

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(24);
    }

    [Fact]
    public void Attack_When_Mini_Should_DealOneDamage()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);

        Unit attacker = DefaultUnit();
        attacker.AddStatus(Statuses.Mini);
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(1);
    }

    [Fact]
    public void Attack_DealNoDamage_When_TargetCanGuard()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);

        Unit attacker = DefaultUnit();
        var weaponWithElement = new Weapon
        {
            Atk = 10,
            ElementalAffix = Elements.Fire
        };
        attacker.Equipment.Weapon = weaponWithElement;

        Unit target = DefaultUnit();
        target.AddImmunities(Elements.Fire);

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(0);
    }

    [Fact]
    public void Attack_HealsTarget_When_TargetCanAbsorbAffix()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);

        Unit attacker = DefaultUnit();
        var weaponWithElement = new Weapon
        {
            Atk = 10,
            ElementalAffix = Elements.Fire,
            WeaponType = WeaponType.Sword
        };

        attacker.Equipment.Weapon = weaponWithElement;

        Unit target = DefaultUnit();
        target.AddAbsorb(Elements.Fire);

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(11);
        attackInfo.TargetAbsorbed.Should().BeTrue();
    }

    [Fact]
    public void Magic_CalculateDamage_When_Cast()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);

        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(154);
    }

    [Theory]
    [InlineData(Statuses.Shell, false)]
    [InlineData(Statuses.Mini, true)]
    public void Magic_Should_Be_Halved(Statuses status, bool affectsTarget)
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);

        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        if (affectsTarget)
        {
            attacker.AddStatus(status);
        }
        else
        {
            target.AddStatus(status);
        }

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(70);
    }

    [Fact]
    public void Magic_ShouldBe_Halved_When_MultiTarget()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);

        Unit attacker = DefaultUnit();

        Unit target1 = DefaultUnit();
        Unit target2 = DefaultUnit();
        IEnumerable<Unit> targets = new[] { target1, target2 };

        IEnumerable<AttackResult> attackInfos =
            e.Magic(attacker, targets, "Fire");

        attackInfos.Should().HaveCount(2);
        attackInfos.First().Damage.Should().Be(70);
        attackInfos.Last().Damage.Should().Be(70);
    }


    [Fact]
    public void Magic_ShouldBe_Increased_When_HasWeaknessToElement()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.AddWeakness(Elements.Fire);

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(224);
    }

    [Fact]
    public void Magic_ShouldBe_Increased_When_HasElemAtk()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        attacker.Equipment.Weapon.ElemAtk = Elements.Fire;
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(224);
    }

    [Fact]
    public void Magic_ShouldBe_Halved_When_HasResistanceToElement()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.AddResistance(Elements.Fire);

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(70);
    }

    [Fact]
    public void Magic_Should_DealZeroDamage_When_TargetCanGuard()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.AddImmunities(Elements.Fire);

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(0);
    }

    [Fact]
    public void Magic_Should_BeAbsorbed_When_TargetCanAbsorb()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.AddAbsorb(Elements.Fire);

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(154);
        attackInfo.TargetAbsorbed.Should().BeTrue();
    }

    [Theory]
    [InlineData(Statuses.Shell, false)]
    [InlineData(Statuses.Mini, true)]
    public void Magic_Should_NotBeHalved_When_Demi(Statuses status,
        bool affectsTarget)
    {
        var rnd = new MockRandomProvider(61);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target1 = DefaultUnit();
        Unit target2 = DefaultUnit();
        target1.Hp = 100;
        target2.Hp = 100;

        if (affectsTarget)
        {
            target1.AddStatus(status);
            target2.AddStatus(status);
        }
        else
        {
            attacker.AddStatus(status);
        }

        Unit[] targets =
        [
            target1,
            target1
        ];

        IEnumerable<AttackResult> attackInfo =
            e.Magic(attacker, targets, "Demi");
        attackInfo.First().Damage.Should().Be(30);
        attackInfo.First().Damage.Should().Be(30);
    }

    [Fact]
    public void Magic_Should_DealZeroDamage_When_TargetIsBoss()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.IsAi = true;
        target.IsBoss = true;

        AttackResult attackInfo = e.Magic(attacker, target, "Demi");
        attackInfo.Damage.Should().Be(0);
    }

    [Fact]
    public void Magic_Might_Miss_When_Demi()
    {
        var rnd = new MockRandomProvider(1);
        var e = new Engine(rnd);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        AttackResult attackInfo = e.Magic(attacker, target, "Demi");
        attackInfo.Damage.Should().Be(0);
        attackInfo.IsMiss.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99, false)]
    public void Escape_Should_Return_Result(int threshold, bool success)
    {
        var rnd = new MockRandomProvider(threshold);
        var p = new Party
        {
            Members = new[] { DefaultUnit() }
        };

        var en = new Enemies
        {
            Members = new[] { DefaultUnit() }
        };

        var e = new Engine(p, en, rnd);

        bool escapeResult = e.Escape();
        escapeResult.Should().Be(success);
    }

    [Fact]
    public void Engine_Should_InitializeAtbBars()
    {
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit() }
        };

        var en = new Enemies
        {
            Members = new[] { DefaultUnit() }
        };

        _ = new Engine(p, en, rnd);

        // We know, by looking at the formula,
        // that unit with 10 SPD will have AtbBarLength of 8000.
        p.Members.First().Atb.Should().Be(1);
        en.Members.First().Atb.Should().Be(1);
    }

    [Theory]
    [InlineData(BattleSpeed.Slow, 9, Statuses.None)]
    [InlineData(BattleSpeed.Slow, 6, Statuses.Slow)]
    [InlineData(BattleSpeed.Slow, 13, Statuses.Haste)]
    [InlineData(BattleSpeed.Medium, 11, Statuses.None)]
    [InlineData(BattleSpeed.Medium, 7, Statuses.Slow)]
    [InlineData(BattleSpeed.Medium, 16, Statuses.Haste)]
    [InlineData(BattleSpeed.Fast, 15, Statuses.None)]
    [InlineData(BattleSpeed.Fast, 10, Statuses.Slow)]
    [InlineData(BattleSpeed.Fast, 22, Statuses.Haste)]
    public void Tick_Should_IncreaseAtbBar(BattleSpeed bs, int expected,
        Statuses status)
    {
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit() }
        };

        p.Members.First().AddStatus(status);

        var e = new Engine(p, new Enemies(), rnd)
        {
            BattleSpeed = bs
        };
        e.Tick();

        p.Members.First().Atb.Should().Be(expected);
    }


    [Fact]
    public void Attack_Should_BeInverted_When_InIpsensCastle()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var e = new Engine(rnd)
        {
            IsInIpsensCastle = true
        };

        AttackResult attackInfo = e.Attack(attacker, target);
        attackInfo.Damage.Should().Be(451);
    }

    [Fact]
    public void Magic_Should_Reflect_If_TargetHasReflect()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddStatus(Statuses.Reflect);
        target.IsAi = true;
        target.CurrentHp = 100;

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target }
        };

        var e = new Engine(p, en, rnd);
        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Damage.Should().Be(154);
        attackInfo.IsReflected.Should().BeTrue();
        attackInfo.RefelectedTo!.Name.Should().Be("A");
        attackInfo.Attacker.Name.Should().Be("A");
        attackInfo.Target.Name.Should().Be("B");
    }

    [Fact]
    public void
        Magic_Should_Reflect_If_SomeEnemiesHaveReflect_WhileMultiTargetSpell_And_OnePlayer()
    {
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit() }
        };
        p.Members.First().Name = "A";

        var en = new Enemies
        {
            Members = new[] { DefaultUnit(), DefaultUnit() }
        };

        en.Members.First().AddStatus(Statuses.Reflect);
        en.Members.First().Name = "B";
        en.Members.First().IsAi = true;
        en.Members.Last().IsAi = true;

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> attackInfo =
            e.Magic(p.Members.First(), en.Members, "Fire");

        // One enemy should suffer damage 
        // Other one should reflect and deal full damage if there only
        // one character that to receive damage.

        AttackResult first = attackInfo.First();
        first.Damage.Should().Be(154);
        first.IsReflected.Should().BeTrue();
        first.RefelectedTo!.Name.Should().Be("A");
        first.Attacker.Name.Should().Be("A");
        first.Target.Name.Should().Be("B");

        AttackResult second = attackInfo.Last();
        second.Damage.Should().Be(70);
        second.Attacker.Should().Be(p.Members.First());
        second.Target.Should().Be(en.Members.Last());
    }

    [Fact]
    public void
        Magic_Should_Reflect_If_SomeEnemiesHaveReflect_WhileMultiTargetSpell_And_ManyPlayers()
    {
        /* Setup here is as follows:
            - Two player's units
            - Two Ai's units (one with Reflect)
            - First player's unit casts fire (multi target)
            - First Ai's unit (with Reflect) reflects spell as single target
               onto random player
            - Second Ai's unit receive damage.
        */
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit(), DefaultUnit() }
        };
        p.Members.First().Name = "A";
        p.Members.First().CurrentHp = 100;
        p.Members.Last().Name = "B";
        p.Members.Last().CurrentHp = 100;

        var en = new Enemies
        {
            Members = new[] { DefaultUnit(), DefaultUnit() }
        };

        en.Members.First().AddStatus(Statuses.Reflect);
        en.Members.First().Name = "C";
        en.Members.First().CurrentHp = 100;
        en.Members.First().IsAi = true;

        en.Members.Last().IsAi = true;

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> attackInfo =
            e.Magic(p.Members.First(), en.Members, "Fire");

        // One enemy should suffer damage 
        // Other one should reflect and deal full damage if there only
        // one character that to receive damage.

        AttackResult first = attackInfo.First();
        first.Damage.Should().Be(70);
        first.IsReflected.Should().BeTrue();

        // To whom spell is reflected should be decided randomly.
        // In current setup, with RND = 1 it will be always player B who
        // receives damage.
        first.RefelectedTo!.Name.Should().Be("B");
        first.Attacker.Name.Should().Be("A");
        first.Target.Name.Should().Be("C");

        AttackResult second = attackInfo.Last();
        second.Damage.Should().Be(70);
        second.Attacker.Should().Be(p.Members.First());
        second.Target.Should().Be(en.Members.Last());
    }

    [Fact]
    public void Magic_Should_Reflect_IfCastedOnOwnParty()
    {
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit(), DefaultUnit() }
        };
        p.Members.First().Name = "A";
        p.Members.First().CurrentHp = 100;
        p.Members.First().AddStatus(Statuses.Reflect);
        p.Members.Last().Name = "B";
        p.Members.Last().CurrentHp = 100;
        p.Members.Last().AddStatus(Statuses.Reflect);

        var en = new Enemies
        {
            Members = new[] { DefaultUnit(), DefaultUnit() }
        };

        en.Members.First().Name = "C";
        en.Members.First().CurrentHp = 100;
        en.Members.First().IsAi = true;
        en.Members.Last().Name = "D";
        en.Members.Last().CurrentHp = 100;
        en.Members.Last().IsAi = true;

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> attackInfo =
            e.Magic(p.Members.First(), p.Members, "Fire");

        // One enemy should suffer damage 
        // Other one should reflect and deal full damage if there only
        // one character that to receive damage.
        AttackResult first = attackInfo.First();
        first.Attacker.Name.Should().Be("A");
        first.Target.Name.Should().Be("A");
        first.Damage.Should().Be(70);
        first.IsReflected.Should().BeTrue();
        first.RefelectedTo!.Name.Should().Be("D");
    }

    [Fact]
    public void Magic_Should_Reflect_OnlyOnceIfBothPartiesHaveReflect()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.CurrentHp = 100;
        attacker.AddStatus(Statuses.Reflect);

        Unit target = DefaultUnit();
        target.Name = "B";
        target.CurrentHp = 100;
        target.AddStatus(Statuses.Reflect);
        target.IsAi = true;

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target }
        };

        var e = new Engine(p, en, rnd);

        AttackResult attackInfo = e.Magic(attacker, target, "Fire");
        attackInfo.Attacker.Name.Should().Be("A");
        attackInfo.Target.Name.Should().Be("B");
        attackInfo.IsReflected.Should().BeTrue();
        attackInfo.RefelectedTo!.Name.Should().Be("A");
    }

    [Fact]
    public void Magic_Should_Reflect_UpToFourTimes()
    {
        var rnd = new MockRandomProvider(1);
        Unit u1 = DefaultUnit();
        u1.Name = "A";
        u1.CurrentHp = 100;
        u1.AddStatus(Statuses.Reflect);

        Unit u2 = DefaultUnit();
        u2.Name = "B";
        u2.CurrentHp = 100;
        u2.AddStatus(Statuses.Reflect);

        Unit u3 = DefaultUnit();
        u3.Name = "C";
        u3.CurrentHp = 100;
        u3.AddStatus(Statuses.Reflect);

        Unit u4 = DefaultUnit();
        u4.Name = "D";
        u4.CurrentHp = 100;
        u4.AddStatus(Statuses.Reflect);

        Unit target = DefaultUnit();
        target.Name = "E";
        target.CurrentHp = 100;
        target.IsAi = true;

        var p = new Party
        {
            Members = new[] { u1, u2, u3, u4 }
        };

        var en = new Enemies
        {
            Members = new[] { target }
        };

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> attackInfo = e.Magic(
            p.Members.First(),
            p.Members,
            "Fire");

        attackInfo.Should().HaveCount(4);
        attackInfo.Should().AllSatisfy(x =>
        {
            x.Attacker.Name.Should().Be("A");
            x.IsReflected = true;
            x.RefelectedTo!.Name.Should().Be("E");
            x.Damage.Should().Be(154);
        });

        attackInfo.ToArray()[0].Target.Name.Should().Be("A");
        attackInfo.ToArray()[1].Target.Name.Should().Be("B");
        attackInfo.ToArray()[2].Target.Name.Should().Be("C");
        attackInfo.ToArray()[3].Target.Name.Should().Be("D");
    }

    [Fact]
    public void Magic_Should_Reflect2x_When_TargetHasStatusReflect2x()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.CurrentHp = 100;
        attacker.AddStatus(Statuses.Reflect2x);

        Unit target = DefaultUnit();
        target.CurrentHp = 100;
        target.IsAi = true;

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target }
        };

        var e = new Engine(p, en, rnd);
        AttackResult attackInfo = e.Magic(attacker, attacker, "Fire");
        attackInfo.Damage.Should().Be(308);
    }

    [Fact]
    public void TranceBar_Should_BeIncreased_When_DamageReceived()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var e = new Engine(rnd);
        AttackResult result = e.Attack(attacker, target);
        result.TranceIncrease.Should().Be(1);

        result = e.Magic(attacker, target, "Fire");
        result.TranceIncrease.Should().Be(1);
    }

    [Fact]
    public void
        Trance_Should_BeIncreased_When_DamageReceived_With_StatusHighTide()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.AddStatus(Statuses.HighTide);

        var e = new Engine(rnd);
        AttackResult result = e.Attack(attacker, target);
        result.TranceIncrease.Should().Be(target.Spr);
    }

    [Fact]
    public void Trance_Should_NotBeIncreased_When_TargetIsAi()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();
        target.IsAi = true;

        var e = new Engine(rnd);
        AttackResult result = e.Attack(attacker, target);
        result.TranceIncrease.Should().Be(0);
    }

    [Fact]
    public void Trance_Should_Decrease_When_AttackerIsInTrance_And_TakesTurn()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.AddStatus(Statuses.Trance);

        attacker.Trance = attacker.TranceBarLength;
        Unit target = DefaultUnit();
        target.IsAi = true;

        var e = new Engine(rnd);
        AttackResult result = e.Attack(attacker, target);
        result.TranceDecrease.Should().Be(34);

        result = e.Magic(attacker, target, "Fire");
        result.TranceDecrease.Should().Be(34);
    }

    [Fact]
    public void Trance_Should_NotIncreaseDamage_When_StainerWearingBloodSword()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "Stainer";
        attacker.AddStatus(Statuses.Trance);
        attacker.Trance = attacker.TranceBarLength;
        attacker.Equipment.Weapon = new Weapon
        {
            Name = "Blood Sword",
            Atk = 24,
            WeaponType = WeaponType.Sword
        };

        Unit target = DefaultUnit();
        target.IsAi = true;

        var e = new Engine(rnd);
        AttackResult result = e.Attack(attacker, target);
        result.Damage.Should().Be(240);
    }

    [Fact]
    public void Magic_Should_DrainMp_When_OsmoseCasted()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var e = new Engine(rnd);
        AttackResult result = e.Magic(attacker, target, "Osmose");

        result.Damage.Should().Be(0);
        result.IsMpRestored.Should().BeTrue();
        result.RestoredMp.Should().Be(41);
    }

    [Fact]
    public void Magic_Should_DrainHp_When_DrainCasted()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var e = new Engine(rnd);
        AttackResult result = e.Magic(attacker, target, "Drain");

        result.Damage.Should().Be(352);
        result.IsHpRestored.Should().BeTrue();
        result.HpRestored.Should().Be(352);
    }

    [Fact]
    public void Magic_Should_ReverseDrain_When_TargetIsUndead()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.EnemyType = EnemyType.Undead;

        var e = new Engine(rnd);
        AttackResult result = e.Magic(attacker, target, "Drain");

        result.Damage.Should().Be(352);
        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.IsReflected.Should().BeTrue();
        result.RefelectedTo!.Name.Should().Be("A");
        result.IsHpRestored.Should().BeTrue();
        result.HpRestored.Should().Be(352);
    }

    [Fact]
    public void Magic_Should_InflictPoison_When_BioCasted()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();

        Unit target = DefaultUnit();

        var e = new Engine(rnd);
        AttackResult result = e.Magic(attacker, target, "Bio");

        result.Damage.Should().Be(0);
        result.InflictStatus.First().Should()
            .Be((Statuses.Poison, target));
    }

    [Fact]
    public void Magic_Should_InflictPoison_When_BioCastedAsMultiTarget()
    {
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit() }
        };

        Unit en1 = DefaultUnit();
        en1.Name = "B";
        Unit en2 = DefaultUnit();
        en2.Name = "C";
        var en = new Enemies
        {
            Members = new[] { en1, en2 }
        };

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> result =
            e.Magic(p.Members.First(), en.Members, "Bio");
        result.Should().HaveCount(2);

        List<(Statuses Status, Unit Unit)> inflict =
            result.First().InflictStatus;
        inflict.First().Status.Should().Be(Statuses.Poison);
        inflict.First().Unit.Name.Should().Be(en1.Name);

        inflict = result.ToArray()[1].InflictStatus;
        inflict.First().Status.Should().Be(Statuses.Poison);
        inflict.First().Unit.Name.Should().Be(en2.Name);
    }

    [Fact]
    public void Magic_Should_DealRandomDamage_When_MeteorCasted()
    {
        // Values below 172 will miss.
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target, target }
        };

        var e = new Engine(p, en, rnd);

        IEnumerable<AttackResult> result =
            e.Magic(attacker, en.Members, "Meteor");
        result.First().IsMiss.Should().BeFalse();
        result.First().Damage.Should().Be(88);
        result.ToArray()[1].Damage.Should().Be(88);
    }

    [Fact]
    public void Magic_Should_DealRandomDamage_When_CometCasted()
    {
        var rnd = new MockRandomProvider(172);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target, target }
        };

        var e = new Engine(p, en, rnd);

        IEnumerable<AttackResult> result =
            e.Magic(attacker, en.Members, "Comet");
        result.First().IsMiss.Should().BeFalse();
        result.First().Damage.Should().Be(56 * 172);
        result.ToArray()[1].Damage.Should().Be(56 * 172);
    }

    [Theory]
    [InlineData("Meteor")]
    [InlineData("Comet")]
    public void Magic_Should_Miss_When_MeteorOrCometMisses(string spellName)
    {
        var rnd = new MockRandomProvider(1);
        var p = new Party
        {
            Members = new[] { DefaultUnit() }
        };

        var en = new Enemies
        {
            Members = new[] { DefaultUnit(), DefaultUnit() }
        };

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> result =
            e.Magic(p.Members.First(), en.Members, spellName);

        result.Should().AllSatisfy(x =>
        {
            x.IsMiss = true;
            x.Damage = 0;
        });
    }


    [Theory]
    [InlineData(Statuses.Shell, true)]
    [InlineData(Statuses.Mini, false)]
    public void Magic_Should_HalfDamage_When_MeteorCastedAndTargetHasShell(
        Statuses status, bool affectsTarget)
    {
        var rnd = new MockRandomProvider(2);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        if (affectsTarget)
        {
            target.AddStatus(status);
        }
        else
        {
            attacker.AddStatus(status);
        }

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target, target }
        };

        var e = new Engine(p, en, rnd);

        IEnumerable<AttackResult> result =
            e.Magic(attacker, en.Members, "Meteor");
        result.First().IsMiss.Should().BeFalse();
        result.First().Damage.Should().Be(88);
        result.ToArray()[1].Damage.Should().Be(88);
    }

    [Theory]
    [InlineData(Statuses.Shell, true)]
    [InlineData(Statuses.Mini, false)]
    public void Magic_Should_HalfDamage_When_CometCastedAndTargetHasShell(
        Statuses status,
        bool affectsTarget)
    {
        var rnd = new MockRandomProvider(172);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        if (affectsTarget)
        {
            target.AddStatus(status);
        }
        else
        {
            attacker.AddStatus(status);
        }

        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { target, target }
        };

        var e = new Engine(p, en, rnd);

        IEnumerable<AttackResult> result =
            e.Magic(attacker, en.Members, "Comet");
        result.First().IsMiss.Should().BeFalse();
        result.First().Damage.Should().Be(56 * 172 / 2);
        result.ToArray()[1].Damage.Should().Be(56 * 172 / 2);
    }

    [Fact]
    public void Magic_Should_IgnoreReflect_When_FlareCasted()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddStatus(Statuses.Reflect);

        var e = new Engine(rnd);
        AttackResult result = e.Magic(attacker, target, "Flare");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.IsReflected.Should().BeFalse();
        result.RefelectedTo.Should().BeNull();
        result.Damage.Should().Be(1309);
    }


    [Fact]
    public void Magic_Should_IgnoreReflect_When_DoomsdayCasted()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddStatus(Statuses.Reflect);

        var e = new Engine(rnd);
        AttackResult result = e.Magic(attacker, target, "Doomsday");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.IsReflected.Should().BeFalse();
        result.RefelectedTo.Should().BeNull();
        result.Damage.Should().Be(112 * 11);
    }

    [Fact]
    public void Focus_Should_IncreaseMagic()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();

        var e = new Engine(rnd);
        AttackResult result = e.Focus(attacker);

        result.FocusUsed.Should().BeTrue();
        result.MagIncrease.Should().Be(2);
    }

    [Theory]
    [InlineData("Rare", 256)]
    [InlineData("Semi-Rare", 16)]
    [InlineData("Uncommon", 64)]
    [InlineData("Common", 255)]
    public void Steal_Should_StealItemFromTarget(string itemStolen, int roll)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddStealableItem("Rare", 0);
        target.AddStealableItem("Semi-Rare", 1);
        target.AddStealableItem("Uncommon", 2);
        target.AddStealableItem("Common", 3);

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeTrue();
        result.ItemStolen.Should().Be(itemStolen);
        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
    }

    [Fact]
    public void Steal_Should_AlwaysPassFirstCheck_When_BanditEquipped()
    {
        // Normally, if Rnd MOD Target.Lvl
        // is greater than Rnd Mod (Source.Lvl + Source.Spr) Source  
        // fails to steal, but with Bandit this check always succeed.

        var rnd = new MockRandomProvider(256);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.Lvl = 30;
        target.AddStealableItem("Rare", 0);

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeTrue();
        result.ItemStolen.Should().Be("Rare");
        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
    }

    [Fact]
    public void Steal_Should_Fail_When_SlotIsEmpty()
    {
        // In this case, target does not have anything to steal.
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        Unit target = DefaultUnit();

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeFalse();
        result.ItemStolen.Should().BeNull();
    }

    [Fact]
    public void Steal_Should_Success_When_MasterTheifIsEquipped()
    {
        // Normally, RND = 1 should result in Semi-Rare but since
        // MasterTheif will be equipped action is going to steal Rare.
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.AddAbility(Sa.MasterTheif);

        Unit target = DefaultUnit();
        target.AddStealableItem("Rare", 0);
        target.AddStealableItem("Semi-Rare", 1);

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeTrue();
        result.ItemStolen.Should().Be("Rare");
    }

    [Fact]
    public void Steal_Should_OmitEmptySlots_When_MasterTheifIsEquipped()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.AddAbility(Sa.MasterTheif);

        Unit target = DefaultUnit();
        target.AddStealableItem("Semi-Rare", 1);

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeTrue();
        result.ItemStolen.Should().Be("Semi-Rare");
    }

    [Fact]
    public void Steal_Should_DealDamage_When_MugIsEquipped()
    {
        var rnd = new MockRandomProvider(256);
        Unit attacker = DefaultUnit();
        attacker.AddAbility(Sa.Mug);

        Unit target = DefaultUnit();
        target.AddStealableItem("Rare", 0);

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeTrue();
        result.ItemStolen.Should().Be("Rare");
        result.Damage.Should().Be(6);
    }

    [Fact]
    public void Steal_Should_StealGil_When_StealGilIsEquipped()
    {
        var rnd = new MockRandomProvider(256);
        Unit attacker = DefaultUnit();
        attacker.AddAbility(Sa.StealGil);

        Unit target = DefaultUnit();
        target.AddStealableItem("Rare", 0);

        var e = new Engine(rnd);
        AttackResult result = e.Steal(attacker, target);

        result.StealSuccess.Should().BeTrue();
        result.ItemStolen.Should().Be("Rare");
        result.Damage.Should().Be(0);
        result.StolenGil.Should().Be(22);
    }

    [Fact]
    public void Flee_Should_EscapeFromBattle_When_Used()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        var p = new Party
        {
            Members = new[] { attacker }
        };

        var en = new Enemies
        {
            Members = new[] { DefaultUnit() }
        };

        var e = new Engine(p, en, rnd);
        AttackResult result = e.Flee(p.Members.First());

        result.Attacker.Name.Should().Be("A");
        result.Escaped.Should().BeTrue();
        result.GilLost.Should().Be(0);
    }

    [Fact]
    public void Flee_Should_NotEscape_When_FightingBoss()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        var p = new Party
        {
            Members = new[] { attacker }
        };

        Unit en1 = DefaultUnit();
        en1.IsBoss = true;
        var en = new Enemies
        {
            Members = new[] { en1 }
        };

        var e = new Engine(p, en, rnd);
        AttackResult result = e.Flee(p.Members.First());

        result.Attacker.Name.Should().Be("A");
        result.Escaped.Should().BeFalse();
        result.GilLost.Should().Be(0);
    }

    [Fact]
    public void Flee_Should_DeductGil_When_Escaped()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        var p = new Party
        {
            Members = new[] { attacker }
        };

        Unit en1 = DefaultUnit();
        en1.Gil = 100;
        var en = new Enemies
        {
            Members = new[] { en1 }
        };

        var e = new Engine(p, en, rnd);
        AttackResult result = e.Flee(p.Members.First());

        result.Attacker.Name.Should().Be("A");
        result.Escaped.Should().BeTrue();
        result.GilLost.Should().Be(10);
    }


    [Fact]
    public void Detect_Should_ShowStealableItems_When_Used()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddStealableItem("Rare", 0);
        target.AddStealableItem("Semi-Rare", 1);
        target.AddStealableItem("Uncommon", 2);
        target.AddStealableItem("Common", 3);

        var e = new Engine(rnd);
        AttackResult result = e.Detect(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.StealableItems.Should()
            .ContainInOrder(["Rare", "Semi-Rare", "Uncommon", "Common"]);
    }

    [Fact]
    public void WhatsThat_Should_TurnTargetAround()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.WhatsThat(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.TurnsTargetAround.Should().BeTrue();
    }

    [Theory]
    [InlineData(WeaponType.TheifSword, 1, Statuses.Darkness)]
    [InlineData(WeaponType.Dagger, 0, Statuses.None)]
    public void SoulBlade_Should_AddStatus_When_TheifSwordEquipped(
        WeaponType weaponType,
        int inflictStatusCount,
        Statuses status)
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.Equipment.Weapon = new Weapon
        {
            WeaponType = weaponType,
            StatusAffix = status
        };

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SoulBlade(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.InflictStatus.Should().HaveCount(inflictStatusCount);
        if (inflictStatusCount <= 0)
        {
            return;
        }

        result.InflictStatus.First().Unit.Name.Should().Be("B");
        result.InflictStatus.First().Status.Should()
            .HaveFlag(status);
    }

    [Fact]
    public void SoulBlade_ShouldNot_AddStatus_When_TargetIsImmune()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.Equipment.Weapon = new Weapon
        {
            WeaponType = WeaponType.TheifSword,
            StatusAffix = Statuses.Darkness
        };

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddStatusImmune(Statuses.Darkness);

        var e = new Engine(rnd);
        AttackResult result = e.SoulBlade(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.StatusImmune.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Annoy_Should_AddStatusTrouble_When_NotResistant(bool isImmune)
    {
        var rnd = new MockRandomProvider(2);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        if (isImmune)
        {
            target.AddStatusImmune(Statuses.Trouble);
        }

        var e = new Engine(rnd);
        AttackResult result = e.Annoy(attacker, target);
        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");

        if (isImmune)
        {
            result.InflictStatus.Should().HaveCount(0);
            result.StatusImmune = true;
        }
        else
        {
            result.InflictStatus.First().Status.Should()
                .HaveFlag(Statuses.Trouble);
            result.InflictStatus.First().Unit.Name.Should().Be("B");
        }
    }

    [Fact]
    public void Sacrifice_Should_FullRestorePartyHpAndMp()
    {
        Unit a1 = DefaultUnit();
        a1.Name = "A";

        var p = new Party
        {
            Members = new[] { a1, DefaultUnit() }
        };

        var en = new Enemies
        {
            Members = new[] { DefaultUnit() }
        };

        var e = new Engine();
        AttackResult result = e.Sacrifice(p.Members.First());

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("A");
        result.Sacrificed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(1, 77)]
    [InlineData(2, 777)]
    [InlineData(3, 7777)]
    public void LuckySeven_Should_DealDamage_When_HpEndsWithSeven(int roll,
        int damage)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.CurrentHp = 7;

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.LuckySeven(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(damage);
    }

    [Fact]
    public void LuckySeven_Should_DealOneDamage_When_HpNotEndsWithSeven()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.CurrentHp = 6;

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.LuckySeven(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    public void Thievery_Should_DealDamage(int stealsCount, int damage)
    {
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.SuccessfulSteals = stealsCount;

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine();
        AttackResult result = e.Thievery(attacker, target);

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(damage);
    }

    [Fact]
    public void SwordArts_Should_DealDamage_When_DarksideUsed()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Darkside");

        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
        result.IsHpDecreased.Should().BeTrue();
        result.HpDecreased.Should().Be((int)(attacker.Hp / 8.0));
        result.Base.Should().Be(1);
        result.Bonus.Should().Be(11);
        result.Damage.Should().Be(11);
    }

    [Fact]
    public void SwordArts_Should_DealDamage_When_MinusStrikeUsed()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Minus Strike");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Base.Should().Be(target.Hp - target.CurrentHp);
        result.Bonus.Should().Be(1);
        result.Damage.Should().Be(target.Hp - target.CurrentHp);
    }


    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(5, false)]
    public void SwordArts_Should_DealDamage_When_IaiStrikeUsedAndHits(int roll,
        bool isHit)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Iai Strike");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(0);

        if (!isHit)
        {
            return;
        }

        result.InflictStatus.First().Status.Should().HaveFlag(Statuses.Death);
        result.InflictStatus.First().Unit.Name.Should().Be("B");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void SwordArts_Should_DealDamage_When_PowerBreakUsedAndHits(int roll,
        bool isHit)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Power Break");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(0);

        if (!isHit)
        {
            return;
        }

        result.IsStrReduced.Should().BeTrue();
        result.StrReduced.Should().Be(7);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void SwordArts_Should_DealDamage_When_ArmourBreakUsedAndHits(
        int roll,
        bool isHit)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Armour Break");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(0);

        if (!isHit)
        {
            return;
        }

        result.IsDefReduced.Should().BeTrue();
        result.DefReduced.Should().Be(4);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void SwordArts_Should_DealDamage_When_MentalBreakUsedAndHits(
        int roll,
        bool isHit)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";
        target.MagDef = 9;

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Mental Break");

        result.Attacker.Name.Should().Be("A");
        result.Target.Name.Should().Be("B");
        result.Damage.Should().Be(0);

        if (!isHit)
        {
            return;
        }

        result.IsMagDefReduced.Should().BeTrue();
        result.MagDefReduced.Should().Be(4);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void SwordArts_Should_DealDamage_When_MagicBreakUsedAndHits(
        int roll,
        bool isHit)
    {
        var rnd = new MockRandomProvider(roll);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Magic Break");

        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
        result.Damage.Should().Be(0);

        if (!isHit)
        {
            return;
        }

        result.IsMagReduced.Should().BeTrue();
        result.MagReduced.Should().Be(5);
    }

    [Fact]
    public void SwordArts_Should_MakeNearDeathUnitsAttack_When_ChargeUsed()
    {
        // All party members which are in critical state will attack
        // expect the one who used Charge.

        var rnd = new MockRandomProvider(0);

        Unit a1 = DefaultUnit();
        a1.Name = "A";
        Unit a2 = DefaultUnit();
        a2.Name = "B";
        a2.CurrentHp = 1;
        Unit a3 = DefaultUnit();
        a3.CurrentHp = 1;
        a3.Name = "C";

        var p = new Party
        {
            Members = new[] { a1, a2, a3 }
        };

        Unit target = DefaultUnit();
        target.Name = "D";

        var en = new Enemies
        {
            Members = new[] { target }
        };

        var e = new Engine(p, en, rnd);
        AttackResult result = e.SwordArt(a1, target, "Charge!");

        result.Attacker.Name.Should().Be("A");
        result.Target.Should().BeNull();
        result.Damage.Should().Be(0);

        result.ChargeAttack.Should().HaveCount(2);
        result.ChargeAttack.First().Attacker.Name.Should().Be("B");
        result.ChargeAttack.First().Target!.Name.Should().Be("D");
        result.ChargeAttack.ToArray()[1].Attacker.Name.Should().Be("C");
        result.ChargeAttack.ToArray()[1].Target!.Name.Should().Be("D");
    }

    [Fact]
    public void SwordArts_Should_DealDamage_When_ThunderSlashUsed()
    {
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine();
        AttackResult result = e.SwordArt(attacker, target, "Thunder Slash");

        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
        result.Damage.Should().Be(19);
    }

    [Fact]
    public void SwordArts_Should_DealDamage_When_StockBreakUsed()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Stock Break");
        result.Base.Should().Be(6);
        result.Bonus.Should().Be(11);
        result.Damage.Should().Be(66);
        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
    }

    [Fact]
    public void
        SwordArts_Should_DealDamage_WhenStockBreakUsedAndTargetIsWeakToElement()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Equipment.Weapon = new Weapon
        {
            Atk = 10,
            ElementalAffix = Elements.Ice,
            Name = "Ice Brand"
        };

        Unit target = DefaultUnit();
        target.AddWeakness(Elements.Ice);
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Stock Break");
        result.Base.Should().Be(6);
        result.Bonus.Should().Be(16);
        result.Damage.Should().Be(96);
    }

    [Fact]
    public void
        SwordArts_Should_DealDamage_WhenStockBreakUsedAndMultipleTargets()
    {
        var rnd = new MockRandomProvider(1);

        Unit a1 = DefaultUnit();
        a1.Name = "A";

        var p = new Party
        {
            Members = new[] { a1 }
        };

        Unit t1 = DefaultUnit();
        t1.Name = "B";
        Unit t2 = DefaultUnit();
        t2.Name = "C";

        var en = new Enemies
        {
            Members = new[] { t1, t2 }
        };

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> result =
            e.SwordArt(a1, en.Members, "Stock Break");
        result.Should().HaveCount(2);
        AttackResult first = result.First();
        first.Attacker.Name.Should().Be("A");
        first.Target!.Name.Should().Be("B");
        first.Base.Should().Be(6);
        first.Bonus.Should().Be(11);
        first.Damage.Should().Be(66);

        AttackResult second = result.ToArray()[1];
        second.Attacker.Name.Should().Be("A");
        second.Target!.Name.Should().Be("C");
        second.Base.Should().Be(6);
        second.Bonus.Should().Be(11);
        second.Damage.Should().Be(66);
    }

    [Fact]
    public void
        SwordArts_Should_DealDamage_WhenClimhazzardUsedAndMultipleTargets()
    {
        var rnd = new MockRandomProvider(1);

        Unit a1 = DefaultUnit();
        a1.Name = "A";

        var p = new Party
        {
            Members = new[] { a1 }
        };

        Unit t1 = DefaultUnit();
        t1.Name = "B";
        Unit t2 = DefaultUnit();
        t2.Name = "C";

        var en = new Enemies
        {
            Members = new[] { t1, t2 }
        };

        var e = new Engine(p, en, rnd);
        IEnumerable<AttackResult> result =
            e.SwordArt(a1, en.Members, "Climhazzard");
        result.Should().HaveCount(2);
        AttackResult first = result.First();
        first.Attacker.Name.Should().Be("A");
        first.Target!.Name.Should().Be("B");
        first.Base.Should().Be(20);
        first.Bonus.Should().Be(11);
        first.Damage.Should().Be(220);

        AttackResult second = result.ToArray()[1];
        second.Attacker.Name.Should().Be("A");
        second.Target!.Name.Should().Be("C");
        second.Base.Should().Be(20);
        second.Bonus.Should().Be(11);
        second.Damage.Should().Be(220);
    }

    [Fact]
    public void SwordArt_Should_DealDamage_When_ShockUsed()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordArt(attacker, target, "Shock");

        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
        result.Base.Should().Be(21);
        result.Bonus.Should().Be(11);
        result.Damage.Should().Be(231);
    }

    [Fact]
    public void SwordArt_Should_DealDamage_When_ShockUsedWithIce()
    {
        var mockRandomProvider = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";
        attacker.Equipment.Weapon = new Weapon
        {
            Atk = 10,
            ElementalAffix = Elements.Ice,
            Name = "Ice Brand"
        };

        Unit target = DefaultUnit();
        target.Name = "B";
        target.AddWeakness(Elements.Ice);

        var e = new Engine(mockRandomProvider);
        AttackResult result = e.SwordArt(attacker, target, "Shock");

        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
        result.Base.Should().Be(21);
        result.Bonus.Should().Be(16);
        result.Damage.Should().Be(21 * 16);
    }

    [Fact]
    public void SwordMagic_Should_DealDamage()
    {
        var rnd = new MockRandomProvider(1);
        Unit attacker = DefaultUnit();
        attacker.Name = "A";

        Unit target = DefaultUnit();
        target.Name = "B";

        var e = new Engine(rnd);
        AttackResult result = e.SwordMagic(attacker, target, "Fire");
        result.Attacker.Name.Should().Be("A");
        result.Target!.Name.Should().Be("B");
        result.Base.Should().Be(6);
        result.Bonus.Should().Be(11);
        result.Damage.Should().Be(66);
    }

    [Fact]
    public void SwordMagic_Should_IgnoreProtect()
    {
         var rnd = new MockRandomProvider(1);
         Unit attacker = DefaultUnit();
         attacker.Name = "A";
 
         Unit target = DefaultUnit();
         target.Name = "B";
         target.AddStatus(Statuses.Protect);
 
         var e = new Engine(rnd);
         AttackResult result = e.SwordMagic(attacker, target, "Fire");
         result.Base.Should().Be(6);
         result.Bonus.Should().Be(11);
         result.Damage.Should().Be(66);       
    }
    
    [Fact]
    public void SwordMagic_Should_NotRemoveSleepStatus()
    {
         var rnd = new MockRandomProvider(1);
         Unit attacker = DefaultUnit();
         attacker.Name = "A";
 
         Unit target = DefaultUnit();
         target.Name = "B";
         target.AddStatus(Statuses.Sleep);
 
         var e = new Engine(rnd);
         AttackResult result = e.SwordMagic(attacker, target, "Fire");
         result.Target!.Statuses.Should().HaveFlag(Statuses.Sleep);
    }

    [Fact]
    public void SwordMagic_Should_BeMultiTargetAttack_When_DoomsdayUsed()
    {
        Unit p1 = DefaultUnit();
        p1.Name = "A";
        var p = new Party
        {
            Members = new[] { p1 }
        };

        Unit en1 = DefaultUnit();
        en1.Name = "B";
        Unit en2 = DefaultUnit();
        en2.Name = "C";

        var en = new Enemies
        {
            Members = new[] { en1, en2 }
        };

        var rnd = new MockRandomProvider(1);
        var e = new Engine(p, en, rnd);
        
        IEnumerable<AttackResult> result = e.SwordMagic(p1, en.Members, "Doomsday");
        result.Should().HaveCount(2);
        result.First().Attacker.Name.Should().Be("A");
        result.First().Target!.Name.Should().Be("B");
        result.First().Base.Should().Be(41);
        result.First().Bonus.Should().Be(11);
        result.First().Damage.Should().Be(451);
        
        result.ToArray()[1].Attacker.Name.Should().Be("A");
        result.ToArray()[1].Target!.Name.Should().Be("C");
        result.ToArray()[1].Damage.Should().Be(451);
    }
    
}

public class MockRandomProvider : IRandomProvider
{
    private readonly int[] _values;
    private int _index;

    public MockRandomProvider(int value)
    {
        _values = new[] { value };
    }

    public int Next()
    {
        int value = _values[_index++];

        if (_index >= _values.Length)
        {
            _index = 0;
        }

        return value;
    }

    public int Next(int minValue, int maxValue)
    {
        int value = _values[_index++];

        if (_index >= _values.Length)
        {
            _index = 0;
        }

        return value;
    }
}