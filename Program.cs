using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramBotExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IBot, Bot>();
                })
                .Build();

            var bot = host.Services.GetRequiredService<IBot>();
            await bot.StartAsync();

            Console.WriteLine("Бот запущен. Нажмите Enter для выхода...");
            Console.ReadLine();

            await bot.StopAsync();
        }
    }
}
