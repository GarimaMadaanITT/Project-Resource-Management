using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Prm.Client.Auth;
using Prm.Client.Models;

namespace Prm.Client.Api;

public sealed class PrmApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly SessionState _session;

    public PrmApiClient(HttpClient http, SessionState session)
    {
        _http = http;
        _session = session;
    }

    public void SyncAuthorizationHeader()
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(_session.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", _session.Token);
    }

    public void ClearAuthorizationHeader() =>
        _http.DefaultRequestHeaders.Authorization = null;

    public Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default) =>
        PostAnonymousAsync<LoginResponse>("/api/auth/login", new LoginRequest(username, password), ct);

    public Task<ChangePasswordResponse> ChangePasswordAsync(string newPassword, string confirmPassword, CancellationToken ct = default) =>
        PostAsync<ChangePasswordResponse>("/api/auth/change-password", new ChangePasswordRequest(newPassword, confirmPassword), ct);

    public Task<EmployeeListResponseModel> GetEmployeesAsync(string? department, string? status, CancellationToken ct = default)
    {
        var query = BuildQuery(("department", department), ("status", status));
        return GetAsync<EmployeeListResponseModel>($"/api/admin/employees{query}", ct);
    }

    public Task<MessageResponse> UpdateEmployeeAsync(int id, string department, string? designation = null, CancellationToken ct = default) =>
        PutAsync<MessageResponse>($"/api/admin/employees/{id}", new { department, designation }, ct);

    public Task<DeactivateEmployeeResponseModel> DeactivateEmployeeAsync(int id, CancellationToken ct = default) =>
        PostAsync<DeactivateEmployeeResponseModel>($"/api/admin/employees/{id}/deactivate", new { }, ct);

    public Task<IReadOnlyList<EmployeeSkillModel>> GetEmployeeSkillsAsync(int id, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EmployeeSkillModel>>($"/api/admin/employees/{id}/skills", ct);

    public Task<EmployeeSkillModel> AddEmployeeSkillAsync(int id, object body, CancellationToken ct = default) =>
        PostAsync<EmployeeSkillModel>($"/api/admin/employees/{id}/skills", body, ct);

    public Task<EmployeeSkillModel> UpdateEmployeeSkillAsync(int id, int skillId, object body, CancellationToken ct = default) =>
        PutAsync<EmployeeSkillModel>($"/api/admin/employees/{id}/skills/{skillId}", body, ct);

    public Task<MessageResponse> RemoveEmployeeSkillAsync(int id, int skillId, CancellationToken ct = default) =>
        DeleteAsync<MessageResponse>($"/api/admin/employees/{id}/skills/{skillId}", ct);

    public Task<MessageResponse> AssignManagerAsync(int id, int managerUserId, CancellationToken ct = default) =>
        PutAsync<MessageResponse>($"/api/admin/employees/{id}/assign-manager", new { managerUserId }, ct);

    public Task<IReadOnlyList<ProjectListItemModel>> GetProjectsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProjectListItemModel>>("/api/admin/projects", ct);

    public Task<ProjectListItemModel> CreateProjectAsync(object body, CancellationToken ct = default) =>
        PostAsync<ProjectListItemModel>("/api/admin/projects", body, ct);

    public Task<ProjectListItemModel> UpdateProjectAsync(int id, object body, CancellationToken ct = default) =>
        PutAsync<ProjectListItemModel>($"/api/admin/projects/{id}", body, ct);

    public Task<IReadOnlyList<MilestoneModel>> GetMilestonesAsync(int projectId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<MilestoneModel>>($"/api/admin/projects/{projectId}/milestones", ct);

    public Task<MilestoneModel> AddMilestoneAsync(int projectId, object body, CancellationToken ct = default) =>
        PostAsync<MilestoneModel>($"/api/admin/projects/{projectId}/milestones", body, ct);

    public Task<MilestoneModel> UpdateMilestoneStatusAsync(int projectId, int milestoneId, string status, CancellationToken ct = default) =>
        PutAsync<MilestoneModel>($"/api/admin/projects/{projectId}/milestones/{milestoneId}/status", new { status }, ct);

    public Task<IReadOnlyList<AllocationListItemModel>> GetAdminAllocationsAsync(int? employeeId, int? projectId, CancellationToken ct = default)
    {
        var query = BuildQuery(("employeeId", employeeId?.ToString()), ("projectId", projectId?.ToString()));
        return GetAsync<IReadOnlyList<AllocationListItemModel>>($"/api/admin/allocations{query}", ct);
    }

    public Task<IReadOnlyList<UserListItemModel>> GetUsersAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<UserListItemModel>>("/api/admin/users", ct);

    public Task<UserListItemModel> CreateUserAsync(object body, CancellationToken ct = default) =>
        PostAsync<UserListItemModel>("/api/admin/users", body, ct);

    public Task<MessageResponse> ResetUserPasswordAsync(int id, string newTemporaryPassword, CancellationToken ct = default) =>
        PostAsync<MessageResponse>($"/api/admin/users/{id}/reset-password", new { newTemporaryPassword }, ct);

    public Task<MessageResponse> DeactivateUserAsync(int id, CancellationToken ct = default) =>
        PostAsync<MessageResponse>($"/api/admin/users/{id}/deactivate", new { }, ct);

    public Task<MessageResponse> ReactivateUserAsync(int id, CancellationToken ct = default) =>
        PostAsync<MessageResponse>($"/api/admin/users/{id}/reactivate", new { }, ct);

    public Task<SystemSettingsModel> GetSettingsAsync(CancellationToken ct = default) =>
        GetAsync<SystemSettingsModel>("/api/admin/settings", ct);

    public Task<SystemSettingsModel> UpdateSettingsAsync(object body, CancellationToken ct = default) =>
        PutAsync<SystemSettingsModel>("/api/admin/settings", body, ct);

    public Task<ManagerDashboardResponseModel> GetManagerDashboardAsync(CancellationToken ct = default) =>
        GetAsync<ManagerDashboardResponseModel>("/api/manager/dashboard", ct);

    public Task<ManagerEmployeeDetailModel> GetManagerEmployeeDetailAsync(int employeeId, CancellationToken ct = default) =>
        GetAsync<ManagerEmployeeDetailModel>($"/api/manager/dashboard/employees/{employeeId}", ct);

    public Task<ManagerAllocationModel> CreateManagerAllocationAsync(object body, CancellationToken ct = default) =>
        PostAsync<ManagerAllocationModel>("/api/manager/allocations", body, ct);

    public Task<EndAllocationResponseModel> EndManagerAllocationAsync(int id, DateOnly? endDate, CancellationToken ct = default) =>
        PostAsync<EndAllocationResponseModel>($"/api/manager/allocations/{id}/end", new { endDate }, ct);

    public Task<IReadOnlyList<ManagerProjectListItemModel>> GetManagerProjectsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ManagerProjectListItemModel>>("/api/manager/projects", ct);

    public Task<ManagerProjectDetailModel> GetManagerProjectDetailAsync(int id, CancellationToken ct = default) =>
        GetAsync<ManagerProjectDetailModel>($"/api/manager/projects/{id}", ct);

    public Task<TeamTimesheetsResponseModel> GetTeamTimesheetsAsync(DateOnly? weekStart, CancellationToken ct = default)
    {
        var query = weekStart is null ? string.Empty : $"?weekStart={weekStart:yyyy-MM-dd}";
        return GetAsync<TeamTimesheetsResponseModel>($"/api/manager/timesheets{query}", ct);
    }

    public Task<ManagerEmployeeTimesheetDetailModel> GetEmployeeTimesheetDetailAsync(int employeeId, DateOnly? weekStart, CancellationToken ct = default)
    {
        var query = weekStart is null ? string.Empty : $"?weekStart={weekStart:yyyy-MM-dd}";
        return GetAsync<ManagerEmployeeTimesheetDetailModel>($"/api/manager/timesheets/employees/{employeeId}{query}", ct);
    }

    public Task<ActivityTagsResponseModel> GetActivityTagsAsync(CancellationToken ct = default) =>
        GetAsync<ActivityTagsResponseModel>("/api/employee/timesheets/activity-tags", ct);

    public Task<MyTimesheetsResponseModel> GetMyTimesheetsAsync(CancellationToken ct = default) =>
        GetAsync<MyTimesheetsResponseModel>("/api/employee/timesheets", ct);

    public Task<TimesheetWeekDetailModel> GetTimesheetWeekAsync(DateOnly weekStart, CancellationToken ct = default) =>
        GetAsync<TimesheetWeekDetailModel>($"/api/employee/timesheets/{weekStart:yyyy-MM-dd}", ct);

    public Task<SubmitTimesheetResponseModel> SubmitTimesheetAsync(object body, CancellationToken ct = default) =>
        PostAsync<SubmitTimesheetResponseModel>("/api/employee/timesheets", body, ct);

    public Task<MyAllocationsResponseModel> GetMyAllocationsAsync(CancellationToken ct = default) =>
        GetAsync<MyAllocationsResponseModel>("/api/employee/allocations", ct);

    public Task<EmployeeReminderResponseModel> GetReminderAsync(CancellationToken ct = default) =>
        GetAsync<EmployeeReminderResponseModel>("/api/employee/reminders", ct);

    public Task<SkillMatchResponseModel> SkillMatchAsync(int projectId, string requirement, CancellationToken ct = default) =>
        PostAsync<SkillMatchResponseModel>("/api/ai/skill-match", new { projectId, requirement }, ct);

    public Task<RiskSummaryResponseModel> GetRiskSummaryAsync(int projectId, CancellationToken ct = default) =>
        GetAsync<RiskSummaryResponseModel>($"/api/ai/risk-summary/{projectId}", ct);

    public Task<TeamBuilderResponseModel> TeamBuilderAsync(string requirement, CancellationToken ct = default) =>
        PostAsync<TeamBuilderResponseModel>("/api/ai/team-builder", new { requirement }, ct);

    public Task<AuditLogListResponseModel> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 20,
        string? entityName = null,
        string? action = null,
        string? source = null,
        CancellationToken ct = default)
    {
        var query = BuildQuery(
            ("page", page.ToString()),
            ("pageSize", pageSize.ToString()),
            ("entityName", entityName),
            ("action", action),
            ("source", source));
        return GetAsync<AuditLogListResponseModel>($"/api/admin/audit-logs{query}", ct);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct) =>
        await ReadSuccessAsync<T>(await _http.GetAsync(path, ct), ct);

    private async Task<T> PostAnonymousAsync<T>(string path, object body, CancellationToken ct) =>
        await ReadSuccessAsync<T>(await _http.PostAsJsonAsync(path, body, JsonOptions, ct), ct);

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct) =>
        await ReadSuccessAsync<T>(await _http.PostAsJsonAsync(path, body, JsonOptions, ct), ct);

    private async Task<T> PutAsync<T>(string path, object body, CancellationToken ct) =>
        await ReadSuccessAsync<T>(await _http.PutAsJsonAsync(path, body, JsonOptions, ct), ct);

    private async Task<T> DeleteAsync<T>(string path, CancellationToken ct) =>
        await ReadSuccessAsync<T>(await _http.DeleteAsync(path, ct), ct);

    private async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return payload ?? throw new InvalidOperationException("Empty API response.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _session.IsAuthenticated)
        {
            _session.Logout();
            ClearAuthorizationHeader();
            throw new SessionExpiredException("Session expired. Please log in again.");
        }

        throw new ApiRequestException(await ApiProblemDetails.ReadErrorAsync(response, ct));
    }

    private static string BuildQuery(params (string Name, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
