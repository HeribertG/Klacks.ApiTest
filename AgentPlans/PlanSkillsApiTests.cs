// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Host-level tests for the chat planning skills against the real Program.cs (DB 5434):
 * proves create_plan and get_plan_status are seeded into the skill catalog, and that executing
 * create_plan through the real ISkillExecutor drafts a plan and returns a Confirmation WITHOUT
 * starting execution. The PlanningAgent is stubbed so no real LLM call is made.
 */

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.ApiTest.AgentPlans;

[TestFixture]
public class PlanSkillsApiTests : ApiTestBase
{
    private const string CreatePlanSkill = "create_plan";
    private const string GetPlanStatusSkill = "get_plan_status";
    private const string DraftingStatus = "drafting";

    private const string TwoStepJson =
        "[{\"Order\":1,\"Skill\":\"create_employee\"},{\"Order\":2,\"Skill\":\"create_shift\"}]";

    [Test]
    public async Task NewPlanSkills_AreInTheSkillCatalog()
    {
        AuthorizeAs(Roles.User);

        var createPlan = await Client.GetAsync($"/api/backend/skills/{CreatePlanSkill}");
        var getStatus = await Client.GetAsync($"/api/backend/skills/{GetPlanStatusSkill}");

        createPlan.StatusCode.ShouldBe(HttpStatusCode.OK);
        getStatus.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task CreatePlanSkill_DraftsPlan_AndReturnsConfirmation_WithoutExecuting()
    {
        var userId = Guid.NewGuid();

        var planningAgentStub = Substitute.For<IPlanningAgent>();
        planningAgentStub
            .CreatePlanAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(ci => new AgentPlan
            {
                Id = Guid.NewGuid(),
                UserId = ci.ArgAt<string>(1),
                Goal = "apitest goal",
                StepsJson = TwoStepJson,
                Status = DraftingStatus
            });

        using var host = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddScoped<IPlanningAgent>(_ => planningAgentStub)));

        using var scope = host.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<ISkillExecutor>();

        var context = new SkillExecutionContext
        {
            UserId = userId,
            TenantId = Guid.Empty,
            UserName = "apitest",
            UserPermissions = new List<string>()
        };
        var invocation = new SkillInvocation
        {
            SkillName = CreatePlanSkill,
            Parameters = new Dictionary<string, object>
            {
                ["goal"] = "create a customer and an order and assign staff"
            }
        };

        var result = await executor.ExecuteAsync(invocation, context);

        result.Type.ShouldBe(SkillResultType.Confirmation);
        result.Success.ShouldBeFalse();
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.ShouldContainKey("confirmationToken");

        var persisted = await DbContext.AgentPlans.AsNoTracking()
            .Where(p => p.UserId == userId.ToString())
            .ToListAsync();

        persisted.ShouldNotBeEmpty();
        persisted.ShouldAllBe(p => p.Status == DraftingStatus);

        await DbContext.AgentPlans
            .Where(p => p.UserId == userId.ToString())
            .ExecuteDeleteAsync();
    }
}
