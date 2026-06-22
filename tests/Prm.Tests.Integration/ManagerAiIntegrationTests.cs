using System.Net;
using System.Net.Http.Json;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class ManagerAiIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagerAiIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        var token = await _factory.LoginAsManagerAsync(_client);
        PrmWebApplicationFactory.Authorize(_client, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SkillMatch_Returns_Ranked_Candidates_For_Owned_Project()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var response = await _client.PostAsJsonAsync("/api/ai/skill-match", new
        {
            projectId = alphaId,
            requirement = "Need a backend developer with microservices experience for 20 hrs per week"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SkillMatchResponse>();
        Assert.NotNull(result);
        Assert.Equal("Alpha Portal", result!.ProjectName);
        Assert.True(result.CandidatesConsidered > 0);
        Assert.NotEmpty(result.Matches);
        Assert.True(result.UsedFallbackProvider);
    }

    [Fact]
    public async Task SkillMatch_Returns_400_When_No_Candidate_Has_Capacity()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var response = await _client.PostAsJsonAsync("/api/ai/skill-match", new
        {
            projectId = alphaId,
            requirement = "Need someone for 50 hours per week dedicated support"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SkillMatch_Returns_403_For_Other_Manager_Project()
    {
        var nehaClient = _factory.CreateClient();
        var nehaToken = await _factory.LoginAsync(nehaClient, "neha.joshi", "Manager@1234");
        PrmWebApplicationFactory.Authorize(nehaClient, nehaToken);

        var ankitProjects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = ankitProjects!.First(p => p.Name == "Alpha Portal").Id;

        var response = await nehaClient.PostAsJsonAsync("/api/ai/skill-match", new
        {
            projectId = alphaId,
            requirement = "Need a React developer"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RiskSummary_Returns_Paragraph_For_Owned_Project()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var result = await _client.GetFromJsonAsync<RiskSummaryResponse>($"/api/ai/risk-summary/{alphaId}");

        Assert.NotNull(result);
        Assert.Equal(alphaId, result!.ProjectId);
        Assert.Equal("Alpha Portal", result.ProjectName);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.True(result.UsedFallbackProvider);
    }

    private sealed record ProjectItem(int Id, string Name, DateOnly EndDate, string HealthStatus);
}
