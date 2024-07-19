using System;

namespace DigitalBattleMap.DataClasses;

public class TokenIdentifier : IEquatable<TokenIdentifier>, ICloneable
{
    public TokenIdentifier()
    {
    }

    public TokenIdentifier(string name)
    {
        Name = name;

        var index = name.LastIndexOf("_");
        if (index != -1)
        {
            if (int.TryParse(name.AsSpan(index + 1), out var id))
            {
                Name = name[..index];
                Id = id;
            }
        }
    }

    public TokenIdentifier(string name, int id)
    {
        Name = name;
        Id = id;
    }

    public string Name { get; set; } = "";
    public int Id { get; set; } = 1;

    public object Clone()
    {
        return new TokenIdentifier(Name, Id);
    }

    public bool Equals(TokenIdentifier? other)
    {
        return other != null && string.Equals(other.Name, Name, StringComparison.CurrentCultureIgnoreCase) && other.Id.Equals(Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is TokenIdentifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public string GetCombinedString()
    {
        return Id == 1 ? Name : $"{Name}_{Id}";
    }
}
