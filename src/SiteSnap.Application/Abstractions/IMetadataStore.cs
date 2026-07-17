using SiteSnap.Domain;

namespace SiteSnap.Application.Abstractions;

public interface IMetadataStore
{
    AppState Load(string rootDir);
    void Save(string rootDir, AppState state);
    string MetadataPath(string rootDir);
}
