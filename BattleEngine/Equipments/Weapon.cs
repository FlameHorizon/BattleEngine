namespace BattleEngine.Equipments;

public class Weapon
{
    private readonly int _statusAccuracy;
    public int Atk { get; init; }
    public WeaponType WeaponType { get; set; }
    public string Name { get; set; }

    public bool CanInflictStatuses
    {
        get => StatusAffix != Statuses.None;
    }

    /// <summary>
    ///     Indicates the accuracy of the status effect. Value has to be between 0 and 100.
    /// </summary>
    public int StatusAccuracy
    {
        get => _statusAccuracy;
        init
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value has to be between 0 and 100.");
            }

            _statusAccuracy = value;
        }
    }

    /// <summary>
    ///     Indicates the status effect that can be inflicted by the weapon.
    /// </summary>
    public Statuses StatusAffix { get; init; }

    /// <summary>
    ///     Indicates the elemental affinity of the weapon.
    /// </summary>
    public Elements ElementalAffix { get; init; } = Elements.None;
    
    /// <summary>
    ///     Indicates if this weapon has Elem-Atk of particular element.
    /// </summary>
    public Elements ElemAtk { get; set; } = Elements.None;
    
    public int Mag { get; set; }
    public int Spr { get; set; }
    public int MagDef { get; set; }
    public int Eva { get; set; }
    public int MagEva { get; set; }
    public int Def { get; set; }
}