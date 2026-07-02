using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MicroserviceExample.Data;
using MicroserviceExample.Models;

namespace MicroserviceExample.Endpoints;

public record CreateProjectRequest(string? Name);
public record CreateTaskRequest(string? Title);

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/projects").WithTags("Projects");
        group.MapGet("/", ListProjects).WithName("ListProjects").WithSummary("List all projects");
        group.MapPost("/", CreateProject).WithName("CreateProject").WithSummary("Create a project");
        group.MapGet("/{id:long}/tasks", ListTasks).WithName("ListTasks").WithSummary("List a project's tasks");
        group.MapPost("/{id:long}/tasks", CreateTask).WithName("CreateTask").WithSummary("Add a task to a project");
        return group;
    }

    public static async Task<Ok<List<Project>>> ListProjects(AppDbContext db) =>
        TypedResults.Ok(await db.Projects.OrderBy(p => p.Id).ToListAsync());

    public static async Task<Results<Created<Project>, ValidationProblem>> CreateProject(
        CreateProjectRequest req, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Name is required."]
            });

        var project = new Project { Name = req.Name.Trim() };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/projects/{project.Id}", project);
    }

    public static async Task<Results<Ok<List<TaskItem>>, NotFound>> ListTasks(long id, AppDbContext db)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == id))
            return TypedResults.NotFound();

        var tasks = await db.Tasks.Where(t => t.ProjectId == id).OrderBy(t => t.Id).ToListAsync();
        return TypedResults.Ok(tasks);
    }

    public static async Task<Results<Created<TaskItem>, NotFound, ValidationProblem>> CreateTask(
        long id, CreateTaskRequest req, AppDbContext db)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == id))
            return TypedResults.NotFound();

        if (string.IsNullOrWhiteSpace(req.Title))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["title"] = ["Title is required."]
            });

        var task = new TaskItem { ProjectId = id, Title = req.Title.Trim() };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/projects/{id}/tasks/{task.Id}", task);
    }
}
