namespace DigitalBattleMap.DataClasses
{
    public class ScreenPosition
    {
        public int X { get; set; }

        public int Y { get; set; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ScreenPosition other && other.X == X && other.Y == Y;
        }

        public override string ToString()
        {
            return $"X: {X} Y: {Y}";
        }
    }
}
