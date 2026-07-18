using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.Utilities;

public class TokenListItemMultiActions : ITokenListItemMultiActions
{
    private Func<List<TokenListItem>> _getSelectedTokens;
    private bool _executing = false;

    public TokenListItemMultiActions(Func<List<TokenListItem>> getSelectedTokens)
    {
        _getSelectedTokens = getSelectedTokens;
    }

    public event EventHandler<ConditionsChangedEventArgs> OnConditionsChanged;

    public void ConditionsChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            var conditions = tokenListItem.Conditions.ToList();
            selectedTokenListItem.Conditions = conditions;
            OnConditionsChanged?.Invoke(this, new ConditionsChangedEventArgs { TokenIdentifier = selectedTokenListItem.GetTokenIdentifier(), NewConditions = conditions });
        });
    }

    public void InitiativeChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Initiative = tokenListItem.Initiative;
        });
    }

    public void HeightChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Height = tokenListItem.Height;
            var conditions = tokenListItem.Conditions.ToList();
            selectedTokenListItem.Conditions = conditions;
            OnConditionsChanged?.Invoke(this, new ConditionsChangedEventArgs { TokenIdentifier = selectedTokenListItem.GetTokenIdentifier(), NewConditions = conditions });
        });
    }

    public void HealthChanged(TokenListItem tokenListItem, TokenHealthChange tokenHealthChange, int hpChange)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            if (tokenHealthChange == TokenHealthChange.Absolute)
            {
                selectedTokenListItem.Health.EditorHp = tokenListItem.Health.EditorHp;
            }
            else
            {
                var newHp = selectedTokenListItem.Health.EditorHp;
                if (int.TryParse(selectedTokenListItem.Health.EditorHp, out var currentHp))
                {
                    newHp = Math.Max(0, currentHp + hpChange).ToString();
                }
                selectedTokenListItem.Health.EditorHp = newHp;
            }
            selectedTokenListItem.Health.ApplyHp();
        });
    }

    public void MaxHealthChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Health.EditorMaxHp = tokenListItem.Health.EditorMaxHp;
            selectedTokenListItem.Health.ApplyMaxHp();
        });
    }

    public void TokenSizeChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Token.Size = tokenListItem.Token.Size;
        });
    }

    public void TokenOrientationChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Token.Orientation = tokenListItem.Token.Orientation;
        });
    }

    public void VisibilityChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Visible = tokenListItem.Visible;
        });
    }

    public void ZLevelChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.ZLevel = tokenListItem.ZLevel;
        });
    }

    private void Execute(TokenListItem tokenListItem, Action<TokenListItem> action)
    {
        if (!_executing)
        {
            _executing = true;
            var selectedTokens = _getSelectedTokens();
            if (selectedTokens != null && selectedTokens.Count > 1 && selectedTokens.Contains(tokenListItem))
            {
                foreach (var selectedTokenListItem in selectedTokens)
                {
                    if (selectedTokenListItem != tokenListItem)
                    {
                        action(selectedTokenListItem);
                    }
                }
            }
            _executing = false;
        }
    }
}
