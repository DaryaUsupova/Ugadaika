using Grpc.Net.Client;
using System.Windows;
using System.Windows.Controls;
using UgadaikaClientWpf.Settings;
using UgadaikaServer;

namespace UgadaikaClientWpf.Windows
{
    /// <summary>
    /// Окно авторизации и переподключения игрока
    /// </summary>
    public partial class LoginView : UserControl
    {
        /// <summary>
        /// Конфиг
        /// </summary>
        private readonly ServiceConfig _config;
        /// <summary>
        /// Действие при успешной аутентификации
        /// </summary>
        private readonly Action<string> _succesfullAuthAction;
        /// <summary>
        /// Действие при успешном переподключении
        /// </summary>
        private readonly Action<string, State> _succesfullReconnectAction;
        /// <summary>
        /// Хранилище порта клиента
        /// </summary>
        private readonly PortStore _portStore;
        // Конструктор окна авторизации
        public LoginView(ServiceConfig serviceConfig, Action<string> succesfullAuthAction,
            Action<string, State> succesfullReconnectAction, PortStore portStore)
        {
            _config = serviceConfig;
            _succesfullAuthAction = succesfullAuthAction;
            _succesfullReconnectAction = succesfullReconnectAction;
            InitializeComponent();
            _portStore = portStore;
        }

        /// <summary>
        /// Переподключиться
        /// </summary>
        private void Reconnect(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(PlayerName.Text))
            {
                MessageBox.Show(Application.Current.MainWindow, "Нельзя переподключиться с пустым именем");
                return;
            }
            try
            {
                // Создание gRPC подключения
                using var channel = GrpcChannel.ForAddress(_config.ServerUri);
                var serverClient = new UgadaikaServer.UgadaikaServer.UgadaikaServerClient(channel);
                // Отправка запроса на переподключение
                var result = serverClient.Reconnect(new UgadaikaServer.AuthRequest()
                {
                    PlayerName = PlayerName.Text,
                    Port = _portStore.Port
                });
                // Обработка ответа
                if (result.IsSuccess)
                {
                    MessageBox.Show(Application.Current.MainWindow, "Переподключение успешно");
                    _succesfullReconnectAction.Invoke(PlayerName.Text, result.State);
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "Попытка переподключения не успешна. Возможно имя уже занято на сервере, попробуйте другое имя");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, "Возникла ошибка при переподключении на сервере " + ex.Message);
            }
        }

        /// <summary>
        /// Аутентифицироваться
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Auth(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(PlayerName.Text))
            {
                MessageBox.Show(Application.Current.MainWindow, "Нельзя аутентифицироваться с пустым именем");
                return;
            }
            try
            {
                using var channel = GrpcChannel.ForAddress(_config.ServerUri);
                var serverClient = new UgadaikaServer.UgadaikaServer.UgadaikaServerClient(channel);
                var result = serverClient.Auth(new UgadaikaServer.AuthRequest()
                {
                    PlayerName = PlayerName.Text,
                    Port = _portStore.Port
                });
                if (result.IsSuccess)
                {
                    MessageBox.Show(Application.Current.MainWindow, "Аутентификация успешна");
                    _succesfullAuthAction.Invoke(PlayerName.Text);
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "Аутентификация не успешна. Возможно имя уже занято на сервере, попробуйте другое имя");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, "Возникла ошибка при аутентификации на сервере " + ex.Message);
            }
        }
    }
}
