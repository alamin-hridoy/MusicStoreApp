namespace MusicStoreApp.Models;

public sealed class SongPageResponse
{
    public required string Locale { get; init; }
    public required string Seed { get; init; }
    public required double LikesAverage { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int? TotalRecords { get; init; }
    public int? TotalPages { get; init; }
    public required bool HasMore { get; init; }
    public required IReadOnlyList<SongRecord> Records { get; init; }
}

public sealed class SongRecord
{
    public required int Index { get; init; }
    public required string Title { get; init; }
    public required string Artist { get; init; }
    public required string Album { get; init; }
    public required string Genre { get; init; }
    public required int Likes { get; init; }
    public required string Review { get; init; }
    public required string Label { get; init; }
    public required int ReleaseYear { get; init; }
    public required int DurationSeconds { get; init; }
    public required string CoverUrl { get; init; }
    public required string AudioUrl { get; init; }
}
