using Grpc.Net.Client;
using UgadaikaServer.Services;

namespace UgadaikaServer.MasterData
{
    /// <summary>
    /// Класс игрока
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Имя игрока
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Для соединений
        /// </summary>
        public required GrpcChannel Channel { get; set; }

        private PlayerState _state;
        /// <summary>
        /// Состояние игрока
        /// </summary>
        public PlayerState State
        {
            get => _state;
            set
            {
                if (value == PlayerState.Disconnected)
                {
                    if (_state != PlayerState.Disconnected)
                    {
                        _state = value;
                        var timeSpan = TimeSpan.FromSeconds(WaitDisconnected);
                        DisconnectTimer = new Timer((state) => OnExpired.Invoke(), null, timeSpan, timeSpan);
                        Game?.SendDisconnect();
                    }
                }
                else
                {
                    _state = value;
                    DisconnectTimer?.Dispose();
                    DisconnectTimer = null;
                }
                if (value != PlayerState.InGame)
                {
                    Game?.CheckPlayersStatuses();
                }
            }
        }
        /// <summary>
        /// Количество очков, если игрок в игре
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Игра в которой находится игрок
        /// </summary>
        public SingleGame? Game { get; set; }

        /// <summary>
        /// Таймер для пинга
        /// </summary>
        public required Timer PingTimer { get; set; }

        /// <summary>
        /// Время которое будут ждать отключившигося игрока
        /// </summary>
        public int WaitDisconnected { get; set; }

        /// <summary>
        /// Действие по истечению времени на дисконнект
        /// </summary>
        public required Action OnExpired { get; set; }

        /// <summary>
        /// Таймер с действием по истечению ожидания игрока
        /// </summary>
        private Timer? DisconnectTimer { get; set; }

        ~Player()
        {
            DisconnectTimer?.Dispose();
            PingTimer?.Dispose();
            Channel.Dispose();
        }
    }
}
