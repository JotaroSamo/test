// ═══════════════════════════════════════════════════════════════════
// ИНТЕГРАЦИОННЫЕ ТЕСТЫ (Integration Tests)
//
// ЧТО: проверяем взаимодействие компонентов — HTTP запросы к реальному серверу
// КАК: без браузера, напрямую через HttpClient (быстрее чем Playwright)
// ЗАЧЕМ: проверяем что бэкенд/API работает независимо от фронтенда
// КОГДА ЗАПУСКАТЬ: после юнит-тестов, перед E2E
//
// Отличие от юнит: юнит = изолированный класс, интеграция = реальная сеть/БД
// Отличие от E2E:  E2E = браузер + UI, интеграция = HTTP протокол напрямую
// ═══════════════════════════════════════════════════════════════════

using System.Net;
using System.Diagnostics;
using FluentAssertions;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

[TestFixture]
[AllureNUnit]
[Category("integration")]
[AllureFeature("Login Page HTTP")]
[AllureSuite("Integration Tests")]
public class LoginApiTests
{
    private HttpClient _client = null!;
    private const string LoginUrl = "https://practicetestautomation.com/practice-test-login/";

    [SetUp]
    public void SetUp()
    {
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        // User-Agent чтобы запросы выглядели как браузерные
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 AQA-Integration-Tests/1.0");
    }

    [TearDown]  // Выполняется ПОСЛЕ каждого теста — освобождаем ресурсы
    public void TearDown() => _client.Dispose();

    // ───────────────────────────────────────────────────────────────
    // HTTP статус-коды
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("HTTP status 200")]
    [Description("GET /practice-test-login/ должен вернуть HTTP 200 OK")]
    public async Task LoginPage_WhenRequested_Returns200()
    {
        await AllureApi.Step($"GET {LoginUrl}", async () =>
        {
            var response = await _client.GetAsync(LoginUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "страница должна быть доступна");
        });
    }

    [Test]
    [AllureStory("Content-Type is HTML")]
    [Description("Ответ сервера должен быть HTML документом")]
    public async Task LoginPage_ContentType_IsTextHtml()
    {
        var response = await _client.GetAsync(LoginUrl);

        response.Content.Headers.ContentType?.MediaType
            .Should().Contain("text/html",
                "страница должна возвращать HTML, не JSON или бинарные данные");
    }

    // ───────────────────────────────────────────────────────────────
    // Время ответа (нефункциональная проверка на уровне интеграции)
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Response time under 3s")]
    [Description("Сервер должен отвечать менее чем за 3 секунды")]
    public async Task LoginPage_ResponseTime_IsUnder3Seconds()
    {
        var stopwatch = Stopwatch.StartNew();
        await _client.GetAsync(LoginUrl);
        stopwatch.Stop();

        await AllureApi.Step($"Время ответа: {stopwatch.Elapsed.TotalMilliseconds:F0}ms", async () =>
        {
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3),
                $"сервер ответил за {stopwatch.Elapsed.TotalMilliseconds:F0}ms — слишком медленно");
        });
    }

    // ───────────────────────────────────────────────────────────────
    // Содержимое HTML (контракт страницы)
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("HTML contains login form elements")]
    [Description("HTML должен содержать поля формы — если их нет, фронтенд сломан")]
    public async Task LoginPage_Html_ContainsFormElements()
    {
        var html = await _client.GetStringAsync(LoginUrl);

        await AllureApi.Step("Проверяем наличие полей формы в HTML", async () =>
        {
            html.Should().Contain("id=\"username\"", "поле логина отсутствует в HTML");
            html.Should().Contain("id=\"password\"", "поле пароля отсутствует в HTML");
            html.Should().Contain("id=\"submit\"",   "кнопка Submit отсутствует в HTML");
        });
    }

    [Test]
    [AllureStory("No server errors in HTML")]
    [Description("HTML не должен содержать стектрейсы или сообщения об ошибках сервера")]
    public async Task LoginPage_Html_ContainsNoServerErrors()
    {
        var html = await _client.GetStringAsync(LoginUrl);

        await AllureApi.Step("Проверяем что нет ошибок сервера в HTML", async () =>
        {
            html.Should().NotContain("Exception",   "в HTML присутствует серверное исключение");
            html.Should().NotContain("Stack Trace", "в HTML присутствует стектрейс");
            html.Should().NotContain("Fatal error", "в HTML присутствует Fatal error");
        });
    }

    // ───────────────────────────────────────────────────────────────
    // Параметризованный: несколько страниц сайта
    // ───────────────────────────────────────────────────────────────

    [TestCase("https://practicetestautomation.com/practice-test-login/", 200, TestName = "LoginPage_Returns200")]
    [TestCase("https://practicetestautomation.com/",                     200, TestName = "HomePage_Returns200")]
    [AllureStory("Multiple pages availability")]
    public async Task Pages_WhenRequested_ReturnExpectedStatusCode(string url, int expectedStatus)
    {
        var response = await _client.GetAsync(url);
        ((int)response.StatusCode).Should().Be(expectedStatus, $"URL: {url}");
    }
}
