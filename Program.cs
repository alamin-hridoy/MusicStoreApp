using MusicStoreApp.Services;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<LocaleCatalog>();
builder.Services.AddSingleton<SongGenerator>();
builder.Services.AddSingleton<CoverArtService>();
builder.Services.AddSingleton<AudioPreviewService>();

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(port, out var parsedPort))
{
    app.Urls.Add($"http://0.0.0.0:{parsedPort}");
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = new StringValues("no-cache, no-store, must-revalidate");
        context.Context.Response.Headers.Pragma = new StringValues("no-cache");
        context.Context.Response.Headers.Expires = new StringValues("0");
    }
});

app.MapGet("/api/locales", (LocaleCatalog catalog) =>
{
    return Results.Ok(new
    {
        locales = catalog.GetAll()
            .Select(locale => new
            {
                code = locale.Code,
                name = locale.DisplayName
            })
    });
});

app.MapGet("/api/songs", (
    string? locale,
    string? seed,
    double? likes,
    int? page,
    int? pageSize,
    SongGenerator generator) =>
{
    var response = generator.GeneratePage(
        locale ?? "en-US",
        seed ?? "58933423",
        likes ?? 3.7,
        page ?? 1,
        pageSize ?? 10);

    return Results.Ok(response);
});

app.MapGet("/api/covers/{locale}/{seed}/{index:int}", (
    string locale,
    string seed,
    int index,
    CoverArtService coverArtService) =>
{
    var svg = coverArtService.Generate(locale, seed, index);
    return Results.Text(svg, "image/svg+xml");
});

app.MapGet("/api/audio/{locale}/{seed}/{index:int}", (
    string locale,
    string seed,
    int index,
    AudioPreviewService audioPreviewService) =>
{
    var audio = audioPreviewService.Generate(locale, seed, index);
    return Results.File(audio, "audio/wav");
});

app.MapFallbackToFile("index.html");

app.Run();
