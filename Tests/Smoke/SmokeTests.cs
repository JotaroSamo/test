// ═══════════════════════════════════════════════════════════════════
// СМОУК-ТЕСТЫ (Smoke Tests)
//
// ЧТО: быстрая проверка что система вообще работает ("не горит")
// КАК: минимальный набор E2E проверок — только критические пути
// ЗАЧЕМ: если смоук упал — нет смысла запускать остальные тесты
// КОГДА ЗАПУСКАТЬ: первыми, при каждом деплое, каждые 30 мин в проде
//
// Правила смоук-тестов:
//   ✓ Каждый тест < 30 секунд
//   ✓ Минимум шагов — только "жив / не жив"
//   ✓ Не проверяют детали бизнес-логики (это работа функциональных тестов)
//   ✓ Таких тестов 5-15 на весь проект, не больше
// ═══════════════════════════════════════════════════════════════════

using Microsoft.Playwright.NUnit;
using FluentAssertions;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

[TestFixture]
[AllureNUnit]
[Category("smoke")]
[AllureFeature("Smoke Checks")]
[AllureSuite("Smoke Tests")]
public class SmokeTests : PageTest  // PageTest даёт готовый Page (Playwright браузер)
{
    [Test]
    [AllureStory("Login page is reachable")]
    [Description("Страница логина открывается и форма видна — базовая проверка доступности")]
    public async Task LoginPage_ShouldBeReachable_AndShowLoginForm()
    {
        await AllureApi.Step("Открываем страницу логина", async () =>
            await Page.GotoAsync("https://practicetestautomation.com/practice-test-login/"));

        await AllureApi.Step("Проверяем что форма видна на странице", async () =>
        {
            // Expect — встроенный Playwright assert с авто-ожиданием (до 5 сек по умолчанию)
            await Expect(Page.Locator("#username")).ToBeVisibleAsync();
            await Expect(Page.Locator("#password")).ToBeVisibleAsync();
            await Expect(Page.Locator("#submit")).ToBeVisibleAsync();
        });
    }

    [Test]
    [AllureStory("Page title check")]
    [Description("Заголовок вкладки браузера содержит название сайта")]
    public async Task LoginPage_Title_ShouldContainSiteName()
    {
        await Page.GotoAsync("https://practicetestautomation.com/practice-test-login/");

        var title = await Page.TitleAsync();

        // FluentAssertions дают читаемые сообщения об ошибке
        title.Should().Contain("Practice Test Automation",
            $"получен заголовок: '{title}'");
    }

    [Test]
    [AllureStory("HTTP 200 smoke check")]
    [Description("Страница отдаёт HTTP 200 — сервер жив")]
    public async Task LoginPage_ShouldReturn_Http200()
    {
        var response = await Page.GotoAsync("https://practicetestautomation.com/practice-test-login/");

        // response?.Status возвращает HTTP статус код
        response?.Status.Should().Be(200, "страница должна быть доступна");
    }
}
