using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using System;
using System.Collections.Generic;

namespace DigitalBattleMap.Utilities;

public class TokenListItemMultiActions : ITokenListItemMultiActions
{
    private Func<List<TokenListItem>> _getSelectedTokens;
    private bool _executing = false;

    public TokenListItemMultiActions(Func<List<TokenListItem>> getSelectedTokens)
    {
        _getSelectedTokens = getSelectedTokens;
    }

    public event ConditionsChangedEventHandler OnConditionsChanged;

    public void ConditionsChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            var conditions = new List<Condition>();
            foreach (var condition in tokenListItem.Conditions)
            {
                conditions.Add(condition);
            }

            selectedTokenListItem.Conditions = conditions;
            OnConditionsChanged?.Invoke(this, new ConditionsChangedEventArgs { TokenIndentifier = selectedTokenListItem.GetTokenIndentifier(), NewConditions = conditions });
        });
    }

    public void InitiativeChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Initiative = tokenListItem.Initiative;
        });
    }

    public void HealthChanged(TokenListItem tokenListItem)
    {
        Execute(tokenListItem, (TokenListItem selectedTokenListItem) =>
        {
            selectedTokenListItem.Health.EditorHp = tokenListItem.Health.EditorHp;
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
            selectedTokenListItem.Token.SetSizeWithoutNotification(tokenListItem.Token.Size);
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
