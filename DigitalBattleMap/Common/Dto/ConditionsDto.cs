namespace DigitalBattleMap.Common.Dto;

public class ConditionsDto
{
    public string Character { get; set; } = "";
    public List<Condition> Conditions { get; set; } = new();
}
