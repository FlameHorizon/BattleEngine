// In this simulation I will be reproducing a battle between two characters:
// 1. Zidane with default equipment 
// 2. Baku

// Zidane and Baku can only use attacks.

// Setup

using BattleEngine;
using BattleEngine.Equipments;

Unit player = GetZidane();



Unit target = GetBaku();

// Initialize battle engine

var e = new Engine();

// Run battle
var i = 0;
while (i < 100)
{
    AttackResult result = e.Attack(player, target);
    Console.WriteLine($"{result.Attacker.Name} attacks {result.Target.Name} for {result.Damage} damage.");
    //Console.WriteLine(result.ToString());

    result = e.Attack(target, player);
    Console.WriteLine($"{result.Attacker.Name} attacks {result.Target.Name} for {result.Damage} damage.");

    i++;
}

return;

Unit GetBaku()
{
    var unit = new Unit
    {
        Name = "Baku",
        Lvl = 2,
        Hp = 202,
        Mp = 1285,
        Spd = 19,
        Str = 8,
        Spr = 10,
        Atk = 9,
        Mag = 8,
        MagDef = 10,
        Eva = 2,
        MagEva = 3,
        Def = 10,
        EnemyType = EnemyType.Human,
        IsAi = true
    };

    unit.AddWeakness(Elements.Fire);
    return unit;
}

Unit GetKingLeo()
{
    return new Unit
    {
        Name = "King Leo",
        Lvl = 1,
        Hp = 10186,
        Mp = 373,
        Spd = 19,
        Str = 9,
        Spr = 10,
        EnemyType = EnemyType.Human,
        Equipment = new Equipment
        {
            Weapon = new Weapon { Name = "Dummy", Atk = 8, Mag = 8, WeaponType = WeaponType.Sword },
            Head = new Head { Name = "Dummy", MagDef = 10 },
            Wrist = new Wrist { Name = "Dummy", Eva = 2, MagEva = 3 },
            Armor = new Armor { Name = "Dummy", Def = 10 }
        },
        IsAi = true
    };
}

Unit GetStainer()
{
    return new Unit
    {
        Name = "Stainer",
        Lvl = 1,
        Hp = 120,
        Mp = 24,
        Spd = 18,
        Str = 24,
        Mag = 12,
        Spr = 21,
        EnemyType = EnemyType.Human,
        Equipment = new Equipment
        {
            Weapon = new Weapon { Name = "Broadsword", Atk = 12, WeaponType = WeaponType.Sword },
            Head = new Head { Name = "Bronze Helm", MagDef = 6 },
            Wrist = new Wrist { Name = "Bronze Gloves", Spr = 1, Eva = 8, MagEva = 2 },
            Armor = new Armor { Name = "Bronze Armor", Def = 9 }
        }
    };
}

Unit GetFang()
{
    return new Unit
    {
        Name = "Fang",
        Lvl = 1,
        Hp = 68,
        Mp = 170,
        Spd = 19,
        Str = 8,
        Mag = 8,
        Spr = 10,
        EnemyType = EnemyType.Normal,
        Equipment = new Equipment
        {
            Weapon = new Weapon { Name = "Dummy", Atk = 8 },
            Armor = new Armor { Name = "Dummy", Def = 10, MagDef = 10, Eva = 2, MagEva = 3 }
        },
        IsAi = true
    };
}

Unit GetGoblin()
{
    return new Unit
    {
        Name = "Goblin",
        Lvl = 5,
        Hp = 33,
        Mp = 172,
        Spd = 19,
        Str = 8,
        Mag = 8,
        Spr = 10,
        EnemyType = EnemyType.Normal,
        Equipment = new Equipment
        {
            Weapon = new Weapon { Name = "Dummy", Atk = 8 },
            Armor = new Armor { Name = "Dummy", Def = 10, MagDef = 10, Eva = 2, MagEva = 3 }
        },
        IsAi = true
    };
}

Unit GetVivi()
{
    return new Unit
    {
        Name = "Vivi",
        Hp = 60,
        Mp = 48,
        Spd = 16,
        Str = 12,
        Mag = 24,
        Spr = 19,
        Equipment = new Equipment
        {
            Weapon = GetWeapon("Mage Staff"),
            Head = GetHead("Leather Hat"),
            Wrist = null!,
            Armor = GetArmor("Leather Shirt"),
            Accessory = null!
        }
    };
}

static Weapon GetWeapon(string name)
{
    return name switch
    {
        "Dagger" => new Weapon { Name = "Dagger", Atk = 12, WeaponType = WeaponType.Dagger },
        "Mage Staff" => new Weapon { Name = "Mage Staff", Atk = 12, WeaponType = WeaponType.Staff },
        "Mage Masher" => new Weapon
        {
            Name = "Mage Masher", Atk = 14, WeaponType = WeaponType.Dagger,
            StatusAffix = Statuses.Silence, StatusAccuracy = 20
        },
        _ => throw new ArgumentException($"Weapon {name} not found.")
    };
}

static Head GetHead(string name)
{
    return name switch
    {
        "Leather Hat" => new Head { Name = "Leather Hat", MagDef = 6 },
        _ => throw new ArgumentException($"Head {name} not found.")
    };
}

static Armor GetArmor(string name)
{
    return name switch
    {
        "Leather Shirt" => new Armor { Name = "Leather Shirt", Def = 6 },
        _ => throw new ArgumentException($"Armor {name} not found.")
    };
}

Unit GetZidane()
{
    return new Unit
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
            Weapon = GetWeapon("Dagger"),
            Head = GetHead("Leather Hat"),
            Wrist = new Wrist { Name = "Wrist", Eva = 5, MagEva = 3 },
            Armor = GetArmor("Leather Shirt")
        }
    };
}

Unit GetFlan()
{
    var u = new Unit
    {
        Name = "Flan",
        Lvl = 2,
        Hp = 75,
        Mp = 183,
        Spd = 17,
        Str = 8,
        Mag = 8,
        Spr = 10,
        Atk = 9,
        Def = 10,
        MagDef = 1,
        Eva = 2,
        MagEva = 3,
        EnemyType = EnemyType.Normal,
        IsAi = true
    };
    
    u.AddWeakness(Elements.Fire);
    u.AddResistance(Elements.Ice);

    return u;
}