using System.Globalization;
using Azure;
using Azure.Maps.Search;
using PolaScan.Api;
using PolaScan.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

var settings = builder.Configuration.GetSection("Settings").Get<Settings>();
var cognitiveService = new CognitiveService(settings);

var credential = new AzureKeyCredential(settings.AzureMapsSubscriptionKey ?? "");
var mapsClient = new MapsSearchClient(credential);

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

app.MapGet("/location-lookup", async (HttpRequest request) =>
{
    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    var lat = double.Parse(request.Query["lat"]);
    var lng = double.Parse(request.Query["lng"]);

    var locationRes = await mapsClient.ReverseSearchAddressAsync(new ReverseSearchOptions
    {
        Coordinates = new Azure.Core.GeoJson.GeoPosition(lng, lat)
    });
    if (locationRes?.Value?.Addresses?.FirstOrDefault()?.Address != null)
        return Results.Ok(locationRes.Value.Addresses.First().Address.FreeformAddress);

    return Results.NotFound();
});

app.Run();
