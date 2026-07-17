using SiteSnap.Application.Abstractions;
using SiteSnap.Domain;

namespace SiteSnap.Application;

public sealed class DocumentExportService(IDocumentExporter exporter)
{
    public void ExportAll(string rootDir, AppState state) => exporter.ExportAll(rootDir, state);
    public string ExportDocxOnly(string rootDir, AppState state) => exporter.ExportDocxOnly(rootDir, state);
    public void ExportDocxTo(string rootDir, string outputPath, AppState state) => exporter.ExportDocxTo(rootDir, outputPath, state);
    public string ExportHwpxOnly(string rootDir, AppState state) => exporter.ExportHwpxOnly(rootDir, state);
    public void ExportHwpxTo(string rootDir, string outputPath, AppState state) => exporter.ExportHwpxTo(rootDir, outputPath, state);
}
