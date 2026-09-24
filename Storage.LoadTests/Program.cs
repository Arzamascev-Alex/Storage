using System.Net.Sockets;
using System.Text;
using Storage.LoadTests;
using NBomber.CSharp;

namespace Storage.LoadTests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var scenario = Scenario.Create("storage_set", async context =>
            {
                var response = await Step.Run("connect_and_step", context, async () =>
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                        using var client = new StorageTcpClient();

                        string key = $"load_{Guid.NewGuid():N}";

                        byte[] value = Encoding.UTF8.GetBytes($"value_{Random.Shared.Next(1_000_000)}");

                        await client.ConnectAsync("127.0.0.1", 8080, timeout.Token);

                        await client.SetAsync(key, value, timeout.Token);

                        return Response.Ok();
                    }
                    catch (OperationCanceledException)
                    {
                        return Response.Fail(statusCode: "TIMEOUT", message: "Подключение и SET не завершились за 5 секунд.");
                    }
                    catch (Exception ex)
                    {
                        return Response.Fail(statusCode: "CLIENT_ERROR", message: ex.Message);
                    }
                });

                return response;
            })
            .WithRestartIterationOnFail(false)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(Simulation.Inject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)
                ));

            NBomberRunner.RegisterScenarios(scenario).Run();

            Console.WriteLine("Тест заверше. Нажми Enter.");
            Console.ReadLine();


            /*
            //using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            //using var client = new StorageTcpClient();

            //await client.ConnectAsync("127.0.0.1", 8080, timeout.Token);

            //await client.SetAsync("test_key", Encoding.UTF8.GetBytes("hello"), timeout.Token);

            //Console.WriteLine("SET выполнен успешно.");

            //string? value = await client.GetAsync("test_key", timeout.Token);

            //if (value != "hello")
            //{
            //    throw new InvalidOperationException($"Ожидаемо hello, получено: {value ?? "(nil)"}");
            //}

            //Console.WriteLine($"GET вернул: {value}");

            //Console.WriteLine("Нажми Enter для выхода...");
            //Console.ReadLine();
            */

        }
    }
}
