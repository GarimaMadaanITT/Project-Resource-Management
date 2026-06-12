using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Admin;

public sealed class AdminAuditLogsScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminAuditLogsScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        var page = 1;
        const int pageSize = 15;
        string? sourceFilter = null;

        while (true)
        {
            ScreenHelper.Clear();
            ScreenHelper.WriteHeader("Audit Logs", _app.Session);
            if (!string.IsNullOrWhiteSpace(sourceFilter))
            {
                System.Console.WriteLine($"Filter: source = {sourceFilter}");
            }

            System.Console.WriteLine();

            try
            {
                var data = await _app.Api.GetAuditLogsAsync(
                    page,
                    pageSize,
                    source: sourceFilter,
                    ct: cancellationToken);

                if (data.Items.Count == 0)
                {
                    System.Console.WriteLine("No audit log entries found.");
                }
                else
                {
                    System.Console.WriteLine($"{"When (UTC)",-20} {"Source",-10} {"Entity",-16} {"Action",-22}");
                    BrdConsole.WriteRule();
                    foreach (var item in data.Items)
                    {
                        var when = item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm");
                        System.Console.WriteLine(
                            $"{when,-20} {item.Source,-10} {item.EntityName,-16} {item.Action,-22}");
                    }
                }

                BrdConsole.WriteRule();
                System.Console.WriteLine(
                    $"Page {data.Page} of {Math.Max(1, (int)Math.Ceiling(data.TotalCount / (double)data.PageSize))}  |  Total: {data.TotalCount}");
                System.Console.WriteLine();
                System.Console.WriteLine("[N] Next page   [P] Previous page   [F] Filter by source   [C] Clear filter   [B] Back");
                System.Console.WriteLine();

                var choice = BrdConsole.ReadKeyChoice("Choice: ");
                if (choice is null)
                {
                    continue;
                }

                switch (char.ToUpperInvariant(choice.Value))
                {
                    case 'N':
                        if (page * pageSize < data.TotalCount)
                        {
                            page++;
                        }
                        break;
                    case 'P':
                        if (page > 1)
                        {
                            page--;
                        }
                        break;
                    case 'F':
                        System.Console.WriteLine("Common sources: User, Scheduler");
                        var input = ConsolePrompt.ReadLine("Source filter (blank = any): ");
                        sourceFilter = string.IsNullOrWhiteSpace(input) ? null : input.Trim();
                        page = 1;
                        break;
                    case 'C':
                        sourceFilter = null;
                        page = 1;
                        break;
                    case 'B':
                        return MenuAction.Back;
                }
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
        }
    }
}
