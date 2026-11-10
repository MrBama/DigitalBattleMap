using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public enum HexcrawlDayPhase
{
    Morning,
    Exploring,
    Camping,
    Rest
}

// Play-tab day tracker state, saved with the map.
public class HexcrawlDayState
{
    public int Day { get; set; } = 1;
    public HexcrawlDayPhase Phase { get; set; } = HexcrawlDayPhase.Morning;
    public double ExploringHoursUsed { get; set; }
    // True once the exploring hours went past the normal day length today (no recovery rest).
    public bool ForcedMarchToday { get; set; }
    // Hexes the party travelled through today, starting with the hex the day began in.
    public List<HexCoord> Path { get; set; } = new();
    // Completed days, oldest first. The last entry is restored by "Previous day".
    public List<HexcrawlDayRecord> History { get; set; } = new();
}

// A completed day: only what is needed to show the route and restore the day.
public class HexcrawlDayRecord
{
    public int Day { get; set; }
    public List<HexCoord> Path { get; set; } = new();
    public double ExploringHoursUsed { get; set; }
    public bool ForcedMarch { get; set; }
}
