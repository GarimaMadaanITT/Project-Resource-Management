using Prm.Client.Api;
using Prm.Client.Auth;

namespace Prm.Client.Rendering;

public static class ScreenHelper
{
    public static void Clear() => System.Console.Clear();

    public static void WriteHeader(string title, SessionState session)
    {
        System.Console.WriteLine("========================================");
        System.Console.WriteLine($"   {title}");
        System.Console.WriteLine("========================================");
        if (session.User is not null)
        {
            System.Console.WriteLine($"User: {session.User.FullName} ({session.User.Username}) — {session.User.Role}");
        }

        System.Console.WriteLine();
    }

    public static void WriteSuccess(string message)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"{message} ✓");
    }

    public static void Pause(string message = "Press Enter to continue...")
    {
        System.Console.WriteLine();
        System.Console.WriteLine(message);
        System.Console.ReadLine();
    }

    public static async Task RunApiAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"Error: {ex.Message}");
            Pause();
        }
        catch (SessionExpiredException ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(ex.Message);
            Pause();
        }
    }

    public static string FormatDate(DateOnly date) => date.ToString("dd-MMM-yyyy");

    public static string FormatHealthStatus(string? status)
    {
        var normalized = status?
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant() ?? "UNKNOWN";

        return normalized switch
        {
            "ONTRACK" => "\u001b[32m● ON TRACK\u001b[0m",
            "ATTENTION" => "\u001b[33m● ATTENTION\u001b[0m",
            "ATRISK" => "\u001b[31m● AT RISK\u001b[0m",
            _ => $"○ {status ?? "UNKNOWN"}"
        };
    }
}

public static class ConsolePrompt
{
    public static string ReadLine(string label, bool secret = false)
    {
        System.Console.Write(label);

        if (!secret)
        {
            return System.Console.ReadLine()?.Trim() ?? string.Empty;
        }

        var password = string.Empty;
        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                System.Console.Write("\b \b");
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                System.Console.Write('*');
            }
        }

        return password;
    }

    public static int ReadInt(string label, int? min = null, int? max = null)
    {
        while (true)
        {
            var input = ReadLine(label);
            if (!int.TryParse(input, out var value))
            {
                System.Console.WriteLine("Please enter a valid number.");
                continue;
            }

            if (min is not null && value < min)
            {
                System.Console.WriteLine($"Value must be at least {min}.");
                continue;
            }

            if (max is not null && value > max)
            {
                System.Console.WriteLine($"Value must be at most {max}.");
                continue;
            }

            return value;
        }
    }

    public static decimal ReadDecimal(string label)
    {
        while (true)
        {
            var input = ReadLine(label);
            if (decimal.TryParse(input, out var value) && value >= 0)
            {
                return value;
            }

            System.Console.WriteLine("Please enter a valid non-negative number.");
        }
    }

    public static DateOnly ReadDate(string label)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        while (true)
        {
            var input = ReadLine($"{label} (yyyy-MM-dd): ");
            if (DateOnly.TryParse(input, out var date))
            {
                if (date < today)
                {
                    System.Console.WriteLine("Date cannot be in the past.");
                    continue;
                }

                return date;
            }

            System.Console.WriteLine("Invalid date. Use yyyy-MM-dd.");
        }
    }

    public static int ReadMenuChoice(int min, int max)
    {
        while (true)
        {
            var choice = ReadInt("Enter choice: ");
            if (choice >= min && choice <= max)
            {
                return choice;
            }

            System.Console.WriteLine($"Please enter a number between {min} and {max}.");
        }
    }

    public static bool ReadYesNo(string prompt)
    {
        while (true)
        {
            var input = ReadLine($"{prompt} (Y/N): ").Trim().ToUpperInvariant();
            if (input is "Y" or "YES")
            {
                return true;
            }

            if (input is "N" or "NO")
            {
                return false;
            }

            System.Console.WriteLine("Please enter Y or N.");
        }
    }
}

public static class ConsoleTable
{
    public static void PrintRows(IEnumerable<string> rows)
    {
        foreach (var row in rows)
        {
            System.Console.WriteLine(row);
        }
    }
}
