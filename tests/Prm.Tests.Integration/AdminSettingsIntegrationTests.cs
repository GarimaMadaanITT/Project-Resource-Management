using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminSettingsIntegrationTests : PrmIntegrationTestBase
{
    public AdminSettingsIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_Returns_Default_Settings()
    {
        var settings = await Client.GetFromJsonAsync<SettingsDto>("/api/admin/settings");
        Assert.NotNull(settings);
        Assert.Equal("Gemini", settings!.LlmProvider);
        Assert.Equal(4, settings.SchedulerIntervalHours);
        Assert.Equal(40, settings.MaxWeeklyHours);
    }

    [Fact]
    public async Task Update_SchedulerInterval_Succeeds()
    {
        var response = await Client.PutAsJsonAsync("/api/admin/settings", new { schedulerIntervalHours = 6 });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<SettingsDto>();
        Assert.Equal(6, settings!.SchedulerIntervalHours);
    }

    [Fact]
    public async Task Update_With_Invalid_SchedulerInterval_Returns_400()
    {
        var response = await Client.PutAsJsonAsync("/api/admin/settings", new { schedulerIntervalHours = 0 });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    private sealed record SettingsDto(string LlmProvider, string LlmApiKeyMasked, int SchedulerIntervalHours, int MaxWeeklyHours);
}
