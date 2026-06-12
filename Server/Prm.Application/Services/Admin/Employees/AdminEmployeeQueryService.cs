using Prm.Application.Common;

using Prm.Application.DTOs.Admin;

using Prm.Application.Interfaces;

using Prm.Application.Validation;

using Prm.Domain.Enums;



namespace Prm.Application.Services.Admin.Employees;



public class AdminEmployeeQueryService

{

    private readonly IResourceProfileRepository _resourceProfiles;



    public AdminEmployeeQueryService(IResourceProfileRepository resourceProfiles)

    {

        _resourceProfiles = resourceProfiles;

    }



    public async Task<EmployeeListResponse> GetAllAsync(

        string? department,

        string? status,

        CancellationToken cancellationToken = default)

    {

        var statusFilter = ParseResourceStatus(status);

        var resourceProfiles = (await _resourceProfiles.GetAllAsync(department, statusFilter, cancellationToken))

            .Where(profile => profile.User.IsActive)

            .ToList();



        var items = resourceProfiles.Select(profile =>

        {

            var utilisation = ActiveDateHelper.SumActiveUtilisation(profile.Allocations, ActiveDateHelper.TodayUtc);

            return new EmployeeListItemDto(

                profile.Id,

                profile.UserId,

                profile.User.FullName,

                profile.User.Department?.ToString() ?? string.Empty,

                profile.User.Designation?.ToString(),

                EmployeeStatusResolver.ResolveStatusName(utilisation),

                profile.User.IsActive,

                utilisation);

        }).ToList();



        if (statusFilter.HasValue)

        {

            items = items.Where(item => item.Status == statusFilter.Value.ToString()).ToList();

        }



        return new EmployeeListResponse(

            items,

            items.Count,

            items.Count(item => item.Status == ResourceStatus.Allocated.ToString()),

            items.Count(item => item.Status == ResourceStatus.Bench.ToString()));

    }



    private static ResourceStatus? ParseResourceStatus(string? status) =>

        string.IsNullOrWhiteSpace(status) ? null : EnumGuard.Parse<ResourceStatus>(status, "Status");

}


