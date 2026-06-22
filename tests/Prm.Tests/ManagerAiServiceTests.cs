using Moq;

using Prm.Application.Common;

using Prm.Application.DTOs.Manager;

using Prm.Application.Interfaces;

using Prm.Application.Services.Manager;

using Prm.Domain.Entities;

using Prm.Domain.Enums;

using Microsoft.Extensions.Logging.Abstractions;



namespace Prm.Tests;



public class AiRequirementParserTests

{

    [Theory]

    [InlineData("Need 10 hrs/week for UI testing", 10)]

    [InlineData("about 15 hours per week", 15)]

    [InlineData("full stack developer for 6 months", null)]

    public void Parse_Extracts_Hours_When_Present(string requirement, int? expectedHours)

    {

        var parsed = Application.Validation.AiRequirementParser.Parse(requirement);

        Assert.Equal(expectedHours, parsed.HoursPerWeek);

    }

}



public class ManagerAiServiceTests

{

    [Fact]

    public async Task SkillMatchAsync_Returns_Matches_From_Llm_Response()

    {

        var managerUserId = 2;

        var projectId = 1;



        var project = new Project

        {

            Id = projectId,

            Name = "Alpha Portal",

            ManagerUserId = managerUserId,

            Status = ProjectStatus.Active,

            HealthStatus = HealthStatus.AtRisk,

            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),

            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))

        };



        var resourceProfile = BuildResourceProfile(10, managerUserId, "Dev Patel", ["Java", "Microservices"]);

        resourceProfile.Allocations = [];



        var context = new Mock<IManagerContextService>();

        context.Setup(service => service.ResolveAsync(managerUserId, It.IsAny<CancellationToken>()))

            .ReturnsAsync(new ManagerContext(managerUserId));



        var projects = new Mock<IProjectRepository>();

        projects.Setup(repo => repo.GetByIdWithAllocationsAsync(projectId, It.IsAny<CancellationToken>()))

            .ReturnsAsync(project);



        var resourceProfiles = new Mock<IResourceProfileRepository>();

        resourceProfiles.Setup(repo => repo.GetOrgWideCandidatesAsync(It.IsAny<CancellationToken>()))

            .ReturnsAsync(new List<ResourceProfile> { resourceProfile });



        var timesheets = new Mock<ITimesheetRepository>();

        timesheets.Setup(repo => repo.GetRecentActivityTagsAsync(resourceProfile.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(new List<string> { "Backend API Development" });



        var settings = new Mock<ISystemSettingsRepository>();

        settings.Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))

            .ReturnsAsync(new SystemSetting { MaxWeeklyHours = 40 });



        var llm = new Mock<ILlmCompletionService>();

        llm.Setup(service => service.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(new LlmCompletionResult(

                """{"matches":[{"employeeId":10,"reason":"Strong microservices fit."}]}""",

                UsedFallbackProvider: false));



        var service = new ManagerAiService(

            context.Object,

            projects.Object,

            resourceProfiles.Object,

            timesheets.Object,

            settings.Object,

            llm.Object,

            NullLogger<ManagerAiService>.Instance);



        var response = await service.SkillMatchAsync(

            managerUserId,

            new SkillMatchRequest(projectId, "Need Java microservices developer"));



        Assert.Single(response.Matches);

        Assert.Equal(10, response.Matches[0].EmployeeId);

        Assert.Equal("Dev Patel", response.Matches[0].EmployeeName);

        Assert.False(response.UsedFallbackProvider);

    }



    [Fact]

    public async Task SkillMatchAsync_Uses_OrgWide_Candidates_Outside_Manager_Team()

    {

        var managerUserId = 2;

        var projectId = 1;

        var otherManagerId = 99;



        var project = new Project

        {

            Id = projectId,

            Name = "Alpha Portal",

            ManagerUserId = managerUserId,

            Status = ProjectStatus.Active,

            HealthStatus = HealthStatus.OnTrack,

            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),

            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))

        };



        var orgWideEmployee = BuildResourceProfile(20, otherManagerId, "Org Wide Dev", ["React", "TypeScript"]);



        var context = new Mock<IManagerContextService>();

        context.Setup(service => service.ResolveAsync(managerUserId, It.IsAny<CancellationToken>()))

            .ReturnsAsync(new ManagerContext(managerUserId));



        var projects = new Mock<IProjectRepository>();

        projects.Setup(repo => repo.GetByIdWithAllocationsAsync(projectId, It.IsAny<CancellationToken>()))

            .ReturnsAsync(project);



        var resourceProfiles = new Mock<IResourceProfileRepository>();

        resourceProfiles.Setup(repo => repo.GetOrgWideCandidatesAsync(It.IsAny<CancellationToken>()))

            .ReturnsAsync(new List<ResourceProfile> { orgWideEmployee });



        var timesheets = new Mock<ITimesheetRepository>();

        timesheets.Setup(repo => repo.GetRecentActivityTagsAsync(orgWideEmployee.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(new List<string>());



        var settings = new Mock<ISystemSettingsRepository>();

        settings.Setup(repo => repo.GetAsync(It.IsAny<CancellationToken>()))

            .ReturnsAsync(new SystemSetting { MaxWeeklyHours = 40 });



        var llm = new Mock<ILlmCompletionService>();

        llm.Setup(service => service.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(new LlmCompletionResult(

                """{"matches":[{"employeeId":20,"reason":"Strong React fit."}]}""",

                UsedFallbackProvider: false));



        var service = new ManagerAiService(

            context.Object,

            projects.Object,

            resourceProfiles.Object,

            timesheets.Object,

            settings.Object,

            llm.Object,

            NullLogger<ManagerAiService>.Instance);



        var response = await service.SkillMatchAsync(

            managerUserId,

            new SkillMatchRequest(projectId, "Need React developer"));



        Assert.Single(response.Matches);

        Assert.Equal(20, response.Matches[0].EmployeeId);

        Assert.Equal("Org Wide Dev", response.Matches[0].EmployeeName);

        resourceProfiles.Verify(repo => repo.GetOrgWideCandidatesAsync(It.IsAny<CancellationToken>()), Times.Once);

        resourceProfiles.Verify(

            repo => repo.GetTeamByManagerUserIdAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),

            Times.Never);

    }



    [Fact]

    public async Task TeamBuilderAsync_Returns_Validated_Roles_From_Llm_Response()

    {

        var managerUserId = 2;

        var anil = BuildTeamBuilderResourceProfile(

            3,

            "Anil Mehta",

            ["Docker", "Kubernetes"],

            []);

        var dev = BuildTeamBuilderResourceProfile(

            5,

            "Dev Patel",

            ["Java", "Spring Boot"],

            [new Allocation

            {

                UtilisationPercent = 50,

                FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),

                ToDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),

                Project = new Project { Name = "Beta CRM" }

            }]);



        var context = new Mock<IManagerContextService>();

        context.Setup(service => service.ResolveAsync(managerUserId, It.IsAny<CancellationToken>()))

            .ReturnsAsync(new ManagerContext(managerUserId));



        var resourceProfiles = new Mock<IResourceProfileRepository>();

        resourceProfiles.Setup(repo => repo.GetOrgWideCandidatesAsync(It.IsAny<CancellationToken>()))

            .ReturnsAsync(new List<ResourceProfile> { anil, dev });



        var llm = new Mock<ILlmCompletionService>();

        llm.Setup(service => service.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(new LlmCompletionResult(

                """

                {

                  "roles": [

                    {

                      "roleTitle": "DevOps Engineer",

                      "status": "FILLED",

                      "requiredSkills": [{"skillName":"Docker","minProficiency":"INTERMEDIATE"}],

                      "assignedEmployeeName": "Anil Mehta",

                      "matchScore": 92,

                      "reason": "Docker advanced; fully benched."

                    },

                    {

                      "roleTitle": "QA Tester",

                      "status": "GAP",

                      "requiredSkills": [{"skillName":"Selenium","minProficiency":"BEGINNER"}],

                      "gap": {

                        "reasonType": "NO_SKILL",

                        "message": "No employee has Selenium skills."

                      }

                    }

                  ]

                }

                """,

                UsedFallbackProvider: false));



        var service = new ManagerAiService(

            context.Object,

            new Mock<IProjectRepository>().Object,

            resourceProfiles.Object,

            new Mock<ITimesheetRepository>().Object,

            new Mock<ISystemSettingsRepository>().Object,

            llm.Object,

            NullLogger<ManagerAiService>.Instance);



        var response = await service.TeamBuilderAsync(

            managerUserId,

            new TeamBuilderRequest("Need DevOps and QA for banking portal"));



        Assert.Equal(2, response.Roles.Count);

        Assert.Equal("FILLED", response.Roles[0].Status);

        Assert.Equal("Anil Mehta", response.Roles[0].AssignedEmployeeName);

        Assert.Equal(1, response.AssignableCandidates);

        Assert.False(response.UsedFallbackProvider);

    }



    private static ResourceProfile BuildResourceProfile(int id, int managerUserId, string name, IReadOnlyList<string> skills)

    {

        var user = TestDataHelpers.CreateUser(UserRole.Employee, fullName: name);

        user.Id = id;

        user.Username = name.Replace(" ", ".").ToLowerInvariant();

        user.Email = $"{name.Replace(" ", ".").ToLowerInvariant()}@example.com";

        user.Department = "Engineering";

        user.Skills = skills.Select((skill, index) => new UserSkill

        {

            UserId = id,

            SkillId = index + 1,

            Skill = new Skill { Id = index + 1, Name = skill, Category = SkillCategory.Backend }

        }).ToList();



        return new ResourceProfile

        {

            Id = id,

            UserId = id,

            ManagerUserId = managerUserId,

            User = user,

            Allocations = []

        };

    }



    private static ResourceProfile BuildTeamBuilderResourceProfile(

        int id,

        string name,

        IReadOnlyList<string> skills,

        IReadOnlyList<Allocation> allocations)

    {

        var user = TestDataHelpers.CreateUser(UserRole.Employee, fullName: name);

        user.Id = id;

        user.Username = name.Replace(" ", ".").ToLowerInvariant();

        user.Email = $"{name.Replace(" ", ".").ToLowerInvariant()}@example.com";

        user.Department = "Engineering";

        user.Skills = skills.Select((skill, index) => new UserSkill

        {

            UserId = id,

            SkillId = index + 1,

            Skill = new Skill { Id = index + 1, Name = skill, Category = SkillCategory.Backend },

            Proficiency = ProficiencyLevel.Intermediate

        }).ToList();



        return new ResourceProfile

        {

            Id = id,

            UserId = id,

            User = user,

            Allocations = allocations.ToList()

        };

    }



    [Fact]

    public async Task TeamBuilderAsync_Offline_Path_Fills_Benched_Java_Developer()

    {

        var managerUserId = 2;

        var javaBench = BuildTeamBuilderResourceProfile(9, "Java Bench Dev", ["Java"], []);

        javaBench.UserId = 9;

        javaBench.User.Id = 9;



        var context = new Mock<IManagerContextService>();

        context.Setup(service => service.ResolveAsync(managerUserId, It.IsAny<CancellationToken>()))

            .ReturnsAsync(new ManagerContext(managerUserId));



        var resourceProfiles = new Mock<IResourceProfileRepository>();

        resourceProfiles.Setup(repo => repo.GetOrgWideCandidatesAsync(It.IsAny<CancellationToken>()))

            .ReturnsAsync(new List<ResourceProfile> { javaBench });



        var llm = new Mock<ILlmCompletionService>();

        llm.Setup(service => service.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(new LlmCompletionResult("{}", UsedFallbackProvider: true));



        var service = new ManagerAiService(

            context.Object,

            new Mock<IProjectRepository>().Object,

            resourceProfiles.Object,

            new Mock<ITimesheetRepository>().Object,

            new Mock<ISystemSettingsRepository>().Object,

            llm.Object,

            NullLogger<ManagerAiService>.Instance);



        var response = await service.TeamBuilderAsync(

            managerUserId,

            new TeamBuilderRequest("I need a java developer"));



        Assert.Single(response.Roles);

        Assert.Equal(TeamBuilderConstants.StatusFilled, response.Roles[0].Status);

        Assert.Equal("Java Bench Dev", response.Roles[0].AssignedEmployeeName);

        Assert.NotEmpty(response.Roles[0].BenchMatches);

    }

}


