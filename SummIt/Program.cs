using SummIt;
using SummIt.DB;
using SummIt.Handlers;

var app = WebApplication.CreateBuilder(args)
    .RegisterServices()
    .Build();
app.MapSpaceWebHookHandler<WebhookHandler>("/api/space");
app.MapGet("/", () => "summIt app is running 😋");

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SummItAppContext>();
    await context.InitializeAsync(app.Configuration);
}

app.Run();
