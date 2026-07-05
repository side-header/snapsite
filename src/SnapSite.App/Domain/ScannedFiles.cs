namespace NewGreen.Domain;

public sealed record PhotoItem(string RelativePath, string Name);

public sealed record FolderItem(string RelativePath, string Name);

public sealed record OtherFileItem(string RelativePath, string Name);

public sealed class ScanResult
{
    public List<PhotoItem> Photos { get; init; } = [];
    public List<FolderItem> Folders { get; init; } = [];
    public List<OtherFileItem> OtherFiles { get; init; } = [];
}
