using System;

namespace DigitalBattleMap.DataClasses;

public class TokenIndentifier : IEquatable<TokenIndentifier>
{
    public TokenIndentifier()
    {
    }

    public TokenIndentifier(string name)
    {
        Name = name;
    }

    public TokenIndentifier(string name, int id)
    {
        Name = name;
        Id = id;
    }

    public string Name { get; set; } = "";
    public int Id { get; set; } = 1;

    public bool Equals(TokenIndentifier? other)
    {
        return other != null && string.Equals(other.Name, Name, StringComparison.CurrentCultureIgnoreCase) && other.Id.Equals(Id);
    }
}
