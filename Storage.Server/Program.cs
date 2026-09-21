using Storage.Core;

namespace Storage.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var store = new SimpleStore();

            TcpServer server = new(store);

            await server.StartAsync();
        }
    }
}
