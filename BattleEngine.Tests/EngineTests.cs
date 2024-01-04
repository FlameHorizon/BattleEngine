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
    public void Attack_Should_Return_Damage_With_WeaponType(WeaponType weaponType, int expected)
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
    public void Attack_Returns_NoDamage_When_IsMiss(Statuses status, bool applyToAttacker)
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
    public void Attack_Damage_Alteration_SupportingAbility(Sa ability, int damage)
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
    public void Attack_Damage_Alteration_Statuses(Statuses status, int damage, string unitName)
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

        IEnumerable<AttackResult> attackInfos = e.Magic(attacker, targets, "Fire");

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
    public void Magic_Should_NotBeHalved_When_Demi(Statuses status, bool affectsTarget)
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
        {
            target1,
            target1
        };

        IEnumerable<AttackResult> attackInfo = e.Magic(attacker, targets, "Demi");
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
    [InlineData(BattleSpeed.Fast,22, Statuses.Haste)]
    public void Tick_Should_IncreaseAtbBar(BattleSpeed bs, int expected, Statuses status)
    {
         var rnd = new MockRandomProvider(1);
         var p = new Party
         {
             Members = new[] { DefaultUnit() }
         };
 
         p.Members.First().AddStatus(status);
 
         var e = new Engine(p, new Enemies(), rnd);
         e.BattleSpeed = bs; 
         e.Tick();
        
         p.Members.First().Atb.Should().Be(expected);
         
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
}