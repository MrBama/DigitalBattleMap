using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Interfaces;

public interface ITokenLinker
{
    public void LinkToSelectedToken(ILinkableObject linkableObject);
}
