using DigitalBattleMap.DataClasses;
namespace DigitalBattleMap.Interfaces;

public interface ITokenLink
{
    public void Unlink(LinkableObject linkableObject);
    public TokenIndentifier GetTokenIndentifier();
}
