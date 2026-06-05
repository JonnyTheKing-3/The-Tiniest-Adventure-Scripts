[System.Serializable]
public struct CombatStats
{
    public float Attack;
    public float Defense;
    public float Weight;

    public static CombatStats operator +(CombatStats a, CombatStats b)
    {
        return new CombatStats
        {
            Attack = a.Attack + b.Attack,
            Defense = a.Defense + b.Defense,
            Weight = a.Weight + b.Weight
        };
    }

    public static CombatStats operator -(CombatStats a, CombatStats b)
    {
        return new CombatStats
        {
            Attack = a.Attack - b.Attack,
            Defense = a.Defense - b.Defense,
            Weight = a.Weight - b.Weight
        };
    }
}
