using Newtonsoft.Json;
using System.Reflection;

namespace UgadaikaServer.Settings
{
    /// <summary>
    /// Конфигурация сервиса
    /// </summary>
    public class ServiceConfig
    {
        private static ServiceConfig? _serviceConfig = null; //храним экземпляр конфига в памяти 
        public static ServiceConfig GetServiceConfig(bool reload) //метод получения конфига с возможностью перечитки
        {
            if (!reload && _serviceConfig != null) //если  не нужна перечитка и конфиг был уже считан 
            {
                return _serviceConfig;
            }
            var assembly = Assembly.GetExecutingAssembly();//получаем текущ. сборку
            //считываем 
            var configData = File.ReadAllText($"{Path.GetDirectoryName(assembly.Location)}{Path.DirectorySeparatorChar}configs{Path.DirectorySeparatorChar}config.json");

            _serviceConfig = JsonConvert.DeserializeObject<ServiceConfig>(configData)!;//преобразуем

            return _serviceConfig;
        }

        /// <summary>
        /// Порт на котором будет запущен сервер
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Время ожидания отключившегося игрока(в секундах)
        /// </summary>
        public int PlayersWaitTimeOut { get; set; }

        /// <summary>
        /// Интервал пинга в секундах
        /// </summary>
        public int PlayersPingTimeOut { get; set; }

        /// <summary>
        /// Словарь слов, который будет использоваться во время игры
        /// </summary>
        public Dictionary<string, string> WordsDictionary { get; set; } = [];
    }
}
