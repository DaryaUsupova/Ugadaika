using System.Windows;
using System.Windows.Controls;
using UgadaikaClient;
using UgadaikaClientWpf.Settings;
using UgadaikaClientWpf.Windows;
using UgadaikaServer;

namespace UgadaikaClientWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Конфигурация сервера
        /// </summary>
        private readonly ServiceConfig _serviceConfig;
        /// <summary>
        /// Имя текущего игрока
        /// </summary>
        private string? _playerName;
        /// <summary>
        /// Количество отключившихся игроков
        /// </summary>
        private int _disconnected = 0;

        /// <summary>
        /// Для многопоточных изменений _disconnected
        /// </summary>
        private readonly object _disconnectLock = new();

        /// <summary>
        /// Хранилище порта
        /// </summary>
        public PortStore? PortStore { get; set; }

        public MainWindow(ServiceConfig config)
        {
            _serviceConfig = config;
            InitializeComponent();
        }

        /// <summary>
        /// Инициализация окна
        /// </summary>
        public void Init()
        {
            var loginView = new LoginView(_serviceConfig, SuccesfullAuth, SuccesfullReconnectAction, PortStore!);
            NavigateTo(loginView);
        }

        /// <summary>
        /// Действие при успешном переподключении на сервер
        /// </summary>
        /// name: Имя игрока, который переподключился.
        /// state: Текущее состояние игрока на сервере.
        private void SuccesfullReconnectAction(string name, State state)
        {
            _playerName = name;//сохранение имени игрока
            switch (state)
            {
                case State.OnServer://Игрок переподключился, но не находится в лобби (например, только что авторизовался).
                    var lobbyView = new LobbyView(_serviceConfig, SuccesfullJoinLobby, PortStore!);
                    NavigateTo(lobbyView);
                    break;
                case State.InLobby://Игрок переподключился и уже находится в лобби.
                    var lobbyWaitingToAccept = new LobbyWaitingToAccept(_serviceConfig, PortStore!);//передает конфигурацию
                    NavigateTo(lobbyWaitingToAccept);// и хранилище порта
                    break;
            }
        }

        /// <summary>
        /// Действие при успешной аутентификации на сервере
        /// </summary>
        private void SuccesfullAuth(string name)
        {
            _playerName = name;
            var lobbyView = new LobbyView(_serviceConfig, SuccesfullJoinLobby, PortStore!);
            NavigateTo(lobbyView);
        }

        /// <summary>
        /// Действие при удачном входе в лобби
        /// </summary>
        private void SuccesfullJoinLobby()
        {
            var lobbyWaitingToAccept = new LobbyWaitingToAccept(_serviceConfig, PortStore!);
            NavigateTo(lobbyWaitingToAccept);
        }

        /// <summary>
        /// Переключится на другое дочернее окно
        /// </summary>
        private void NavigateTo(UserControl newWindow)
        {
            ChildWindow.Content = newWindow;
        }

        /// <summary>
        /// Для изменения настроек без перезапуска приложения
        /// </summary>
        private void Reconfigure(object sender, RoutedEventArgs e)
        {
            _serviceConfig.ServerUri = ServiceConfig.GetServiceConfig(true).ServerUri;
            Init();
        }

        /// <summary>
        /// Сообщение об отключившимся игроке
        /// </summary>
        internal Task PlayerDisconnected(string name)
        {
            lock (_disconnectLock)
            {
                _disconnected++;
            }
            Dispatcher.BeginInvoke(() => MessageBox.Show(Application.Current.MainWindow, $"Игрок {name} отключился"));/// Показ сообщения в UI потоке
            //Ожидаем что игрок подключится либо его выкинет с сервера
            while (_disconnected > 0) { }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Игрок переподключился
        /// </summary>
        internal void PlayerReconnected()
        {
            lock (_disconnectLock)
            {
                _disconnected--;
            }
        }

        /// <summary>
        /// Игра закончена
        /// </summary>
        internal void EndGame(string currentWord, List<PlayersWithPoints> players)
        {
            var message = "Игра завершена!" +
                Environment.NewLine +
                $"Загаданное слово: {currentWord}" +
                Environment.NewLine +
                "Таблица очков" +
                Environment.NewLine +
                String.Join(Environment.NewLine, players.OrderByDescending(p => p.Points).Select(p => $"{p.Name}: {p.Points}"));// Сортировка по очкам
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(Application.Current.MainWindow, message);//Показ итогов
                var lobbyView = Dispatcher.Invoke(() => new LobbyView(_serviceConfig, SuccesfullJoinLobby, PortStore!));
                NavigateTo(lobbyView);// Возврат в лобби
            });
        }

        /// <summary>
        /// Начало игры для клиента
        /// </summary>
        /// starredWord - слово с маской (например "****")
        /// wordDescription - подсказка
        /// players - список игроков
        internal void StartGame(string starredWord, string wordDescription, List<string> players)
        {
            Dispatcher.BeginInvoke(() => NavigateTo(new GameWindow(_serviceConfig, players, wordDescription, starredWord)));
        }

        /// <summary>
        /// Сделать ход
        /// </summary>
        /// <returns></returns>
        internal string Turn()
        {
            return Dispatcher.Invoke(() =>
            {
                var gameWindow = (ChildWindow.Content as GameWindow)!;//// Получаем текущее игровое окно
                if (gameWindow != null)
                {
                    AnswerString answer = new() { Answer = String.Empty }; //для хранения ответа
                    var turnWindow = new TurnWindow(gameWindow.Desciption.Text,//// Описание слова
                    gameWindow.StarredWord.Text,// Текущее состояние слова (например, "к**т")
                    String.Concat(gameWindow.UsedChars),//// Уже использованные буквы
                    answer);
                    turnWindow.ShowDialog();

                    return answer.Answer;
                }
                return String.Empty;
            });
        }

        /// <summary>
        /// Обновить состояние игры
        /// </summary>
        internal void UpdateGameStat(string currentWord, List<PlayersWithPoints> playersWithPoints, string usedChars)
        {
            Dispatcher.Invoke(() =>
            {
                var gameWindow = (ChildWindow.Content as GameWindow)!;
                if (gameWindow != null)
                {
                    gameWindow.UsedChars = [.. usedChars];// Обновляем использованные буквы
                    gameWindow.UsedCharsUi.Text = String.Concat(usedChars);
                    gameWindow.StarredWord.Text = currentWord;// Обновляем текущее состояние слова
                    gameWindow.PlayersInfo.Text = String.Join(Environment.NewLine, playersWithPoints.OrderByDescending(p => p.Points).Select(p => $"{p.Name}: {p.Points}"));// Обновляем таблицу игроков
                }
            });
        }
    }
}