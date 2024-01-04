namespace BattleEngine.Equipments;

public record Armor
{
    public string Name { get; set; }
    public int Def { get; set; }
    public int MagDef { get; set; }
    public int Eva { get; set; }
    public int MagEva { get; set; }
    public int Spr { get; set; }
    public int Mag { get; set; }
    public int Atk { get; set; }
}