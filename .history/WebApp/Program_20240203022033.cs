using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());
var app = builder.Build();

// inbuilt middleware
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

// custom middleware
app.Use(async (context, next) =>{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow} ] Ended.");
});

var todos = new List<Todo>();

app.MapGet("/", () => "Good morning" );
app.MapGet("/todos", (ITaskService service) => service.GetTodos());
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service)=>{
var targetTodo = service.GetTodoById(id);
return targetTodo is null?TypedResults.NotFound():TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task, ITaskService service) =>{
    service.AddTodo(task);
    return TypedResults.Created("/todos/{id}", task);

})
.AddEndpointFilter(async (context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if (taskArgument.Schedule < DateTime.UtcNow)
    {
        errors.Add(nameof(Todo.Schedule), ["Cannot have due date in the past"]);
    }
    if (taskArgument.IsCompleted)
    {
        errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);
    }
    if (errors.Count >0){
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id, ITaskService service)=>{
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});





// Requires debugging, works as a Get request instead of Delete
// app.MapDelete("/todos", () => todos);

// app.MapDelete("/todos/", (Todo tasks)=>{
//     todos.Remove(tasks);
//     return TypedResults.NoContent();
// });

app.Run();

public record Todo( string Activity, DateTime Schedule, int Id, bool IsCompleted );

// creation of interface known as dependency injection
interface ITaskService
{
    Todo? GetTodoById(int id);

    List<Todo> GetTodos();

    void DeleteTodoById(int id);

    Todo AddTodo(Todo task);
}

// implementation of dependency injection

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(task => id == task.Id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t => id == t.Id);
    }

    public List<Todo> GetTodos()
    {
       return _todos;
    }
}
