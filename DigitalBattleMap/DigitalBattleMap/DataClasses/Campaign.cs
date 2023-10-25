using DigitalBattleMap.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DigitalBattleMap.DataClasses;

public class Campaign : IEquatable<Campaign>, ICloneable
{
    public Campaign(string name)
    {
        Name = name;
    }

    public string Name { get; set; } = "";
    public ObservableCollection<Player> Players { get; set; } = new();

    public object Clone()
    {
        return new Campaign(Name)
        {
            Players = new(Players.Clone())
        };
    }

    public bool Equals(Campaign? other)
    {
        if (other == null || other.Name != Name)
        {
            return false;
        }

        return other.Players.EqualsList(Players);
    }

    public override bool Equals(object? obj)
    {
        return obj is Campaign other && Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}

public class Player : IEquatable<Player>, ICloneable
{
    public Player(string name)
    {
        Name = name;
    }

    public string Name { get; set; } = "";
    public ObservableCollection<TokenIndentifier> TokenIdentifiers { get; set; } = new();

    public object Clone()
    {
        return new Player(Name)
        {
            TokenIdentifiers = new(TokenIdentifiers.Clone())
        };
    }

    public bool Equals(Player? other)
    {
        if (other == null || other.Name != Name)
        {
            return false;
        }

        return other.TokenIdentifiers.EqualsList(TokenIdentifiers);
    }

    public override bool Equals(object? obj)
    {
        return obj is Player other && Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}
