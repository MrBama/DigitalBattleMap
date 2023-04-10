namespace DigitalBattleMap.DataClasses
{
    public static class TabIndex
    {
        public const int Background = 0;
        public const int Grid = 1;
        public const int Drawing = 2;
        public const int Tokens = 3;
    }

    public enum ArrowDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum TokenSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Gargantuan
    }

    public enum Condition
    {
        Baned,
        Blessed,
        Blinded,
        Charmed,
        Concentration,
        Deafened,
        Exhausted,
        Flying,
        Frightened,
        Grappled,
        Hasted,
        Hex,
        Highlighted,
        Incapacitated,
        Invisible,
        Mark,
        Paralyzed,
        Petrified,
        Poisoned,
        Prone,
        Restrained,
        Stabilized,
        Stunned,
        Unconcious
    }

    public enum ZLevelDirection
    {
        Back,
        Front
    }
}
