namespace Dashboard.Models.ViewModels;

public sealed class GlobalSearchHitDto
{
    public string Section { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Url { get; set; } = "";
}

public sealed class GlobalSearchResponseDto
{
    public IReadOnlyList<GlobalSearchHitDto> Items { get; set; } = Array.Empty<GlobalSearchHitDto>();
    public bool Truncated { get; set; }
}
