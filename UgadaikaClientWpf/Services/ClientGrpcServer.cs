using Grpc.Core;
using UgadaikaClient;

namespace UgadaikaClientWpf.Services
{
    /// <summary>
    /// GRPC сервер клиента
    internal class ClientGrpcServer(MainWindow mainWindow) : UgadaikaClient.UgadaikaClient.UgadaikaClientBase
    {
        /// <summary>
        /// Окно для взаимодействия
        /// </summary>
        private readonly MainWindow _mainWindow = mainWindow;

        /// <summary>
        /// Закончить игру
  
        public override Task<EmptyMessage> EndGame(GameStateMessage request, ServerCallContext context)
        {
            _mainWindow.EndGame(request.CurrentWord, [.. request.Players]);

            return Task.FromResult(new EmptyMessage());
        }

        /// <summary>
        /// Пинг||
        public override Task<EmptyMessage> Ping(EmptyMessage request, ServerCallContext context)
        {
            return Task.FromResult(new EmptyMessage());
        }

        /// <summary>
        /// Игрок отключился
        public override Task<EmptyMessage> PlayerDisconnect(StringMessage request, ServerCallContext context)
        {
            _mainWindow.PlayerDisconnected(request.Value);
            return Task.FromResult(new EmptyMessage());
        }

        /// <summary>
        /// Игрок переподключился
        public override Task<EmptyMessage> PlayerReconnect(StringMessage request, ServerCallContext context)
        {
            _mainWindow.PlayerReconnected();
            return Task.FromResult(new EmptyMessage());
        }

        /// <summary>
        /// Начать игру
        /// </summary>
        public override Task<EmptyMessage> StartGame(GameStartMessage request, ServerCallContext context)
        {
            _mainWindow.StartGame(request.StarredWord, request.WordDescription, [.. request.Players]);//StarredWord - замаскированное слово (например, "****")+описание слова
            return Task.FromResult(new EmptyMessage());

        }

        /// <summary>
        /// Твой ход//
        public override Task<StringMessage> Turn(EmptyMessage request, ServerCallContext context)
        {
            return Task.FromResult(new StringMessage() { Value = _mainWindow.Turn() });
        }

        /// <summary>
        /// Обновить состояние игры

        public override Task<EmptyMessage> UpdateGameState(GameStateMessage request, ServerCallContext context)
        {
            _mainWindow.UpdateGameStat(request.CurrentWord, [.. request.Players], request.UsedChars); 

            return Task.FromResult(new EmptyMessage());
        }
    }
}
