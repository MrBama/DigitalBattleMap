namespace DigitalBattleMap.DataClasses;
public class TokenIndentifier
{
    public TokenIndentifier()
    {
    }

    public TokenIndentifier(string name, int id)
    {
        Name = name;
        Id = id;
    }

    public string Name { get; set; }
    public int Id { get; set; }
}
