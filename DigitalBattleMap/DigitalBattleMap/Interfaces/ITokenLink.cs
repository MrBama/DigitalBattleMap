using DigitalBattleMap.DataClasses;
namespace DigitalBattleMap.Interfaces;

public interface ITokenLink
{
    public void Unlink(ILinkableObject linkableObject);
    public TokenIndentifier GetTokenIndentifier();
}
