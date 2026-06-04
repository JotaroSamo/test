using Microsoft.Playwright.NUnit;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using FluentAssertions;

[TestFixture]
[AllureNUnit]
public class LoginTest : PageTest
{
    [Test]
    [AllureFeature("Auth")]
    [AllureStory("Login with valid credentials")]
    public async Task Login_WithValidCreds_ShouldSucceed()
    {
        await AllureApi.Step("Open login page", async () =>
            await Page.GotoAsync("https://practicetestautomation.com/practice-test-login/"));

        await AllureApi.Step("Fill credentials", async () => {
            await Page.FillAsync("#username", "student");
            await Page.FillAsync("#password", "Password123");
        });

        await AllureApi.Step("Submit form", async () =>
            await Page.ClickAsync("#submit"));

        await AllureApi.Step("Check success", async () => {
            var header = await Page.Locator("h1").TextContentAsync();
            header.Should().Contain("Logged In");
        });
    }
}