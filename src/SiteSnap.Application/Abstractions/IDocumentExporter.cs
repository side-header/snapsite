using SiteSnap.Domain;

namespace SiteSnap.Application.Abstractions;

public interface IDocumentExporter
{
    void ExportAll(string rootDir, AppState state);
    string ExportDocxOnly(string rootDir, AppState state);
    void ExportDocxTo(string rootDir, string outputPath, AppState state);
    string ExportHwpxOnly(string rootDir, AppState state);
    void ExportHwpxTo(string rootDir, string outputPath, AppState state);
}
