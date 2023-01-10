var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseProxy(httpContext =>
{
    // Set the target URL for the downstream server
    httpContext.TargetUrl = "http://localhost:5001";
});


app.MapGet("/", () => "Hello World!");

app.Run();
