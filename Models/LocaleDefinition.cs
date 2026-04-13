namespace MusicStoreApp.Models;

public sealed class LocaleDefinition
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
    public required string BogusLocale { get; init; }
    public required string SingleLabel { get; init; }
    public required string[] Genres { get; init; }
    public required string[] TitleAdjectives { get; init; }
    public required string[] TitleNouns { get; init; }
    public required string[] TitleVerbs { get; init; }
    public required string[] TitlePatterns { get; init; }
    public required string[] AlbumDescriptors { get; init; }
    public required string[] BandDescriptors { get; init; }
    public required string[] BandNouns { get; init; }
    public required string[] ReviewOpenings { get; init; }
    public required string[] ReviewTextures { get; init; }
    public required string[] ReviewSentenceOneTemplates { get; init; }
    public required string[] ReviewSentenceTwoTemplates { get; init; }
    public required string[] ReviewClosings { get; init; }
    public required string[] LabelSuffixes { get; init; }
}
