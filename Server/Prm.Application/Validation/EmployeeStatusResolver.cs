using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class EmployeeStatusResolver
{
    public static ResourceStatus ResolveFromUtilisation(int utilisationPercent) =>
        utilisationPercent > 0 ? ResourceStatus.Allocated : ResourceStatus.Bench;

    public static string ResolveStatusName(int utilisationPercent) =>
        ResolveFromUtilisation(utilisationPercent).ToString();
}
