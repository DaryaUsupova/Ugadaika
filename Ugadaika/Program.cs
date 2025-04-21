using UgadaikaServer.Services;
using UgadaikaServer.Settings;

namespace UgadaikaServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);// �������� ���-����������

            var config = ServiceConfig.GetServiceConfig(false);// �������� ������������ �������

            builder.WebHost.ConfigureKestrel(opt =>
            {
                opt.ListenAnyIP(config.Port, //������ ������� ��� IP-������
                    options => 
                    {
                        options.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;// �������� ��������� HTTP/2 �������� ��� gRPC
                    });
            });

            var gameService = new GameService();
            builder.Services.AddSingleton(gameService);

            builder.Services.AddGrpc();// ����������� gRPC � ����������

            var app = builder.Build();// ��������� �������� gRPC-�������

            app.MapGrpcService<UgadaikaServerGrpc>();

            app.Map("/reconfigure", () =>
            {
                gameService.Reconfigure();// ���������� ������������ ������� �������
            });

            app.Run();//������ �������
        }
    }
}
