using Grpc.Net.Client;
using System.Collections.Concurrent;
using UgadaikaClient;
using UgadaikaServer.MasterData;

namespace UgadaikaServer.Services
{
    /// <summary>
    /// Класс представление одной игры
    /// </summary>
    public class SingleGame(string word, string description, Action onGameEnds)
    {
        /// <summary>
        /// Рандом для определения порядка ходов
        /// </summary>
        private readonly Random _random = new();

        /// <summary>
        /// Игроки
        /// </summary>
        private readonly ConcurrentDictionary<string, Player> _players = [];

        /// <summary>
        /// Использованные буквы
        /// </summary>
        private readonly List<char> _usedChars = [];

        /// <summary>
        /// Описание слова
        /// </summary>
        private readonly string _desription = description;

        /// <summary>
        /// Слово, которое надо отгадать
        /// </summary>
        private readonly string _word = word;

        /// <summary>
        /// Слово которое будет отправляться клиентам
        /// </summary>
        private string _starredWord = String.Concat(word.Select(_ => '*'));

        /// <summary>
        /// Действие при окончании игры
        /// </summary>
        private readonly Action _onGameEnds = onGameEnds;

        /// <summary>
        /// Если кто-то отключен
        /// </summary>
        public bool SomeoneDisconnected
        {
            get => _players.Any(p => p.Value.State == PlayerState.Disconnected);
        }

        /// <summary>
        /// Отсылаем запросы на отключение игрока
        /// </summary>
        public void SendDisconnect()
        {
            foreach (var player in _players.Select(p => p.Value).Where(p => p.State != PlayerState.Disconnected))
            {
                try
                {
                    var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                    client.PlayerDisconnect(new StringMessage()
                    {
                        Value = _players.Select(p => p.Value).FirstOrDefault(p => p.State == PlayerState.Disconnected)?.Name ?? String.Empty
                    });
                }
                catch
                {
                    player.State = PlayerState.Disconnected;
                    return;
                }
            }
        }

        /// <summary>
        /// Переподключение игрока
        /// </summary>
        public void SendReconnect(string playerName)
        {
            foreach (var player in _players.Where(p => p.Key != playerName).Select(p => p.Value))
            {
                try
                {
                    var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                    client.PlayerReconnect(new StringMessage());
                }
                catch
                {
                    player.State = PlayerState.Disconnected;
                    return;
                }
            }
        }

        /// <summary>
        /// Состояние игры
        /// </summary>
        public GameState State;

        /// <summary>
        /// Имена игроков в порядке ходов
        /// </summary>
        private List<string>? _namesOrdered;

        public void AddPlayer(Player player)
        {
            _players.TryAdd(player.Name, player);
        }

        /// <summary>
        /// Проверить статусы игроков
        /// </summary>
        public void CheckPlayersStatuses()
        {
            bool allReadyToStartGame = true;
            foreach (var player in _players)
            {
                switch (player.Value.State)
                {
                    case PlayerState.New:
                        allReadyToStartGame = false;
                        break;
                }
            }
            if (allReadyToStartGame)
            {
                StartGame();
            }
        }

        /// <summary>
        /// Начать игру
        /// </summary>
        private void StartGame()
        {
            State = GameState.Running;
            if (SomeoneDisconnected)
            {
                return;
            }
            foreach (var player in _players.Select(p => p.Value))
            {
                player.State = PlayerState.InGame;
                try
                {
                    var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                    var request = new GameStartMessage()
                    {
                        StarredWord = _starredWord,
                        WordDescription = _desription
                    };
                    request.Players.AddRange(_players.Keys);
                    client.StartGame(request);
                }
                catch
                {
                    player.State = PlayerState.Disconnected;
                    return;
                }
            }
            Task.Run(ConfigureTurnsAndStart);
        }

        /// <summary>
        /// Играем
        /// </summary>
        private void ConfigureTurnsAndStart()
        {
            //Формируем очередность ходов
            _namesOrdered = [.. _players.Select(p =>
            {
                var randomNumb = _random.Next(1, 100);
                return (randomNumb, p.Key);
            }).OrderByDescending(tuple => tuple.randomNumb).Select(p => p.Key)];
            bool gameRunning = true;
            while (gameRunning)
            {
                for (int i = 0; i < _namesOrdered.Count; i++)
                {
                    //Ждем что все подключены
                    while (SomeoneDisconnected)
                    {
                        Task.Delay(1000);
                    }
                    //Если кого-то окончательно выкидываем с лобби, чтобы не попасть на IndexOutOfRange
                    if (_namesOrdered.Count == i)
                    {
                        break;
                    }
                    var player = _players[_namesOrdered[i]];
                    try
                    {
                        //Ход игрока
                        var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                        var turnResult = client.Turn(new UgadaikaClient.EmptyMessage()).Value.ToLower();
                        if (turnResult.Length != 1)
                        {
                            if (turnResult.Equals(_word, StringComparison.CurrentCultureIgnoreCase))
                            {
                                List<char> notResolvedChars = [];
                                for (int j = 0; j < _starredWord.Length; j++)
                                {
                                    if (_starredWord[j] == '*')
                                    {
                                        notResolvedChars.Add(_word[j]);
                                    }
                                }
                                player.Points += notResolvedChars.Distinct().Count();
                                EndGame();
                                gameRunning = false;
                                break;
                            }
                            else
                            {
                                SendUpdatedGameState(i == _namesOrdered.Count - 1 ? _namesOrdered[0] : _namesOrdered[i + 1]);
                            }
                        }
                        else
                        {
                            if (_word.Contains(turnResult, StringComparison.CurrentCultureIgnoreCase))
                            {
                                var sendedChar = turnResult[0];
                                for (int j = 0; j < _starredWord.Length; j++)
                                {
                                    if (_word[j] == sendedChar)
                                    {
                                        _starredWord = _starredWord[..j] + sendedChar + _starredWord[(j + 1)..];
                                    }
                                }
                                _usedChars.Add(sendedChar);
                                player.Points++;
                                if (_starredWord.Contains('*'))
                                {
                                    SendUpdatedGameState(i == _namesOrdered.Count - 1 ? _namesOrdered[0] : _namesOrdered[i + 1]);
                                }
                                else
                                {
                                    EndGame();
                                }
                            }
                            else
                            {
                                _usedChars.Add(turnResult[0]);
                                SendUpdatedGameState(i == _namesOrdered.Count - 1 ? _namesOrdered[0] : _namesOrdered[i + 1]);
                            }
                        }
                    }
                    catch
                    {
                        //Если не ответил то отключился
                        player.State = PlayerState.Disconnected;
                        i--;
                        //Ждем что все подключены
                        while (SomeoneDisconnected)
                        {
                            Task.Delay(1000);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Послать каждому игроку информацию об обновленном состоянии игры
        /// </summary>
        /// <param name="nextPlayer"></param>
        private void SendUpdatedGameState(string nextPlayer)
        {
            var playersStates = _players.Select(p => new PlayersWithPoints()
            {
                Name = p.Key,
                Points = p.Value.Points
            }).ToArray();
            foreach (var player in _players.Select(p => p.Value))
            {
                try
                {
                    var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                    var request = new GameStateMessage()
                    {
                        CurrentWord = _starredWord,
                        NextTurnPlayer = nextPlayer,
                        UsedChars = String.Concat(_usedChars)
                    };
                    request.Players.AddRange(playersStates);
                    client.UpdateGameState(request);
                }
                catch
                {
                    player.State = PlayerState.Disconnected;
                }
            }
        }

        /// <summary>
        /// Окончание игры
        /// </summary>
        private void EndGame()
        {
            var playersStates = _players.Select(p => new PlayersWithPoints()
            {
                Name = p.Key,
                Points = p.Value.Points
            }).ToArray();
            foreach (var player in _players.Select(p => p.Value))
            {
                try
                {
                    player.Points = 0;
                    var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                    var request = new GameStateMessage()
                    {
                        CurrentWord = _word,
                    };
                    request.Players.AddRange(playersStates);
                    client.EndGame(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            _onGameEnds.Invoke();
        }

        /// <summary>
        /// Убрать игрока из игры
        /// </summary>
        /// <param name="playerName"></param>
        internal void RemovePlayer(string playerName)
        {
            SendReconnect(playerName);
            _players.TryRemove(playerName, out _);
            _namesOrdered?.Remove(playerName);
        }

        /// <summary>
        /// Отправить состояение игры игроку при переподключении
        /// </summary>
        /// <param name="player"></param>
        internal void SendPlayerGameState(Player player)
        {
            try
            {
                var client = new UgadaikaClient.UgadaikaClient.UgadaikaClientClient(player.Channel);
                var request = new GameStartMessage()
                {
                    StarredWord = _starredWord,
                    WordDescription = _desription
                };
                request.Players.AddRange(_players.Keys);
                client.StartGame(request);
            }
            catch
            {
                player.State = PlayerState.Disconnected;
            }
        }
    }
}
