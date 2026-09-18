namespace Portfolio.Content;

/// <summary>A portfolio project. Source: <c>content/projects/</c>.</summary>
public sealed class Project : ContentEntry
{
    /// <summary>Promotes the project into the homepage selection.</summary>
    public bool Featured { get; init; }

    /// <summary>Role performed, e.g. "Sole engineer".</summary>
    public string? Role { get; init; }

    /// <summary>Free-form period label, e.g. "2023" or "2022–2023".</summary>
    public string? Period { get; init; }

    /// <summary>Optional decorative artwork: network, stack, or signal.</summary>
    public string? Visual { get; init; }

    public string? Category { get; init; }
}

/// <summary>A professional certification. Source: <c>content/certifications/</c>.</summary>
public sealed class Certification : ContentEntry
{
    public bool Featured { get; init; }

    /// <summary>Awarding body, e.g. "Cloud Native Computing Foundation".</summary>
    public string Issuer { get; init; } = "";

    /// <summary>Short form shown as a badge, e.g. "CKA".</summary>
    public string? Abbreviation { get; init; }
}

/// <summary>An item on the "now" page. Source: <c>content/now/</c>.</summary>
public sealed class NowItem : ContentEntry
{
    /// <summary>Groups items under a heading, e.g. "Currently Working On".</summary>
    public string Group { get; init; } = "Currently Working On";

    /// <summary>Progress label, e.g. "In progress".</summary>
    public string? Status { get; init; }
}

/// <summary>
/// A named cluster of technologies, e.g. "Cloud &amp; Platform". One file per
/// cluster; adding a technology is a one-line edit to <c>items</c>.
/// Source: <c>content/skills/</c>.
/// </summary>
public sealed class SkillGroup : ContentEntry
{
    public string[] Items { get; init; } = [];
}

/// <summary>A personal interest. Source: <c>content/interests/</c>.</summary>
public sealed class Interest : ContentEntry
{
}

/// <summary>A short "did you know" line. Source: <c>content/facts/</c>.</summary>
public sealed class Fact : ContentEntry
{
}

public sealed class RepositoryEntry : ContentEntry
{
    public string? Language { get; init; }

    public bool Fork { get; init; }
}

public sealed class Track : ContentEntry
{
    public string Artist { get; init; } = "";
}

public sealed class ActivitySnapshot : ContentEntry
{
    public DateTime Start { get; init; }

    public DateTime End { get; init; }

    public ContributionDay[] Days { get; init; } = [];
}

public sealed class ContributionDay
{
    public DateTime Date { get; init; }

    public int Count { get; init; }
}
