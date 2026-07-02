using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MicroserviceExample.Data;
using MicroserviceExample.Endpoints;
using MicroserviceExample.Models;

namespace MicroserviceExample.Tests;

public class ProjectEndpointsTests
{
    // Each test gets an isolated in-memory store so handlers can be unit-tested
    // without a real Postgres instance.
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateProject_WithValidName_ReturnsCreatedAndTrimsName()
    {
        using var db = NewDb();

        var result = await ProjectEndpoints.CreateProject(new CreateProjectRequest("  My Project  "), db);

        var created = Assert.IsType<Created<Project>>(result.Result);
        Assert.Equal("My Project", created.Value!.Name);
        Assert.True(created.Value.Id > 0);
        Assert.Equal(1, await db.Projects.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProject_WithBlankName_ReturnsValidationProblem(string? name)
    {
        using var db = NewDb();

        var result = await ProjectEndpoints.CreateProject(new CreateProjectRequest(name), db);

        Assert.IsType<ValidationProblem>(result.Result);
        Assert.Equal(0, await db.Projects.CountAsync());
    }

    [Fact]
    public async Task ListProjects_ReturnsProjectsOrderedById()
    {
        using var db = NewDb();
        db.Projects.AddRange(new Project { Name = "a" }, new Project { Name = "b" });
        await db.SaveChangesAsync();

        var result = await ProjectEndpoints.ListProjects(db);

        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("a", result.Value[0].Name);
    }

    [Fact]
    public async Task CreateTask_WhenProjectMissing_ReturnsNotFound()
    {
        using var db = NewDb();

        var result = await ProjectEndpoints.CreateTask(999, new CreateTaskRequest("x"), db);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task CreateTask_WithValidTitle_ReturnsCreatedUnderProject()
    {
        using var db = NewDb();
        var project = new Project { Name = "p" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var result = await ProjectEndpoints.CreateTask(project.Id, new CreateTaskRequest("do it"), db);

        var created = Assert.IsType<Created<TaskItem>>(result.Result);
        Assert.Equal("do it", created.Value!.Title);
        Assert.Equal(project.Id, created.Value.ProjectId);
        Assert.False(created.Value.IsDone);
    }

    [Fact]
    public async Task ListTasks_WhenProjectMissing_ReturnsNotFound()
    {
        using var db = NewDb();

        var result = await ProjectEndpoints.ListTasks(123, db);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task ListTasks_ReturnsOnlyTasksForThatProject()
    {
        using var db = NewDb();
        var p1 = new Project { Name = "p1" };
        var p2 = new Project { Name = "p2" };
        db.Projects.AddRange(p1, p2);
        await db.SaveChangesAsync();
        db.Tasks.AddRange(
            new TaskItem { ProjectId = p1.Id, Title = "t1" },
            new TaskItem { ProjectId = p2.Id, Title = "t2" });
        await db.SaveChangesAsync();

        var result = await ProjectEndpoints.ListTasks(p1.Id, db);

        var ok = Assert.IsType<Ok<List<TaskItem>>>(result.Result);
        Assert.Single(ok.Value!);
        Assert.Equal("t1", ok.Value![0].Title);
    }
}
