// ═══════════════════════════════════════════════════════════════════
// ФУНКЦИОНАЛЬНЫЕ ТЕСТЫ (Functional Tests) / E2E
//
// ЧТО: проверяем бизнес-сценарии через реальный браузер от начала до конца
// КАК: Playwright управляет браузером, имитируя действия реального пользователя
// ЗАЧЕМ: убеждаемся что ФУНКЦИЯ (вход, выход, ошибки) работает как ожидается
// КОГДА ЗАПУСКАТЬ: после смоук + интеграционных тестов
//
// Отличие от смоук: смоук = "жив ли сайт", функциональный = "работает ли логика"
// Отличие от интеграции: здесь браузер + UI, там голый HTTP
//
// В каждом тесте проверяем один бизнес-сценарий:
//   "Пользователь вводит верный пароль → видит страницу успеха"
//   "Пользователь вводит неверный пароль → видит сообщение об ошибке"
// ═══════════════════════════════════════════════════════════════════

using Microsoft.Playwright.NUnit;
using FluentAssertions;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

[TestFixture]
[AllureNUnit]
[Category("functional")]
[AllureFeature("Login Feature")]
[AllureSuite("Functional Tests")]
public class LoginFunctionalTests : PageTest
{
    private const string LoginUrl = "https://practicetestautomation.com/practice-test-login/";

    // ───────────────────────────────────────────────────────────────
    // Позитивные сценарии — "Happy Path"
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Successful login")]
    [Description("Верные данные → редирект на страницу успеха, заголовок содержит 'Logged In'")]
    public async Task Login_WithValidCredentials_RedirectsToSuccessPage()
    {
        await AllureApi.Step("Открываем страницу логина", async () =>
            await Page.GotoAsync(LoginUrl));

        await AllureApi.Step("Вводим корректные данные", async () =>
        {
            await Page.FillAsync("#username", "student");
            await Page.FillAsync("#password", "Password123");
        });

        await AllureApi.Step("Нажимаем кнопку входа", async () =>
            await Page.ClickAsync("#submit"));

        await AllureApi.Step("Проверяем успешный вход", async () =>
        {
            // Проверяем URL — браузер должен перейти на новую страницу
            Page.Url.Should().Contain("logged-in-successfully",
                "после входа должен быть редирект на страницу успеха");

            // Проверяем заголовок страницы
            var header = await Page.Locator("h1").TextContentAsync();
            header.Should().Contain("Logged In",
                "страница успеха должна содержать текст 'Logged In'");
        });
    }

    [Test]
    [AllureStory("Logout after login")]
    [Description("После входа пользователь может выйти — кнопка Log out возвращает на страницу логина")]
    public async Task Login_ThenLogout_ReturnsToLoginPage()
    {
        await AllureApi.Step("Входим в систему", async () =>
        {
            await Page.GotoAsync(LoginUrl);
            await Page.FillAsync("#username", "student");
            await Page.FillAsync("#password", "Password123");
            await Page.ClickAsync("#submit");
        });

        await AllureApi.Step("Нажимаем кнопку Log out", async () =>
        {
            // Locator по тексту кнопки — гибкий способ найти элемент
            await Page.Locator("a:has-text('Log out')").ClickAsync();
        });

        await AllureApi.Step("Проверяем возврат на форму логина", async () =>
        {
            // После выхода форма логина должна снова быть видна
            await Expect(Page.Locator("#username")).ToBeVisibleAsync();
            await Expect(Page.Locator("#submit")).ToBeVisibleAsync();
        });
    }

    // ───────────────────────────────────────────────────────────────
    // Негативные сценарии — "Unhappy Path"
    // Важно: негативные тесты не менее важны, чем позитивные
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Wrong password shows error")]
    [Description("Неверный пароль → сообщение 'Your password is invalid!'")]
    public async Task Login_WithWrongPassword_ShowsPasswordError()
    {
        await AllureApi.Step("Открываем страницу и вводим неверный пароль", async () =>
        {
            await Page.GotoAsync(LoginUrl);
            await Page.FillAsync("#username", "student");
            await Page.FillAsync("#password", "WrongPassword999");
            await Page.ClickAsync("#submit");
        });

        await AllureApi.Step("Проверяем сообщение об ошибке", async () =>
        {
            var error = Page.Locator("#error");
            await Expect(error).ToBeVisibleAsync();

            var errorText = await error.TextContentAsync();
            errorText.Should().Contain("Your password is invalid!",
                "должно быть специфическое сообщение о неверном пароле");
        });
    }

    [Test]
    [AllureStory("Wrong username shows error")]
    [Description("Неверный логин → сообщение 'Your username is invalid!'")]
    public async Task Login_WithWrongUsername_ShowsUsernameError()
    {
        await Page.GotoAsync(LoginUrl);
        await Page.FillAsync("#username", "nonexistent_user");
        await Page.FillAsync("#password", "Password123");
        await Page.ClickAsync("#submit");

        var errorText = await Page.Locator("#error").TextContentAsync();
        errorText.Should().Contain("Your username is invalid!");
    }

    [Test]
    [AllureStory("Empty form shows error")]
    [Description("Отправка пустой формы → ошибка валидации")]
    public async Task Login_WithEmptyFields_ShowsValidationError()
    {
        await Page.GotoAsync(LoginUrl);

        // Отправляем форму без заполнения полей
        await Page.ClickAsync("#submit");

        // Должно появиться какое-то сообщение об ошибке
        var error = Page.Locator("#error");
        await Expect(error).ToBeVisibleAsync();
    }

    // ───────────────────────────────────────────────────────────────
    // ПАРАМЕТРИЗОВАННЫЙ функциональный тест
    // Один шаблон — несколько наборов неверных данных
    // ───────────────────────────────────────────────────────────────

    [TestCase("",        "",            TestName = "BothFieldsEmpty")]
    [TestCase("student", "",            TestName = "PasswordEmpty")]
    [TestCase("",        "Password123", TestName = "UsernameEmpty")]
    [TestCase("hacker",  "hacked",      TestName = "BothFieldsWrong")]
    [AllureStory("Invalid credentials parametrized")]
    [Description("Любые неверные данные не должны давать доступ в систему")]
    public async Task Login_WithInvalidCredentials_DoesNotGrantAccess(
        string username, string password)
    {
        await Page.GotoAsync(LoginUrl);
        await Page.FillAsync("#username", username);
        await Page.FillAsync("#password", password);
        await Page.ClickAsync("#submit");

        // Главное: URL не должен измениться на страницу успеха
        Page.Url.Should().NotContain("logged-in-successfully",
            $"пользователь '{username}' не должен получить доступ");
    }
}
