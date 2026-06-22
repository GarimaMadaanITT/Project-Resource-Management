using Prm.Client.Api;
using Prm.Client.Core;
using Prm.Client.Models;
using Prm.Client.Navigation;
using Prm.Client.Rendering;

namespace Prm.Client.Screens.Admin;

public sealed class AdminEmployeesMenuScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminEmployeesMenuScreen(ConsoleApp app) => _app = app;

    public Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        BrdConsole.WriteTitle("MANAGE EMPLOYEES");
        System.Console.WriteLine("1. View All Employees");
        System.Console.WriteLine("2. Update Employee");
        System.Console.WriteLine("3. Deactivate Employee");
        System.Console.WriteLine("4. Manage Employee Skills");
        System.Console.WriteLine("5. Assign Manager");
        System.Console.WriteLine("6. Back");
        System.Console.WriteLine();

        return Task.FromResult(ConsolePrompt.ReadMenuChoice(1, 6) switch
        {
            1 => Push(new AdminViewEmployeesScreen(_app)),
            2 => Push(new AdminUpdateEmployeeScreen(_app)),
            3 => Push(new AdminDeactivateEmployeeScreen(_app)),
            4 => Push(new AdminManageEmployeeSkillsScreen(_app)),
            5 => Push(new AdminAssignManagerScreen(_app)),
            6 => MenuAction.Back,
            _ => MenuAction.None
        });
    }

    private MenuAction Push(IMenuScreen s)
    {
        _app.Navigator.Push(s);
        return MenuAction.None;
    }
}

public sealed class AdminViewEmployeesScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminViewEmployeesScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        string? department = null;
        string? status = null;

        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("ALL EMPLOYEES");
            System.Console.WriteLine($"{"ID",4}  {"Name",-18} {"Department",-12} {"Designation",-22} {"Status",10}");
            BrdConsole.WriteRule();

            try
            {
                var data = await _app.Api.GetEmployeesAsync(department, status, cancellationToken);
                foreach (var employee in data.Employees)
                {
                    var designation = string.IsNullOrWhiteSpace(employee.Designation) ? "-" : employee.Designation;
                    System.Console.WriteLine(
                        $"{employee.Id,4}  {employee.Name,-18} {employee.Department,-12} {designation,-22} {employee.Status.ToUpperInvariant(),10}");
                }

                BrdConsole.WriteRule();
                System.Console.WriteLine(
                    $"Total: {data.Total}   |   Allocated: {data.AllocatedCount}   |   Bench: {data.BenchCount}");
                System.Console.WriteLine();
                System.Console.WriteLine("[F] Filter by Status / Department     [B] Back");
                System.Console.WriteLine();

                var choice = BrdConsole.ReadKeyChoice("Choice: ");
                if (choice == 'B')
                {
                    return MenuAction.Back;
                }

                if (choice == 'F')
                {
                    department = ConsolePrompt.ReadLine("Department (blank = any): ");
                    if (string.IsNullOrWhiteSpace(department))
                    {
                        department = null;
                    }

                    System.Console.WriteLine("Status: 1=Bench  2=Allocated  3=Any");
                    var statusChoice = ConsolePrompt.ReadInt("Status filter: ", min: 1, max: 3);
                    status = statusChoice switch { 1 => "Bench", 2 => "Allocated", _ => null };
                    continue;
                }

                System.Console.WriteLine("Please enter F to filter or B to go back.");
                ScreenHelper.Pause();
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

public sealed class AdminUpdateEmployeeScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminUpdateEmployeeScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<EmployeeListItemModel> employees;
        try
        {
            var data = await _app.Api.GetEmployeesAsync(null, null, cancellationToken);
            employees = data.Employees;
        }
        catch (ApiRequestException ex)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("UPDATE EMPLOYEE");
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
            return MenuAction.Back;
        }

        ScreenHelper.Clear();
        BrdConsole.WriteTitle("UPDATE EMPLOYEE");
        System.Console.WriteLine($"{"ID",4}  {"Name",-18} {"Department",-12} {"Designation",-22} {"Status",10}");
        BrdConsole.WriteRule();
        foreach (var item in employees)
        {
            var designation = string.IsNullOrWhiteSpace(item.Designation) ? "-" : item.Designation;
            System.Console.WriteLine(
                $"{item.Id,4}  {item.Name,-18} {item.Department,-12} {designation,-22} {item.Status.ToUpperInvariant(),10}");
        }

        BrdConsole.WriteRule();
        System.Console.WriteLine();
        var id = ConsolePrompt.ReadInt("Employee ID: ");

        try
        {
            var employee = employees.FirstOrDefault(item => item.Id == id);
            if (employee is null)
            {
                System.Console.WriteLine("Employee not found.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            if (!employee.IsActive)
            {
                System.Console.WriteLine("Inactive or deactivated employees cannot be updated.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            System.Console.WriteLine($"Current department : {employee.Department}");
            System.Console.WriteLine($"Current designation: {employee.Designation ?? "-"}");
            System.Console.WriteLine();

            var dept = ConsolePrompt.ReadLine("New department (blank = keep current): ");
            if (string.IsNullOrWhiteSpace(dept))
            {
                dept = employee.Department;
            }

            var designationInput = ConsolePrompt.ReadLine("New designation (blank = keep current): ");
            string? designation = string.IsNullOrWhiteSpace(designationInput) ? null : designationInput.Trim();

            var result = await _app.Api.UpdateEmployeeAsync(id, dept, designation, cancellationToken);
            ScreenHelper.WriteSuccess(result.Message);
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminDeactivateEmployeeScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminDeactivateEmployeeScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        ScreenHelper.Clear();
        BrdConsole.WriteTitle("DEACTIVATE EMPLOYEE");
        var id = ConsolePrompt.ReadInt("Enter Employee ID: ");

        EmployeeListItemModel? employee;
        try
        {
            var employees = await _app.Api.GetEmployeesAsync(null, null, cancellationToken);
            employee = employees.Employees.FirstOrDefault(item => item.Id == id);
            if (employee is null)
            {
                System.Console.WriteLine("Employee not found.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
            return MenuAction.Back;
        }

        IReadOnlyList<AllocationListItemModel> allocations;
        try
        {
            allocations = await _app.Api.GetAdminAllocationsAsync(id, null, cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
            return MenuAction.Back;
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"── {employee.Name} ─────────────────────────────────");
        System.Console.WriteLine($"Department  : {employee.Department}");
        System.Console.WriteLine($"Designation : {employee.Designation ?? "-"}");
        System.Console.WriteLine($"Status      : {employee.Status.ToUpperInvariant()} ({employee.UtilisationPercent}%)");
        System.Console.WriteLine();

        if (allocations.Count > 0)
        {
            System.Console.WriteLine($"⚠  Warning: This employee has {allocations.Count} active allocation(s).");
            System.Console.WriteLine("   Ending their employment will remove them from:");
            foreach (var allocation in allocations)
            {
                System.Console.WriteLine(
                    $"     - {allocation.ProjectName}  ({allocation.UtilisationPercent}%,  ends {ScreenHelper.FormatDate(allocation.ToDate)})");
            }

            System.Console.WriteLine();
        }

        System.Console.WriteLine($"Are you sure you want to deactivate {employee.Name}?");
        System.Console.WriteLine("This will: set is_active = false, end all active allocations today,");
        System.Console.WriteLine("and block their login account.");
        System.Console.WriteLine();

        if (!BrdConsole.ReadYesDeactivate())
        {
            return MenuAction.Back;
        }

        try
        {
            var result = await _app.Api.DeactivateEmployeeAsync(id, cancellationToken);
            ScreenHelper.WriteSuccess("Employee deactivated.");
            if (result.EndedAllocations > 0)
            {
                System.Console.WriteLine($"Ended allocations: {result.EndedAllocations}");
            }
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }

        ScreenHelper.Pause();
        return MenuAction.Back;
    }
}

public sealed class AdminAssignManagerScreen : IMenuScreen
{
    private readonly ConsoleApp _app;

    public AdminAssignManagerScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("ASSIGN MANAGER");
            var employeeUserId = ConsolePrompt.ReadInt("Employee User ID : ");
            var managerUserId = ConsolePrompt.ReadInt("Manager User ID  : ");
            BrdConsole.WriteRule();
            System.Console.WriteLine("[S] Save     [B] Back");
            System.Console.WriteLine();

            var action = BrdConsole.ReadSaveOrBack();
            if (action == false)
            {
                return MenuAction.Back;
            }

            if (action != true)
            {
                continue;
            }

            try
            {
                var users = await _app.Api.GetUsersAsync(cancellationToken);
                var employeeUser = users.FirstOrDefault(user => user.Id == employeeUserId);
                var managerUser = users.FirstOrDefault(user => user.Id == managerUserId);

                if (employeeUser is null)
                {
                    System.Console.WriteLine("Employee user not found.");
                    ScreenHelper.Pause();
                    continue;
                }

                if (managerUser is null)
                {
                    System.Console.WriteLine("Manager user not found.");
                    ScreenHelper.Pause();
                    continue;
                }

                if (!employeeUser.Role.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine("Only employees can have a reporting manager.");
                    ScreenHelper.Pause();
                    continue;
                }

                if (!managerUser.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine("Selected manager user must have the Manager role.");
                    ScreenHelper.Pause();
                    continue;
                }

                if (employeeUserId == managerUserId)
                {
                    System.Console.WriteLine("A manager cannot be assigned to themselves.");
                    ScreenHelper.Pause();
                    continue;
                }

                var employees = await _app.Api.GetEmployeesAsync(null, null, cancellationToken);
                var employee = employees.Employees.FirstOrDefault(item => item.UserId == employeeUserId);
                if (employee is null)
                {
                    System.Console.WriteLine("Employee profile not found for the selected user.");
                    ScreenHelper.Pause();
                    continue;
                }

                var result = await _app.Api.AssignManagerAsync(employee.Id, managerUserId, cancellationToken);
                ScreenHelper.WriteSuccess(result.Message);
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
            }
        }
    }
}

public sealed class AdminManageEmployeeSkillsScreen : IMenuScreen
{
    private readonly ConsoleApp _app;
    private int? _employeeId;
    private string? _employeeName;

    public AdminManageEmployeeSkillsScreen(ConsoleApp app) => _app = app;

    public async Task<MenuAction> RunAsync(CancellationToken cancellationToken)
    {
        if (_employeeId is null)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("MANAGE SKILLS");
            var id = ConsolePrompt.ReadInt("Enter Employee ID: ");
            try
            {
                var employees = await _app.Api.GetEmployeesAsync(null, null, cancellationToken);
                var employee = employees.Employees.FirstOrDefault(item => item.Id == id);
                if (employee is null)
                {
                    await _app.Api.GetEmployeeSkillsAsync(id, cancellationToken);
                    _employeeName = $"Employee {id}";
                }
                else
                {
                    _employeeName = employee.Name;
                }

                _employeeId = id;
            }
            catch (ApiRequestException)
            {
                System.Console.WriteLine("Employee not found.");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }
        }

        while (true)
        {
            ScreenHelper.Clear();
            BrdConsole.WriteTitle("MANAGE SKILLS");
            System.Console.WriteLine($"── {_employeeName} ─────────────────────────────────");

            IReadOnlyList<EmployeeSkillModel> skills;
            try
            {
                skills = await _app.Api.GetEmployeeSkillsAsync(_employeeId!.Value, cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                ScreenHelper.Pause();
                return MenuAction.Back;
            }

            System.Console.WriteLine("Current Skills:");
            if (skills.Count == 0)
            {
                System.Console.WriteLine("  (none)");
            }
            else
            {
                for (var i = 0; i < skills.Count; i++)
                {
                    var skill = skills[i];
                    System.Console.WriteLine($"  {i + 1,2}.  {skill.Name,-18} {skill.Proficiency}");
                }
            }

            BrdConsole.WriteRule();
            System.Console.WriteLine("1. Add Skill");
            System.Console.WriteLine("2. Update Proficiency Level");
            System.Console.WriteLine("3. Remove Skill");
            System.Console.WriteLine("4. Back");
            System.Console.WriteLine();

            switch (ConsolePrompt.ReadMenuChoice(1, 4))
            {
                case 1:
                    await AddSkillAsync(cancellationToken);
                    break;
                case 2:
                    await UpdateSkillAsync(skills, cancellationToken);
                    break;
                case 3:
                    await RemoveSkillAsync(skills, cancellationToken);
                    break;
                case 4:
                    return MenuAction.Back;
            }
        }
    }

    private async Task AddSkillAsync(CancellationToken cancellationToken)
    {
        System.Console.WriteLine();
        var name = ConsolePrompt.ReadLine("Skill Name        : ");
        var (category, customCategory) = AdminSkillInput.ReadCategory();
        var proficiency = AdminSkillInput.ReadProficiency();

        try
        {
            await _app.Api.AddEmployeeSkillAsync(
                _employeeId!.Value,
                new { skillName = name, category, proficiency, customCategory },
                cancellationToken);
            ScreenHelper.WriteSuccess("Skill added.");
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }
    }

    private async Task UpdateSkillAsync(IReadOnlyList<EmployeeSkillModel> skills, CancellationToken cancellationToken)
    {
        if (skills.Count == 0)
        {
            System.Console.WriteLine("No skills to update.");
            ScreenHelper.Pause();
            return;
        }

        System.Console.WriteLine();
        var skillNumber = ConsolePrompt.ReadInt("Enter Skill # : ", min: 1, max: skills.Count);
        var skill = skills[skillNumber - 1];
        var proficiency = AdminSkillInput.ReadProficiency();

        try
        {
            await _app.Api.UpdateEmployeeSkillAsync(
                _employeeId!.Value,
                skill.SkillId,
                new { proficiency },
                cancellationToken);
            ScreenHelper.WriteSuccess("Proficiency updated.");
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }
    }

    private async Task RemoveSkillAsync(IReadOnlyList<EmployeeSkillModel> skills, CancellationToken cancellationToken)
    {
        if (skills.Count == 0)
        {
            System.Console.WriteLine("No skills to remove.");
            ScreenHelper.Pause();
            return;
        }

        System.Console.WriteLine();
        var skillNumber = ConsolePrompt.ReadInt("Enter Skill # : ", min: 1, max: skills.Count);
        var skill = skills[skillNumber - 1];

        if (!ConsolePrompt.ReadYesNo("Remove this skill?"))
        {
            return;
        }

        try
        {
            var result = await _app.Api.RemoveEmployeeSkillAsync(_employeeId!.Value, skill.SkillId, cancellationToken);
            ScreenHelper.WriteSuccess(result.Message);
            ScreenHelper.Pause();
        }
        catch (ApiRequestException ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            ScreenHelper.Pause();
        }
    }
}

internal static class AdminSkillInput
{
    public static (string Category, string? CustomCategory) ReadCategory()
    {
        System.Console.WriteLine("Category          : (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
        var choice = ConsolePrompt.ReadInt("Enter choice      : ", 1, 5);
        if (choice == 5)
        {
            var custom = ConsolePrompt.ReadLine("Enter Custom Category: ");
            while (string.IsNullOrWhiteSpace(custom))
            {
                System.Console.WriteLine("Custom category cannot be empty.");
                custom = ConsolePrompt.ReadLine("Enter Custom Category: ");
            }

            return ("Other", custom.Trim());
        }

        return choice switch
        {
            1 => ("Backend", null),
            2 => ("Frontend", null),
            3 => ("DevOps", null),
            4 => ("Qa", null),
            _ => ("Other", null)
        };
    }

    public static string ReadProficiency()
    {
        System.Console.WriteLine("Proficiency Level : (1) Beginner  (2) Intermediate  (3) Advanced");
        return ConsolePrompt.ReadInt("Enter choice      : ", 1, 3) switch
        {
            1 => "Beginner",
            2 => "Intermediate",
            3 => "Advanced",
            _ => "Beginner"
        };
    }
}
