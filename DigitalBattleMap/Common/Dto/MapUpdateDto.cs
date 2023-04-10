namespace DigitalBattleMap.Common.Dto;

public class MapUpdateDto
{
    public DrawLayer Layer { get; set; }
    public byte[] Data { get; set; }
}