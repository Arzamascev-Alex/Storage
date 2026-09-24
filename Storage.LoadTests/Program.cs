using System.Net.Sockets;
using System.Text;
using Storage.LoadTests;
using NBomber.CSharp;
using Storage.Core;
using System.Text.Json;

namespace Storage.LoadTests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await CheckSetGetAsync();

            var scenario = Scenario.Create("storage_set", async context =>
            {
                var response = await Step.Run("connect_and_step", context, async () =>
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                        using var client = new StorageTcpClient();

                        string key = $"load_{Guid.NewGuid():N}";

                        UserProfile profile = new()
                        {
                            Id = Random.Shared.Next(1, 1_000_000),
                            UserName = $"user_{Random.Shared.Next(1_000_000)}",
                            CreatedAt = DateTime.UtcNow,
                        };

                        await client.ConnectAsync("127.0.0.1", 8080, timeout.Token);

                        await client.SetAsync(key, profile, timeout.Token);

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

        private static async Task CheckSetGetAsync()    //  разовая проверка записи и чтения
        {
            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(5));

            using var client = new StorageTcpClient();

            await client.ConnectAsync("127.0.0.1", 8080, timeout.Token);

            UserProfile expected = new()
            {
                Id = 1,
                UserName = "Алексей Test",
                CreatedAt = DateTime.UtcNow
            };

            await client.SetAsync("check:user:1", expected, timeout.Token);

            string? json = await client.GetAsync(
                "check:user:1", timeout.Token);

            if (json is null)
            {
                throw new InvalidOperationException(
                    "GET не вернул сохранённый профиль.");
            }

            //  восстанавливаю объект и сравниваю свойства
            UserProfile? actual =
                JsonSerializer.Deserialize<UserProfile>(json);

            if (actual is null ||
                actual.Id != expected.Id ||
                actual.UserName != expected.UserName ||
                actual.CreatedAt != expected.CreatedAt)
            {
                throw new InvalidOperationException(
                    "Полученный профиль отличается от отправленного.");
            }

            //  проверяю отсутствующий ключ
            string? missing = await client.GetAsync(
                $"missing:{Guid.NewGuid():N}", timeout.Token);

            if (missing is not null)
            {
                throw new InvalidOperationException(
                    "Для отсутствующего ключа ожидался null.");
            }

            Console.WriteLine(
                "Проверка TCP пройдена: профиль совпадает, отсутствующий ключ возвращает null.");
        }

    }
}
