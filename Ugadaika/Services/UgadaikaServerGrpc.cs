using Grpc.Core;
using System.Text.RegularExpressions;

namespace UgadaikaServer.Services
{
    /// <summary>
    /// GRPC сервис сервера
    /// </summary>
    /// <param name="gameService"></param>
    public partial class UgadaikaServerGrpc(GameService gameService) : UgadaikaServer.UgadaikaServerBase //наследуемся от авт.сгенер. файла
    {
        private readonly GameService _gameService = gameService;// хранени яклиентов и игр

        public override Task<BoolResult> Auth(AuthRequest request, ServerCallContext context)//запрос на аунтиф.
        {
            return Task.FromResult(_gameService.TryAuth(request.PlayerName, GetRemoteId(context.Peer, request.Port)));
        }

        public override Task<BoolResult> GoIntoLobby(LobbyRequest request, ServerCallContext context)//запрос на заход в лобби
        {
            return Task.FromResult(_gameService.TryGoIntoLobby(request.Pass, GetRemoteId(context.Peer, request.Port)));
        }

        public override Task<EmptyMessage> ReadySignal(PortRequest request, ServerCallContext context)//готов к старту игры
        {
            _gameService.PlayerReady(GetRemoteId(context.Peer, request.Port)); 
            return Task.FromResult(new EmptyMessage());
        }

        public override Task<PlayerStateResult> Reconnect(AuthRequest request, ServerCallContext context) //переподключение игрока
        {
            return Task.FromResult(_gameService.TryReconncet(request.PlayerName, GetRemoteId(context.Peer, request.Port)));
        }

        private static string GetRemoteId(string peer, string port)// получить ipv4 из запроса 
        {
            if (string.IsNullOrEmpty(peer)) throw new Exception("Wrong request");
            //считываение ip клиента
            string pattern = @"\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
            Match match = Regex.Match(peer, pattern);//ищем совпадения в адресе клиента
            if (match.Success)
            {
                return "http://" + match.Value + ":" + port; //если нашлось то извне и формируем адрс. строку для канала
            }
            else
            {
                return "http://localhost:" + port; 
            }
        }
    }
}
