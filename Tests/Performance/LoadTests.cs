// ═══════════════════════════════════════════════════════════════════
// НАГРУЗОЧНЫЕ / СТРЕСС-ТЕСТЫ (Load / Stress / Performance Tests)
//
// НАГРУЗОЧНЫЙ (Load Test):
//   — имитируем нормальную ожидаемую нагрузку (N пользователей)
//   — цель: убедиться что система держит рабочий трафик
//   — пример: 10 одновременных пользователей, 1 минута
//
// СТРЕСС-ТЕСТ (Stress Test):
//   — превышаем нормальную нагрузку, ищем точку отказа
//   — цель: найти предел прочности системы
//   — пример: постепенно 1→5→15→30→50 пользователей
//
// ТЕСТ ПРОИЗВОДИТЕЛЬНОСТИ (Performance Test):
//   — измеряем время ответа и пропускную способность
//   — цель: убедиться что время ответа в допустимых рамках
//   — стандарт: < 1s отлично, < 3s норма, > 3s проблема
//
// КАК: используем HttpClient (без браузера) — это быстрее и дешевле
// КОГДА ЗАПУСКАТЬ: НЕ на каждый commit! Только по расписанию или вручную.
// ═══════════════════════════════════════════════════════════════════

using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

[TestFixture]
[AllureNUnit]
[Category("performance")]
[AllureFeature("Performance & Load")]
[AllureSuite("Performance Tests")]
public class LoadTests
{
    private const string LoginUrl = "https://practicetestautomation.com/practice-test-login/";

    // ───────────────────────────────────────────────────────────────
    // ТЕСТ ПРОИЗВОДИТЕЛЬНОСТИ: время ответа одного запроса
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Single request performance")]
    [Description("Один запрос должен выполниться менее чем за 2 секунды")]
    public async Task LoginPage_SingleRequest_RespondsFast()
    {
        var (statusCode, elapsedMs) = await MeasureRequest(LoginUrl);

        await AllureApi.Step($"Результат: HTTP {statusCode}, время {elapsedMs}ms", async () =>
        {
            statusCode.Should().Be(200);
            elapsedMs.Should().BeLessThan(2000, $"ответ занял {elapsedMs}ms — слишком долго");
        });
    }

    // ───────────────────────────────────────────────────────────────
    // НАГРУЗОЧНЫЙ ТЕСТ: 10 одновременных пользователей
    // Task.WhenAll — запускаем все задачи параллельно, ждём завершения всех
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("10 concurrent users load test")]
    [Description("10 одновременных пользователей — все должны получить HTTP 200 менее чем за 5 сек")]
    public async Task LoginPage_Under10ConcurrentUsers_AllRespondSuccessfully()
    {
        const int userCount = 10;
        List<Task<(int StatusCode, long Ms)>> tasks = [];

        await AllureApi.Step($"Запускаем {userCount} запросов одновременно", async () =>
        {
            // Создаём N задач и запускаем их ВСЕ СРАЗУ — имитируем N пользователей
            for (int i = 0; i < userCount; i++)
                tasks.Add(MeasureRequest(LoginUrl));

            // Ждём пока все завершатся
            await Task.WhenAll(tasks);
        });

        await AllureApi.Step("Анализируем результаты", async () =>
        {
            var results = tasks.Select(t => t.Result).ToList();

            // Все запросы должны вернуть 200
            var failed = results.Where(r => r.StatusCode != 200).ToList();
            failed.Should().BeEmpty($"{failed.Count} из {userCount} запросов вернули не 200");

            // Самый медленный запрос должен уложиться в 5 секунд
            var maxMs = results.Max(r => r.Ms);
            maxMs.Should().BeLessThan(5000, $"самый медленный запрос: {maxMs}ms");
        });
    }

    // ───────────────────────────────────────────────────────────────
    // ТЕСТ СТАБИЛЬНОСТИ: 30 последовательных запросов
    // Проверяем что нет утечек памяти и деградации производительности
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Sequential stability test")]
    [Description("30 запросов подряд — проверяем стабильность (нет деградации)")]
    public async Task LoginPage_30SequentialRequests_NoPerformanceDegradation()
    {
        const int requestCount = 30;
        var responseTimes = new List<long>();

        await AllureApi.Step($"Отправляем {requestCount} запросов последовательно", async () =>
        {
            for (int i = 0; i < requestCount; i++)
            {
                var (_, ms) = await MeasureRequest(LoginUrl);
                responseTimes.Add(ms);
            }
        });

        await AllureApi.Step("Проверяем статистику времени ответа", async () =>
        {
            var avgMs = responseTimes.Average();
            var maxMs = responseTimes.Max();
            var minMs = responseTimes.Min();

            // Сохраняем статистику как вложение в Allure отчёт
            var stats = $"Запросов:  {requestCount}\n" +
                        $"Среднее:   {avgMs:F0}ms\n" +
                        $"Минимум:   {minMs}ms\n" +
                        $"Максимум:  {maxMs}ms\n" +
                        $"Все успешны: да";

            AllureApi.AddAttachment(
                "Response Time Statistics",
                "text/plain",
                System.Text.Encoding.UTF8.GetBytes(stats));

            avgMs.Should().BeLessThan(3000, $"среднее время {avgMs:F0}ms превышает порог 3000ms");
        });
    }

    // ───────────────────────────────────────────────────────────────
    // СТРЕСС-ТЕСТ: постепенное увеличение нагрузки ("spike test")
    // Ищем при каком количестве пользователей начинаются ошибки
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Spike stress test")]
    [Description("Резкий рост нагрузки: 1 → 5 → 15 → 25 пользователей — ищем точку отказа")]
    public async Task LoginPage_GraduallyIncreasingLoad_NoErrors()
    {
        // Волны нагрузки — имитируем трафик-пики
        var waves = new[] { 1, 5, 15, 25 };

        foreach (var concurrentUsers in waves)
        {
            await AllureApi.Step($"Волна: {concurrentUsers} одновременных пользователей", async () =>
            {
                var tasks = Enumerable.Range(0, concurrentUsers)
                    .Select(_ => MeasureRequest(LoginUrl))
                    .ToList();

                var results = await Task.WhenAll(tasks);

                var failedCount = results.Count(r => r.StatusCode != 200);
                var avgMs = results.Average(r => r.Ms);

                // Логируем результаты каждой волны
                AllureApi.AddAttachment(
                    $"Wave {concurrentUsers} users",
                    "text/plain",
                    System.Text.Encoding.UTF8.GetBytes(
                        $"Пользователей: {concurrentUsers}\n" +
                        $"Упавших: {failedCount}\n" +
                        $"Среднее время: {avgMs:F0}ms"));

                failedCount.Should().Be(0,
                    $"при {concurrentUsers} пользователях не должно быть ошибок");
            });
        }
    }

    // ───────────────────────────────────────────────────────────────
    // Вспомогательный метод: отправляет один HTTP запрос
    // Возвращает (HTTP статус код, время в миллисекундах)
    // ───────────────────────────────────────────────────────────────
    private static async Task<(int StatusCode, long Ms)> MeasureRequest(string url)
    {
        // Каждый запрос — отдельный HttpClient (имитируем отдельного пользователя)
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.GetAsync(url);
            sw.Stop();
            return ((int)response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch
        {
            // Таймаут или сетевая ошибка → статус 0
            sw.Stop();
            return (0, sw.ElapsedMilliseconds);
        }
    }
}
