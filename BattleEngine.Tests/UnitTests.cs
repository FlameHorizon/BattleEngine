using FluentAssertions;
using BattleEngine.Equipments;

namespace BattleEngine.Tests;

public class UnitTests
{
    [Fact]
    public void Unit_Should_CalculateStats_With_Equipment()
    {
        var unit = new Unit
        {
            Name = "Zidane",
            Lvl = 1,
            Hp = 105,
            Mp = 36,
            Spd = 23,
            Str = 21,
            Mag = 18,
            Spr = 23,
            Equipment = new Equipment
            {
                Weapon = new Weapon { Name = "Dagger", Atk = 12, WeaponType = WeaponType.Dagger },
                Head = new Head { Name = "Leather Hat", MagDef = 6 },
                Wrist = new Wrist { Name = "Wrist", Eva = 5, MagEva = 3 },
                Armor = new Armor { Name = "Leather Shirt", Def = 6 },
                Accessory = new Accessory { Name = "Moonstone" }
            }
        };

        unit.Atk.Should().Be(12);
        unit.Def.Should().Be(6);
        unit.Eva.Should().Be(5);
        unit.MagDef.Should().Be(6);
        unit.MagEva.Should().Be(3);
    }


    [Fact]
    public void IsWeakTo_ReturnFalse_When_ElementIsNone()
    {
        var u = new Unit();
        u.AddWeakness(Elements.Fire);
        u.IsWeakTo(Elements.None).Should().BeFalse();
    }
    
    [Fact]
    public void Spirit_Returns_SumOfBaseSprAndEquipmentSpr()
    {
        var u = new Unit
        {
            Spr = 10,
            Equipment = new Equipment { Armor = new Armor { Spr = 5 } }
        };
        u.Spr.Should().Be(15);
    }
    
    [Fact]
    public void SetSpirit_ChangesOnlyBasedValueOfSpirit()
    {
        var u = new Unit
        {
            Spr = 10,
            Equipment = new Equipment { Armor = new Armor { Spr = 5 } }
        };

        u.Spr.Should().Be(15);
        
        u.Spr = 5;
        u.Spr.Should().Be(10);
    }

    [Fact]
    public void Unit_Should_HaveAtbBarLength_WhenSpdIsSet()
    {
        var u = new Unit
        {
            Spd = 10
        };
        
        u.AtbBarLength.Should().Be(8000);
    }
}