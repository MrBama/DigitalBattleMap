using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Interfaces;

public interface ITokenLinker
{
    public void LinkToToken(ILinkableObject linkableObject);
    public void LinkToToken(ILinkableObject linkableObject, TokenIdentifier tokenIdentifier);
}
