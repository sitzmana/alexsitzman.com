namespace Portfolio.Components;

/// <summary>
/// Cascaded to each section so it can render its position without the page
/// having to thread the number through every component.
/// </summary>
/// <param name="Index">1-based position within the page.</param>
public sealed record SectionContext(int Index);
