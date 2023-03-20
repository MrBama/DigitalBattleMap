using Microsoft.AspNetCore.SignalR;
using WebServer;

namespace DigitalBattleMapServer
{
    public class WebHub : Hub
    {
        public void MoveTokenButtonPressed(string user, string direction)
        {
            ConnectionController.GetInstance().ButtonPressed(user, direction);
        }
    }
}
