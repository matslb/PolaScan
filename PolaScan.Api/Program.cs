using Microsoft.AspNetCore.Mvc;
using PolaScan;
using PolaScan.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
builder.Services.AddApplicationInsightsTelemetry();

var settings = builder.Configuration.GetSection("Settings").Get<Settings>();
var cognitiveService = new CognitiveService(settings);
app.MapGet("/", async (HttpContext ctx) =>
{
    ctx.Response.Headers.ContentType = new Microsoft.Extensions.Primitives.StringValues("text/html; charset=UTF-8");
    await ctx.Response.SendFileAsync("index.html");
}
);

app.MapPost("/DetectPolaroidsInImage", async Task<IResult> (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var formFile = form.Files.First();

    if (formFile is null || formFile.Length == 0)
        return Results.BadRequest();

    await using var stream = formFile.OpenReadStream();

    var res = await cognitiveService.DetectPolaroidsInImage(stream);
    return Results.Ok(res);

});

app.MapPost("/DetectDateInImage", async Task<IResult> (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var formFile = form.Files.FirstOrDefault();

    if (formFile is null || formFile.Length == 0)
        return Results.BadRequest();

    await using var stream = formFile.OpenReadStream();

    var res = await cognitiveService.DetectDateInImage(stream, formFile.FileName);
    return Results.Ok(res);

});

app.Run();
