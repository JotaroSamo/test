namespace Tests.Helpers;

// Вспомогательный класс — простая бизнес-логика для демонстрации юнит-тестов.
// В реальном AQA-проекте здесь могут быть: генераторы данных, парсеры ответов, утилиты.
public class PasswordValidator
{
    // Проверяет что пароль соответствует минимальным требованиям
    public bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsDigit)) return false;
        return true;
    }

    // Вычисляет "силу" пароля: Weak / Medium / Strong
    public string GetStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return "Weak";

        int score = 0;
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => "!@#$%^&*".Contains(c))) score++;

        return score switch
        {
            <= 2 => "Weak",
            3 => "Medium",
            _ => "Strong"
        };
    }
}
