using UgadaikaServer.Services;
using UgadaikaServer.Settings;

namespace UgadaikaServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);// Создание веб-приложения

            var config = ServiceConfig.GetServiceConfig(false);// Загрузка конфигурации сервиса

            builder.WebHost.ConfigureKestrel(opt =>
            {
                opt.ListenAnyIP(config.Port, //сервер слушает все IP-адреса
                    options => 
                    {
                        options.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;// Включаем поддержку HTTP/2 протокол для gRPC
                    });
            });

            var gameService = new GameService();
            builder.Services.AddSingleton(gameService);

            builder.Services.AddGrpc();// Подключение gRPC в приложение

            var app = builder.Build();// Настройка маршрута gRPC-сервиса

            app.MapGrpcService<UgadaikaServerGrpc>();

            app.Map("/reconfigure", () =>
            {
                gameService.Reconfigure();// Обновление конфигурации сервиса вручную
            });

            app.Run();//запуск сервиса
        }
    }
}
