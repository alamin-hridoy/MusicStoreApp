# Music Store Showcase

Seeded music catalog generator built with ASP.NET Core 8 and a static frontend.

The catalog is deterministic by `locale + seed + page/index`, so new songs can be generated indefinitely without a hard record cap. Album covers are also generated per song with multiple SVG layouts.

## Run locally

```bash
dotnet restore
dotnet run
```
