using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UgadaikaClientWpf
{
    /// <summary>
    /// Хранит номер порта, на котором запущен gRPC-сервер клиента
    /// </summary>
    public record PortStore(string Port);
}
