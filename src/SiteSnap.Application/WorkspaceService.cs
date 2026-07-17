using SiteSnap.Application.Abstractions;
using SiteSnap.Domain;

namespace SiteSnap.Application;

public sealed class WorkspaceService(IFileScanner scanner, IMetadataStore metadataStore)
{
    public WorkspaceSnapshot Open(string rootDir)
    {
        return new WorkspaceSnapshot(metadataStore.Load(rootDir), scanner.Scan(rootDir));
    }

    public ScanResult Scan(string rootDir) => scanner.Scan(rootDir);

    public void Save(string rootDir, AppState state) => metadataStore.Save(rootDir, state);

    public string MetadataPath(string rootDir) => metadataStore.MetadataPath(rootDir);
}

public sealed record WorkspaceSnapshot(AppState State, ScanResult ScanResult);
