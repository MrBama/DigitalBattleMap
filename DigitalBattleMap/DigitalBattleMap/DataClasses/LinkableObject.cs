using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System;

namespace DigitalBattleMap.DataClasses;

public class LinkableObject : PropertyHandler, IDisposable
{
    private ITokenLink _tokenLink;
    private Action<Point<int>> _updatePosition;

    public LinkableObject(Action<Point<int>> updatePosition)
    {
        _updatePosition = updatePosition;
        LinkToTokenButtonText = "Link to token";
    }

    public string LinkToTokenButtonText { get => Get<string>(); set => Set(value); }

    public void Link(ITokenLink tokenLink)
    {
        _tokenLink?.Unlink(this);
        _tokenLink = tokenLink;
        RefreshLinkToTokenButtonText();
    }

    public void Unlink()
    {
        _tokenLink?.Unlink(this);
        _tokenLink = null;
        RefreshLinkToTokenButtonText();
    }

    public bool IsLinked()
    {
        return _tokenLink != null;
    }

    public void DisposeLink()
    {
        _tokenLink = null;
        RefreshLinkToTokenButtonText();
    }

    public TokenIdentifier GetLinkIdentifier()
    {
        return _tokenLink.GetTokenIdentifier();
    }

    public void Dispose()
    {
        Unlink();
    }

    public void UpdatePosition(Point<int> offset)
    {
        _updatePosition(offset);
    }

    private void RefreshLinkToTokenButtonText()
    {
        if (IsLinked())
        {
            var linkIdentifier = GetLinkIdentifier();
            LinkToTokenButtonText = $"Unlink from {linkIdentifier.Name} {linkIdentifier.Id}";
        }
        else
        {
            LinkToTokenButtonText = "Link to token";
        }
    }
}
