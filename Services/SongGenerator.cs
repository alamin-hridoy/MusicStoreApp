using Bogus;
using MusicStoreApp.Models;
using MusicStoreApp.Utilities;

namespace MusicStoreApp.Services;

public sealed class SongGenerator
{
    private readonly LocaleCatalog _localeCatalog;

    public SongGenerator(LocaleCatalog localeCatalog)
    {
        _localeCatalog = localeCatalog;
    }

    public SongPageResponse GeneratePage(string localeCode, string seedInput, double likesAverage, int page, int pageSize)
    {
        var locale = _localeCatalog.Get(localeCode);
        var normalizedLikes = Math.Clamp(likesAverage, 0, 10);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 24);
        var normalizedPage = Math.Max(page, 1);
        var seedValue = ParseSeed(seedInput);

        var startIndex = (normalizedPage - 1) * normalizedPageSize + 1;
        var endIndex = startIndex + normalizedPageSize - 1;

        var records = Enumerable.Range(startIndex, endIndex - startIndex + 1)
            .Select(index => GenerateRecord(locale, seedValue, seedInput, normalizedLikes, index))
            .ToArray();

        return new SongPageResponse
        {
            Locale = locale.Code,
            Seed = seedInput,
            LikesAverage = normalizedLikes,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            HasMore = true,
            Records = records
        };
    }

    public SongRecord GenerateRecord(string localeCode, string seedInput, double likesAverage, int index)
    {
        var locale = _localeCatalog.Get(localeCode);
        var normalizedLikes = Math.Clamp(likesAverage, 0, 10);
        var normalizedIndex = Math.Max(1, index);
        var seedValue = ParseSeed(seedInput);

        return GenerateRecord(locale, seedValue, seedInput, normalizedLikes, normalizedIndex);
    }

    private SongRecord GenerateRecord(LocaleDefinition locale, ulong seedValue, string seedInput, double likesAverage, int index)
    {
        var localeSeed = StableRandom.ComposeString(locale.Code);
        var coreSeed = StableRandom.Compose(seedValue, localeSeed, (ulong)index);
        var contentRandom = new StableRandom(coreSeed);
        var faker = new Faker(locale.BogusLocale);
        faker.Random = new Randomizer(unchecked((int)(coreSeed & 0x7FFFFFFF)));

        var title = BuildTitle(locale, contentRandom);
        var artist = BuildArtist(locale, contentRandom, faker);
        var album = contentRandom.NextBool(0.24) ? locale.SingleLabel : BuildAlbum(locale, contentRandom);
        var genre = contentRandom.Pick(locale.Genres);
        var releaseYear = contentRandom.Next(1998, 2026);
        var durationSeconds = contentRandom.Next(10, 19);
        var labelSuffix = contentRandom.Pick(locale.LabelSuffixes);
        var label = $"{faker.Company.CompanyName()} {labelSuffix}";
        var review = BuildReview(locale, contentRandom, genre, album);
        var likes = BuildLikes(seedValue, index, likesAverage);

        return new SongRecord
        {
            Index = index,
            Title = title,
            Artist = artist,
            Album = album,
            Genre = genre,
            Likes = likes,
            Review = review,
            Label = label,
            ReleaseYear = releaseYear,
            DurationSeconds = durationSeconds,
            CoverUrl = $"/api/covers/{Uri.EscapeDataString(locale.Code)}/{Uri.EscapeDataString(seedInput)}/{index}",
            AudioUrl = $"/api/audio/{Uri.EscapeDataString(locale.Code)}/{Uri.EscapeDataString(seedInput)}/{index}"
        };
    }

    private static ulong ParseSeed(string seedInput)
    {
        return ulong.TryParse(seedInput, out var parsed)
            ? parsed
            : StableRandom.ComposeString(seedInput);
    }

    private static string BuildTitle(LocaleDefinition locale, StableRandom random)
    {
        var pattern = random.Pick(locale.TitlePatterns);
        return pattern
            .Replace("{adjective}", random.Pick(locale.TitleAdjectives))
            .Replace("{noun}", random.Pick(locale.TitleNouns))
            .Replace("{verb}", random.Pick(locale.TitleVerbs));
    }

    private static string BuildAlbum(LocaleDefinition locale, StableRandom random)
    {
        return $"{random.Pick(locale.AlbumDescriptors)} {random.Pick(locale.TitleNouns)}";
    }

    private static string BuildArtist(LocaleDefinition locale, StableRandom random, Faker faker)
    {
        return random.NextBool(0.56)
            ? faker.Name.FullName()
            : $"{random.Pick(locale.BandDescriptors)} {random.Pick(locale.BandNouns)}";
    }

    private static string BuildReview(LocaleDefinition locale, StableRandom random, string genre, string album)
    {
        var opening = random.Pick(locale.ReviewOpenings);
        var textureA = random.Pick(locale.ReviewTextures);
        var textureB = random.Pick(locale.ReviewTextures);
        var sentenceOne = random.Pick(locale.ReviewSentenceOneTemplates);
        var sentenceTwo = random.Pick(locale.ReviewSentenceTwoTemplates);
        var closing = random.Pick(locale.ReviewClosings);

        return sentenceOne
                   .Replace("{opening}", opening)
                   .Replace("{genre}", genre)
                   .Replace("{texture}", textureA) +
               " " +
               sentenceTwo
                   .Replace("{album}", album)
                   .Replace("{texture}", textureB) +
               " " +
               $"{closing}";
    }

    private static int BuildLikes(ulong seedValue, int index, double likesAverage)
    {
        var clamped = Math.Clamp(likesAverage, 0, 10);
        var whole = (int)Math.Floor(clamped);
        var fractional = clamped - whole;
        var random = new StableRandom(StableRandom.Compose(seedValue, (ulong)index, 0xA11CE5UL));
        return whole + (random.NextDouble() < fractional ? 1 : 0);
    }
}
