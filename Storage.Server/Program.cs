namespace Storage.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            TcpServer server = new();

            await server.StartAsync();
        }
    }
}
