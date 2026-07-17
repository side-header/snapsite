using System.Text.Json;
using System.Text.Encodings.Web;
using NewGreen.Domain;
using NewGreen.Infrastructure.FileSystem;

namespace NewGreen.Infrastructure.Persistence;

public sealed class MetadataStore
{
    public const string MetadataFileName = "sitesnape_manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public AppState Load(string rootDir)
    {
        var path = MetadataPath(rootDir);
        if (!File.Exists(path))
        {
            return new AppState { RootDir = rootDir };
        }

        var json = File.ReadAllText(path);
        var state = JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
        state.RootDir = rootDir;
        state.AppVersion = string.IsNullOrWhiteSpace(state.AppVersion) ? AppInfo.Version : state.AppVersion;
        state.NormalizePaperTemplates();
        state.ExportSettings ??= new ExportSettings();
        state.ExportSettings.Normalize();

        Sanitize(rootDir, state);

        return state;
    }

    public void Save(string rootDir, AppState state)
    {
        if (string.IsNullOrWhiteSpace(rootDir))
        {
            throw new InvalidOperationException("기준 폴더가 선택되지 않았습니다.");
        }

        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"기준 폴더를 찾을 수 없습니다: {rootDir}");
        }

        Sanitize(rootDir, state);
        state.RootDir = rootDir;
        state.AppVersion = AppInfo.Version;
        state.NormalizePaperTemplates();
        state.ExportSettings ??= new ExportSettings();
        state.ExportSettings.Normalize();
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var path = MetadataPath(rootDir);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json + Environment.NewLine);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        File.Move(tempPath, path);

        if (!File.Exists(path))
        {
            throw new IOException($"저장 후 메타데이터 파일을 확인할 수 없습니다: {path}");
        }
    }

    public static string MetadataPath(string rootDir)
    {
        return Path.Combine(rootDir, MetadataFileName);
    }

    private static void Sanitize(string rootDir, AppState state)
    {
        state.ExportSettings ??= new ExportSettings();
        state.ExportSettings.Normalize();
        state.NormalizePaperTemplates();

        foreach (var group in state.Groups)
        {
            group.MigrateLegacyCells();
            if (group.IsBlankPage)
            {
                group.Title = string.Empty;
                group.NormalizeCells();
                continue;
            }

            group.CntPerPage = NormalizeCntPerPage(group.CntPerPage);
            group.NormalizeCells();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SanitizeCells(rootDir, group.Target, seen);
            SanitizeCells(rootDir, group.Omit, seen);
        }
    }

    private static int NormalizeCntPerPage(int cntPerPage)
    {
        return cntPerPage == 4 ? 4 : 3;
    }

    private static void SanitizeCells(
        string rootDir,
        IEnumerable<PhotoCell> cells,
        HashSet<string> seen)
    {
        foreach (var cell in cells)
        {
            cell.Label = cell.Label?.Trim() ?? string.Empty;
            var path = AppState.NormalizePath(cell.Image ?? string.Empty);
            if (string.IsNullOrWhiteSpace(path))
            {
                cell.Image = string.Empty;
                continue;
            }

            cell.Image = FileScanner.IsIgnoredRelativePath(path) ||
                         !File.Exists(FileScanner.ToAbsolutePath(rootDir, path)) ||
                         !seen.Add(path)
                ? string.Empty
                : path;
        }
    }
}
