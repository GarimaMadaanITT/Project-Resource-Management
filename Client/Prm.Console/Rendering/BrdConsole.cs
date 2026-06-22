namespace Prm.Client.Rendering;

public static class BrdConsole
{
    public static void WriteTitle(string title)
    {
        System.Console.WriteLine("╔══════════════════════════════════════════════╗");
        System.Console.WriteLine($"║    {title.PadRight(42)}║");
        System.Console.WriteLine("╚══════════════════════════════════════════════╝");
        System.Console.WriteLine();
    }

    public static void WriteRule(int width = 46) =>
        System.Console.WriteLine(new string('─', width));

    public static char? ReadKeyChoice(string prompt)
    {
        while (true)
        {
            var input = ConsolePrompt.ReadLine(prompt).Trim();
            if (input.Length == 1)
            {
                return char.ToUpperInvariant(input[0]);
            }

            System.Console.WriteLine("Please enter exactly one character representing your choice.");
        }
    }

    public static bool? ReadSaveOrBack()
    {
        while (true)
        {
            var choice = ReadKeyChoice("Choice ([S] Save  [B] Back): ");
            if (choice == 'S')
            {
                return true;
            }

            if (choice == 'B')
            {
                return false;
            }

            System.Console.WriteLine("Please enter S to save or B to go back.");
        }
    }

    public static bool ReadYesDeactivate()
    {
        while (true)
        {
            var choice = ReadKeyChoice("Choice ([Y] Yes, Deactivate  [B] Cancel): ");
            if (choice == 'Y')
            {
                return true;
            }

            if (choice == 'B')
            {
                return false;
            }

            System.Console.WriteLine("Please enter Y to deactivate or B to cancel.");
        }
    }
}
