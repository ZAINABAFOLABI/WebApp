using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var todos = new List<Todo>();

app.MapGet("/", () => "Good morning" );
app.MapGet("/todos", () => todos);
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id)=>{
var targetTodo = todos.SingleOrDefault(t => id == t.Id);
return targetTodo is null?TypedResults.NotFound():TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task) =>{
    todos.Add(task);
    return TypedResults.Created("/todos/{id}", task);

});

app.MapDelete("/todos/{id}", (int id)=>{
    todos.RemoveAll(t => id == t.Id);
    return TypedResults.NoContent();
});

// inbuilt middleware
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

// custom middleware
app.Use(async (context, next) =>{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow} ] Ended.");
});



// Requires debugging, works as a Get request instead of Delete
// app.MapDelete("/todos", () => todos);

// app.MapDelete("/todos/", (Todo tasks)=>{
//     todos.Remove(tasks);
//     return TypedResults.NoContent();
// });

app.Run();

public record Todo( string Activity, DateTime Schedule, int Id, bool IsCompleted );
