// ═══════════════════════════════════════════════════════════════════
// ЮНИТ-ТЕСТЫ (Unit Tests)
//
// ЧТО: тестируем один изолированный класс / метод / функцию
// КАК: без браузера, без сети, без базы данных — только логика
// ЗАЧЕМ: самые быстрые тесты (миллисекунды), ловят логические ошибки
// КОГДА ЗАПУСКАТЬ: при каждом коммите, первыми в пайплайне
//
// Паттерн Arrange-Act-Assert (AAA):
//   Arrange  = готовим входные данные и зависимости
//   Act      = вызываем тестируемый метод
//   Assert   = проверяем результат
// ═══════════════════════════════════════════════════════════════════

using Tests.Helpers;
using FluentAssertions;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

[TestFixture]                    // NUnit: этот класс содержит тесты
[AllureNUnit]                    // Allure: включить сбор отчётности для класса
[Category("unit")]               // Категория для запуска через --filter "Category=unit"
[AllureFeature("Password Validation")]  // Группировка в Allure по фиче
[AllureSuite("Unit Tests")]      // Отображаемое название набора в отчёте
public class PasswordValidatorTests
{
    private PasswordValidator _validator = null!;

    [SetUp]  // NUnit: выполняется ПЕРЕД каждым тестом — создаём чистый экземпляр
    public void SetUp() => _validator = new PasswordValidator();

    // ───────────────────────────────────────────────────────────────
    // Простые позитивные / негативные тесты
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Valid password passes")]
    [Description("Правильный пароль (заглавная + цифра + 8+ символов) должен пройти валидацию")]
    public void IsValid_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        var password = "Password123";

        // Act
        var result = _validator.IsValid(password);

        // Assert
        result.Should().BeTrue("пароль содержит заглавную букву, цифру и длину >= 8");
    }

    [Test]
    [AllureStory("Short password fails")]
    [Description("Пароль короче 8 символов — невалидный")]
    public void IsValid_WithShortPassword_ReturnsFalse()
    {
        var result = _validator.IsValid("Ab1");
        result.Should().BeFalse("пароль содержит только 3 символа");
    }

    [Test]
    [AllureStory("Empty password fails")]
    [Description("Пустая строка — невалидный пароль")]
    public void IsValid_WithEmptyPassword_ReturnsFalse()
    {
        var result = _validator.IsValid("");
        result.Should().BeFalse("пустая строка не является паролем");
    }

    [Test]
    [AllureStory("Password without uppercase fails")]
    [Description("Пароль без заглавной буквы — невалидный")]
    public void IsValid_WithoutUppercase_ReturnsFalse()
    {
        var result = _validator.IsValid("password123");
        result.Should().BeFalse("нет заглавной буквы");
    }

    [Test]
    [AllureStory("Password without digits fails")]
    [Description("Пароль без цифры — невалидный")]
    public void IsValid_WithoutDigit_ReturnsFalse()
    {
        var result = _validator.IsValid("PasswordOnly");
        result.Should().BeFalse("нет цифры");
    }

    // ───────────────────────────────────────────────────────────────
    // ПАРАМЕТРИЗОВАННЫЙ ТЕСТ
    // Один тест — много входных данных.
    // Используй когда логика одна, а кейсов много.
    // В Allure каждый [TestCase] отображается как отдельный тест.
    // ───────────────────────────────────────────────────────────────

    [TestCase("Password123",  true,  TestName = "ValidPassword_Returns_True")]
    [TestCase("password123",  false, TestName = "NoUppercase_Returns_False")]
    [TestCase("PASSWORD123",  true,  TestName = "AllUppercase_With_Digit_Returns_True")]  // IsValid не требует строчных
    [TestCase("PasswordOnly", false, TestName = "NoDigit_Returns_False")]
    [TestCase("Pass1",        false, TestName = "TooShort_Returns_False")]
    [TestCase("",             false, TestName = "Empty_Returns_False")]
    [TestCase("   ",          false, TestName = "Whitespace_Returns_False")]
    [AllureStory("Parametrized password validation")]
    public void IsValid_VariousPasswords_MatchExpected(string password, bool expected)
    {
        var result = _validator.IsValid(password);
        result.Should().Be(expected, $"пароль: '{password}'");
    }

    // ───────────────────────────────────────────────────────────────
    // Тесты силы пароля
    // ───────────────────────────────────────────────────────────────

    [Test]
    [AllureStory("Password strength - weak")]
    [Description("Короткий простой пароль → Weak")]
    public void GetStrength_ShortSimplePassword_ReturnsWeak()
    {
        var strength = _validator.GetStrength("abc");
        strength.Should().Be("Weak");
    }

    [Test]
    [AllureStory("Password strength - medium")]
    [Description("Пароль с цифрой и заглавной, длина 8+ → Medium")]
    public void GetStrength_PasswordWithUpperAndDigit_ReturnsMedium()
    {
        // "Password1" → длина 9 (>=8: +1), заглавная (+1), цифра (+1) = score 3 → Medium
        var strength = _validator.GetStrength("Password1");
        strength.Should().Be("Medium");
    }

    [Test]
    [AllureStory("Password strength - strong")]
    [Description("Длинный пароль с заглавной, цифрой и спецсимволом → Strong")]
    public void GetStrength_LongComplexPassword_ReturnsStrong()
    {
        // "Password123!Long" → длина 16 (>=8: +1, >=12: +1), upper (+1), digit (+1), special (+1) = score 5 → Strong
        var strength = _validator.GetStrength("Password123!Long");
        strength.Should().Be("Strong");
    }

    [Test]
    [AllureStory("Password strength - null input")]
    [Description("Null пароль → Weak (не выбрасывает исключение)")]
    public void GetStrength_NullPassword_ReturnsWeakWithoutException()
    {
        var strength = _validator.GetStrength(null!);
        strength.Should().Be("Weak");
    }
}
