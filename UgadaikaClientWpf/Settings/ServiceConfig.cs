using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace UgadaikaClientWpf.Settings
{
    public class ServiceConfig
    {
        private static ServiceConfig? _serviceConfig = null;
        public static ServiceConfig GetServiceConfig(bool reload)
        {
            if (!reload && _serviceConfig != null) //Если конфиг уже загружен (_serviceConfig != null) и не требуется перезагрузка (!reload), возвращает существующий экземпляр.
            {
                return _serviceConfig;
            }
            var assembly = Assembly.GetExecutingAssembly();//получает информацию о текущей сборке.

            var configData = File.ReadAllText($"{Path.GetDirectoryName(assembly.Location)}{Path.DirectorySeparatorChar}configs{Path.DirectorySeparatorChar}config.json");//получает путь к папке с исполняемым файлом.

            _serviceConfig = JsonConvert.DeserializeObject<ServiceConfig>(configData)!;

            return _serviceConfig;
        }

        /// <summary>
        /// Адрес сервера
        /// </summary>
        public required string ServerUri { get; set; }
    }
}
