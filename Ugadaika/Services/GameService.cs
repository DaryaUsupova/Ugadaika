using Grpc.Net.Client;
using System.Collections.Concurrent;
using UgadaikaServer.MasterData;
using UgadaikaServer.Settings;

namespace UgadaikaServer.Services
{
    /// <summary>
    /// Основной игровой сервис, который выбирает слова, формирует сессии и управляет играми и т.д.
    /// </summary>
    public class GameService()
    {
        /// <summary>
        /// Рандомайзер для выбора слов
        /// </summary>
        private readonly Random _random = new();

        /// <summary>
        /// Конфигурация сервера
        /// </summary>
        private ServiceConfig _config = ServiceConfig.GetServiceConfig(false);

        /// <summary>
        /// Игроки на сервере
        /// </summary>
        private readonly ConcurrentDictionary<string, Player> _players = [];

        /// <summary>
        /// Игры на сервере
        /// </summary>
        private readonly ConcurrentDictionary<int, SingleGame> _games = [];

        /// <summary>
        /// Попытка авторизовать нового игрока на сервере
        /// Если имя свободно — создаётся новый объект Player и добавляется в словарь
        internal BoolResult TryAuth(string playerName, string peer)
        {
            if (!_players.ContainsKey(playerName))// Проверяем, есть ли уже игрок с таким именем в списке игроков
            {
                var timeSpan = TimeSpan.FromSeconds(_config.PlayersPingTimeOut);// Получаем интервал пинга из конфигурации (через сколько и как часто пинговать игрока)
                _players.TryAdd(playerName, new Player()//добавление нового игрока
                {
                    Channel = GrpcChannel.ForAddress(peer),// Канал связи с игроком
                    Name = playerName,// Имя игрока 
                    Points = 0,// Начальные очки игрока 
                    State = PlayerState.New, // Устанавливаем статус игрока как "новый"
                    PingTimer = new Timer((state) => TimerPingCallBack(playerName), null, timeSpan, timeSpan),// Запускаем таймер, который будет регулярно вызывать метод пинга
                    WaitDisconnected = _config.PlayersWaitTimeOut,// Время ожидания перед окончательным отключением игрока
                    OnExpired = () => DisconnectPlayer(playerName)
                });

                return new BoolResult() { IsSuccess = true }; // Успешное добавление нового игрока
            }
            return new BoolResult() { IsSuccess = false };//если есть похожий то -
        }

        /// <summary>
        /// Действие при отключении игрока
        private void DisconnectPlayer(string playerName)
        {
            if (_players.TryRemove(playerName, out var player))
            {
                player.Game?.RemovePlayer(playerName);// Удалить из игры, если был в ней
            }
        }

        /// <summary>
        /// Пинг игрока по таймеру с конфига — проверяет доступность игрока
        /// </summary>
        private void TimerPingCallBack(string playerName)
        {
            if (!_players.TryGetValue(playerName, out var player)
                || player == null
                || !CanPingPlayer(player))// Не удалось пропинговать
            {
                if (player == null || player!.Game == null)
                {
                    _players.TryRemove(playerName, out _);// Полностью убрать игрока
                }
                else
                {
                    _players[playerName].State = PlayerState.Disconnected;// Отметить как "отключён"
                }
            }
        }

        /// <summary>
        /// Попытка переподключить игрока к серверу после разрыва соединения
        internal PlayerStateResult TryReconncet(string playerName, string peer)
        {
            // Пытаемся найти игрока по имени в словаре
            if (_players.TryGetValue(playerName, out var player))
            {
                if (peer.Contains(player.Channel.Target) // Проверяем, совпадает ли адрес клиента с текущим или игрок недоступен 
                    || !CanPingPlayer(player))
                {
                    player.Channel = GrpcChannel.ForAddress(peer);  // Обновляем GRPC-канал связи новым адресом клиента
                    if (player.State == PlayerState.Disconnected)// Если игрок ранее был отключён
                    {
                        if (player.Game != null) // Проверяем, есть ли у него текущая игра
                        {
                            if (player.Game.State == GameState.Running) // Если игра активна
                            {
                                player.State = PlayerState.InGame;// Возвращаем игрока обратно в игру
                                player.Game.SendPlayerGameState(player); // Отправляем игроку текущее состояние игры
                                player.Game.SendReconnect(playerName); // Уведомляем остальных игроков о переподключении игрока
                            }
                            else
                            {
                                player.State = PlayerState.InLobby; // Если игра ещё не началась — возвращаем игрока в лобби
                            }
                        }
                        else
                        {
                            player.State = PlayerState.New;//либо считаем игрока новым на сервере
                        }
                    }
                    return new PlayerStateResult() // Возвращаем результат с текущим состоянием игрока
                    {
                        IsSuccess = true,// Переподключение прошло успешно
                        State = player.State switch // Определяем внешнее состояние игрока по его текущему внутреннему статусу
                        {
                            PlayerState.New => State.OnServer,               //Игрок только что подключился к серверу
                            PlayerState.InGame => State.GameRunning,         // Игрок находится в активной игре
                            PlayerState.InLobby => State.InLobby,            // Игрок в лобби, ожидает начала игры
                            PlayerState.ReadyToStartGame => State.InLobby,   // Игрок нажал "готов", но игра ещё не началась
                            _ => default
                        }
                    }
                    ;
                }
            }
            return new PlayerStateResult() { IsSuccess = false };
        }

        /// <summary>
        /// Проверка: можно ли "достучаться" до игрока по GRPC
        public static bool CanPingPlayer(Player player)
        {
            try
            {
                var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel); //создаем клиента по сущ.каналу игрока
                client.Ping(new UgadaikaClient.EmptyMessage());//посылыаем пинг
                return true;
            }
            catch
            {
                return false;//ошибка
            }
        }

        /// <summary>
        /// Попытка подключится к лобби
        internal BoolResult TryGoIntoLobby(int pass, string peer)
        {
            var player = _players.Values.First(p => peer.Contains(p.Channel.Target)); //Ищем игрока по идентификатору GRPC-канала
            if (_games.TryGetValue(pass, out var game))//проверка существования игры с паролем
            {
                if (game.State == GameState.Running) //игра запущена
                {
                    return new BoolResult() { IsSuccess = false };
                }
            }
            else // Если игры нет — создаём новую( слово,подсказки и добавляем в общий список)
            {
                var word = GenerateWord();
                game = new SingleGame(word, _config.WordsDictionary[word], () => RemoveGame(pass));
                _games.TryAdd(pass, game);
            }
            game.AddPlayer(player);
            player.Game = game;
            return new BoolResult() { IsSuccess = true };
        }

        /// <summary>
        ///Удаление игры из списка активных 
        /// </summary>
        /// <param name="pass"></param>
        private void RemoveGame(int pass)
        {
            _games.TryRemove(pass, out _);
        }

        /// <summary>
        /// Получить случайное слово с конфига
        /// </summary>
        /// <returns></returns>
        private string GenerateWord()
        {
            return _config.WordsDictionary.ElementAt(_random.Next(0, _config.WordsDictionary.Keys.Count - 1)).Key;
        }

        /// <summary>
        /// Проставить готовность игрока к началу игры
        internal void PlayerReady(string peer)
        {
            var player = _players.Values.First(p => peer.Contains(p.Channel.Target));
            player.State = PlayerState.ReadyToStartGame;
        }


        /// <summary>
        /// Переконфигурировать сервис
        /// </summary>
        internal void Reconfigure()
        {
            _config = ServiceConfig.GetServiceConfig(true);
            var timeSpan = TimeSpan.FromSeconds(_config.PlayersPingTimeOut);// Вычисляем новый интервал пинга
            foreach (var player in _players.Select(p => p.Value))// Проходимся по всем игрокам на сервере
            {
                player.PingTimer.Dispose(); // Удаляем старый таймер пинга,
                player.PingTimer = new Timer((state) => TimerPingCallBack(player.Name), null, timeSpan, timeSpan);//добавляем новый
                player.WaitDisconnected = _config.PlayersWaitTimeOut; // Обновляем таймаут ожидания перед отключением игрока
            }
        }
    }
}
