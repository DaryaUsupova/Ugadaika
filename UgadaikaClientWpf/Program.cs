using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using UgadaikaClientWpf.Services;
using UgadaikaClientWpf.Settings;

namespace UgadaikaClientWpf
{
    public class Program
    {
        private static IHost? _host; //_host для того чтоб могли слушать запросы
        //нам надо отмечать [STAThread], тк мы взаимодействуют с UI элементами иначе не принимает
        [STAThread]
        public static void Main()
        {
            var serviceConfig = ServiceConfig.GetServiceConfig(false);
            var mainWindow = new MainWindow(serviceConfig); 
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options => options.ListenAnyIP(0, 
                        opt =>
                        {
                            opt.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2; 
                        }));
                    webBuilder.Configure(app => 
                    {
                        app.UseRouting(); 
                        app.UseEndpoints(enpoints =>
                        {
                            enpoints.MapGrpcService<ClientGrpcServer>(); 
                        });
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddGrpc();
                    services.AddSingleton(mainWindow);
                    services.AddSingleton(serviceConfig);
                })
                .Build();

            _host.Start(); //запускает веб-сервер
            var server = _host.Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()!.Addresses; //["http://[::]:5000"].
            mainWindow.PortStore = new PortStore(addresses.First().Split(':').Last()); 
            mainWindow.Init();
            var app = new Application();
            app.Exit += (s, e) => _host.StopAsync(); 
            app.Run(mainWindow);
        }
    }
}
