using MusicStoreApp.Utilities;

namespace MusicStoreApp.Services;

public sealed class CoverArtService
{
    private readonly string _coversDirectory;
    private readonly string[] _coverFiles;

    public CoverArtService(IWebHostEnvironment environment)
    {
        _coversDirectory = Path.Combine(environment.WebRootPath, "images", "generated-covers");
        _coverFiles = Directory.Exists(_coversDirectory)
            ? Directory.GetFiles(_coversDirectory, "*.png")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : Array.Empty<string>();
    }

    public byte[] Generate(string locale, string seed, int index)
    {
        if (_coverFiles.Length == 0)
        {
            throw new InvalidOperationException($"No generated covers found in '{_coversDirectory}'.");
        }

        var normalizedIndex = Math.Max(1, index);
        var hash = StableRandom.Compose(StableRandom.ComposeString(locale, seed), (ulong)normalizedIndex, 0xC0FEBABEUL);
        var selected = _coverFiles[(int)(hash % (ulong)_coverFiles.Length)];
        return File.ReadAllBytes(selected);
    }
}
