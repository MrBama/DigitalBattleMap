using System;

namespace DigitalBattleMap.DataClasses;

public class TokenIndentifier : IEquatable<TokenIndentifier>, ICloneable
{
    public TokenIndentifier()
    {
    }

    public TokenIndentifier(string name)
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

    public TokenIndentifier(string name, int id)
    {
        Name = name;
        Id = id;
    }

    public string Name { get; set; } = "";
    public int Id { get; set; } = 1;

    public object Clone()
    {
        return new TokenIndentifier(Name, Id);
    }

    public bool Equals(TokenIndentifier? other)
    {
        return other != null && string.Equals(other.Name, Name, StringComparison.CurrentCultureIgnoreCase) && other.Id.Equals(Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is TokenIndentifier other && Equals(other);
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
