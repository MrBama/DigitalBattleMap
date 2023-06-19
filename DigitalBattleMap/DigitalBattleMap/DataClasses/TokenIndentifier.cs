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

    public bool Equals(TokenIndentifier? other)
    {
        return other != null && string.Equals(other.Name, Name, StringComparison.CurrentCultureIgnoreCase) && other.Id.Equals(Id);
    }

    public string GetCombinedString()
    {
        return Id == 1 ? Name : $"{Name}_{Id}";
    }
}
