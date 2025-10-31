namespace DigitalBattleMap.Common;

public interface IWebHub
{
    // Server to client
    Task SetConditions(string character, List<Condition> conditions);
    Task SetTokens(string player, List<string> tokens);
    Task SetCampaign(Dictionary<string, List<string>> players);

    // Client to server
    Task MoveToken(string character, Direction direction);
    Task ToggleCondition(string character, Condition condition);
    Task SetOrientation(string player, Orientation orientation);
    Task SetHeight(string character, int height);
    Task GetTokens(string player);
    Task GetConditions(string character);
}