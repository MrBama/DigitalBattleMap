using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Interfaces;

public interface ITokenListItemMultiActions
{
    void InitiativeChanged(TokenListItem tokenListItem);
    void VisibilityChanged(TokenListItem tokenListItem);
    void ConditionsChanged(TokenListItem tokenListItem);
    void TokenSizeChanged(TokenListItem tokenListItem);
    void TokenOrientationChanged(TokenListItem tokenListItem);
    void HealthChanged(TokenListItem tokenListItem);
    void MaxHealthChanged(TokenListItem tokenListItem);
    void ZLevelChanged(TokenListItem tokenListItem);
}
