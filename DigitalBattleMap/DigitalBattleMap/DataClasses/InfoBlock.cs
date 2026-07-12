namespace DigitalBattleMap.DataClasses;

public class InfoBlock 
{
    public InfoBlock(string controlInfo)
    {
        Type = null;
        Additional = null;
        ControlInfo = controlInfo;
    }

    public InfoBlock(ControlType type, string controlInfo)
    {
        Type = type;
        Additional = null;
        ControlInfo = controlInfo;
    }

    public InfoBlock(ControlType type, ControlType additional, string controlInfo)
    {
        Type = type;
        Additional = additional;
        ControlInfo = controlInfo;
    }

    public ControlType? Type { get; set; }
    public ControlType? Additional { get; set; }
    public string ControlInfo { get; set; }

}
