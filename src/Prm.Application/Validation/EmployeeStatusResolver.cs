using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class EmployeeStatusResolver
{
    public static EmployeeStatus ResolveFromUtilisation(int utilisationPercent) =>
        utilisationPercent > 0 ? EmployeeStatus.Allocated : EmployeeStatus.Bench;

    public static string ResolveStatusName(int utilisationPercent) =>
        ResolveFromUtilisation(utilisationPercent).ToString();
}
