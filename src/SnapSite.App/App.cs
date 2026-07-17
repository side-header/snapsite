using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using SiteSnap.Application;
using SiteSnap.Infrastructure.Export;
using SiteSnap.Infrastructure.FileSystem;
using SiteSnap.Infrastructure.Persistence;
using SiteSnap.Presentation;

namespace SiteSnap.Presentation;

public sealed class App : Avalonia.Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var workspaceService = new WorkspaceService(new FileScanner(), new MetadataStore());
            var exportService = new DocumentExportService(new DocumentExporter());
            desktop.MainWindow = new MainWindow(workspaceService, exportService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
