using Grpc.Net.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UgadaikaClientWpf.Settings;

namespace UgadaikaClientWpf.Windows
{
    /// <summary>
    /// Окно лобби - позволяет игроку создать/присоединиться к игровой комнате
    /// </summary>
    public partial class LobbyView : UserControl
    {
        private readonly ServiceConfig _serviceConfig;// Конфигурация подключения к серверу 
        private readonly Action _joinLobbyAction;
        private readonly PortStore _portStore;// Хранилище порта gRPC-сервера клиента

        /// <summary>
        /// Конструктор окна лобби
        /// </summary>
        /// serviceConfig - Конфигурация сервера
        /// joinLobbyAction - Действие после успешного входа
        /// portStore - Информация о порте клиента
        public LobbyView(ServiceConfig serviceConfig, Action joinLobbyAction, PortStore portStore)
        {
            _serviceConfig = serviceConfig;
            _joinLobbyAction = joinLobbyAction;
            InitializeComponent();
            _portStore = portStore;
        }

        /// <summary>
        /// Проверяем что в пароль для лобби вводят только цифры
        /// </summary>
        private void CheckLobbyPass(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !e.Text.All(char.IsDigit);
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Войти в лобби"
        /// </summary>
        private void JoinLobby(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress(_serviceConfig.ServerUri);// Создаем gRPC-канал к серверу
                var client = new UgadaikaServer.UgadaikaServer.UgadaikaServerClient(channel);// Создаем клиента для взаимодействия с сервером
                var result = client.GoIntoLobby(new UgadaikaServer.LobbyRequest()// Отправляем запрос на вход в лобби
                {
                    Pass = Convert.ToInt32(LobbyPass.Text),
                    Port = _portStore.Port // Порт клиента для обратной связи
                });
                if (result.IsSuccess)
                {
                    MessageBox.Show(Application.Current.MainWindow, "Вы успешно зашли в лобби");
                    _joinLobbyAction.Invoke();
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "Попытка зайти в лобби неудачна, скорее всего такое лобби уже существует и игра началась, попробуйте другой пароль");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, "Ошибка при вступлении в лобби " + ex.Message);
            }
        }
    }
}
