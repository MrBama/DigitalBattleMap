using DigitalBattleMap.DataClasses;
namespace DigitalBattleMap.Interfaces;

public interface ILinkableObject
{
    public void UpdatePosition(Point<int> offset);
    public void Link(ITokenLink tokenLink);
    public void Unlink();
    public bool IsLinked();
    public void DisposeLink();
    public TokenIndentifier GetLinkIdentifier();
}
