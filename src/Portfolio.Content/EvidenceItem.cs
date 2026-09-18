namespace Portfolio.Content;

public enum EvidenceKind { Project, Credential, Repository, Now }

public sealed record EvidenceItem(ContentEntry Entry, EvidenceKind Kind, string[] Topics, string? Url)
{
    public string Key => $"{Kind.ToString().ToLowerInvariant()}-{Entry.Slug}";

    public string? Detail => Entry switch
    {
        Certification certification => certification.Issuer,
        RepositoryEntry repository => repository.Language,
        NowItem item => item.Status,
        Project project => project.Category,
        _ => null,
    };
}

public sealed class ExplorerCopy
{
    public string SearchLabel { get; init; } = "";
    public string SearchPlaceholder { get; init; } = "";
    public string TopicLabel { get; init; } = "";
    public string AllTopicsLabel { get; init; } = "";
    public string KindLabel { get; init; } = "";
    public string ResetLabel { get; init; } = "";
    public string ShareLabel { get; init; } = "";
    public string ShareUrlLabel { get; init; } = "";
    public string CopiedMessage { get; init; } = "";
    public string CopyFailedMessage { get; init; } = "";
    public string ResultsTemplate { get; init; } = "";
    public string EmptyMessage { get; init; } = "";
    public string FallbackMessage { get; init; } = "";
    public string ReadyMessage { get; init; } = "";
    public string InvalidFilterMessage { get; init; } = "";
    public string HistoryFailedMessage { get; init; } = "";
    public string OpenLabel { get; init; } = "";
    public string ProjectLabel { get; init; } = "";
    public string CredentialLabel { get; init; } = "";
    public string RepositoryLabel { get; init; } = "";
    public string NowLabel { get; init; } = "";

    public string LabelFor(EvidenceKind kind) => kind switch
    {
        EvidenceKind.Project => ProjectLabel,
        EvidenceKind.Credential => CredentialLabel,
        EvidenceKind.Repository => RepositoryLabel,
        EvidenceKind.Now => NowLabel,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
