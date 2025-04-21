using Grpc.Net.Client;
using System.Windows;
using System.Windows.Controls;
using UgadaikaClientWpf.Settings;

namespace UgadaikaClientWpf.Windows
{
    /// <summary>
    /// Экран ожидания начала игры в лобби
    /// Позволяет игроку подтвердить готовность к игре
    /// </summary>
    public partial class LobbyWaitingToAccept : UserControl
    {
        /// <summary>
        /// Конфиг
        /// </summary>
        private readonly ServiceConfig _config;
        /// <summary>
        /// Хранилище порта клиентского gRPC сервера
        /// (нужен для идентификации клиента на сервере)
        /// </summary>
        private readonly PortStore _portStore;
        /// Конструктор экрана ожидания
        /// </summary>
        /// <param name="serviceConfig">Конфигурация сервера</param>
        /// <param name="portStore">Информация о порте клиента</param>
        public LobbyWaitingToAccept(ServiceConfig serviceConfig,
            PortStore portStore)
        {
            _config = serviceConfig;
            _portStore = portStore;
            InitializeComponent();
        }

        /// <summary>
        /// Отправить запрос о том, что готов к игре
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadyForGame(object sender, RoutedEventArgs e)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress(_config.ServerUri);// Создаем gRPC-канал
                var client = new UgadaikaServer.UgadaikaServer.UgadaikaServerClient(channel);// Создаем клиента для взаимодействия с сервером
                client.ReadySignal(new UgadaikaServer.PortRequest() //Отправляем сигнал готовности
                {
                    Port = _portStore.Port // Указываем порт для обратной связи
                });
                BtnReady.IsEnabled = false; // Делаем кнопку неактивной
                MessageBox.Show(Application.Current.MainWindow, "Ожидайте готовности остальных игроков");
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, "Ошибка при запросе на сервер " + ex.Message);
            }
        }
    }
}
