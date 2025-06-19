using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Bot : IBot
{
    private readonly TelegramBotClient _telegramClient;
    private CancellationTokenSource _cts;

    // Переменная для хранения режима работы пользователя
    private enum Mode
    {
        None,
        CountSymbols,
        SumNumbers
    }

    // Для простоты — храним режим в памяти по chatId (можно расширить)
    private readonly Dictionary<long, Mode> _userModes = new();

    public Bot()
    {
        string token = "TOKEN";
        _telegramClient = new TelegramBotClient(token);
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _telegramClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: _cts.Token);
        Console.WriteLine("Бот запущен");
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        await Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message != null)
        {
            var message = update.Message;
            var chatId = message.Chat.Id;

            if (message.Text != null)
            {
                // Обработка команд /start и выбор режима
                if (message.Text.StartsWith("/start"))
                {
                    await SendMainMenu(chatId);
                    _userModes[chatId] = Mode.None; // сброс режима
                }
                else if (message.Text == "Подсчёт символов")
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы выбрали подсчёт символов.\nОтправьте мне любой текст.", replyMarkup: new ReplyKeyboardRemove());
                    _userModes[chatId] = Mode.CountSymbols;
                }
                else if (message.Text == "Вычисление суммы")
                {
                    await botClient.SendTextMessageAsync(chatId, "Вы выбрали вычисление суммы.\nОтправьте мне числа через пробел.", replyMarkup: new ReplyKeyboardRemove());
                    _userModes[chatId] = Mode.SumNumbers;
                }
                else
                {
                    // Обработка сообщения в зависимости от режима
                    if (_userModes.TryGetValue(chatId, out var mode))
                    {
                        switch (mode)
                        {
                            case Mode.CountSymbols:
                                int length = message.Text.Length;
                                await botClient.SendTextMessageAsync(chatId, $"В вашем сообщении {length} знаков.");
                                break;

                            case Mode.SumNumbers:
                                try
                                {
                                    var parts = message.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    int sum = 0;
                                    foreach (var part in parts)
                                    {
                                        if (int.TryParse(part, out int num))
                                        {
                                            sum += num;
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(chatId, $"'{part}' не является числом. Пожалуйста, отправьте только числа через пробел.");
                                            return; // прерываем обработку при ошибке
                                        }
                                    }
                                    await botClient.SendTextMessageAsync(chatId, $"Сумма чисел: {sum}");
                                }
                                catch (Exception ex)
                                {
                                    await botClient.SendTextMessageAsync(chatId, $"Ошибка при обработке: {ex.Message}");
                                }
                                break;

                            default:
                                // Если режим не выбран или сброшен — показываем меню снова
                                await SendMainMenu(chatId);
                                break;
                        }
                    }
                    else
                    {
                        // Если режим не выбран — показываем меню снова
                        await SendMainMenu(chatId);
                    }
                }
            }
        }

        // Обработка callback-кнопок или других типов обновлений можно добавить здесь при необходимости.
    }

    private async Task SendMainMenu(long chatId)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Подсчёт символов"), new KeyboardButton("Вычисление суммы") },
        })
        { ResizeKeyboard = true };

        await _telegramClient.SendTextMessageAsync(chatId, "Пожалуйста, выберите функцию:", replyMarkup: keyboard);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}
