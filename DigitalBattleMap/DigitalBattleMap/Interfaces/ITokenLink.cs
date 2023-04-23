using System;
using System.Collections.Generic;
namespace DigitalBattleMap.Interfaces;

public interface ITokenLink
{
    public void Unlink(ILinkableObject linkableObject);
}
