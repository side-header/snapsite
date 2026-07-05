using System.Text.Json;
using NewGreen.Domain;
using NewGreen.Infrastructure.FileSystem;

namespace NewGreen.Infrastructure.Persistence;

public sealed class MetadataStore
{
    public const string MetadataFileName = "sitesnape_manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
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
            group.CntPerPage = NormalizeCntPerPage(group.CntPerPage);
            (group.Before, group.BeforeLabels) = CleanPathsWithLabels(rootDir, group.Before, group.BeforeLabels, Phase.Before);
            (group.Processing, group.ProcessingLabels) = CleanPathsWithLabels(rootDir, group.Processing, group.ProcessingLabels, Phase.Processing);
            (group.After, group.AfterLabels) = CleanPathsWithLabels(rootDir, group.After, group.AfterLabels, Phase.After);
            group.NormalizeLabels();
        }
    }

    private static int NormalizeCntPerPage(int cntPerPage)
    {
        return cntPerPage == 4 ? 4 : 3;
    }

    private static (List<string> Paths, List<string> Labels) CleanPathsWithLabels(
        string rootDir,
        IReadOnlyList<string> paths,
        IReadOnlyList<string> labels,
        Phase phase)
    {
        var cleanPaths = new List<string>();
        var cleanLabels = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < paths.Count; i++)
        {
            var path = AppState.NormalizePath(paths[i]);
            if (FileScanner.IsIgnoredRelativePath(path) || !File.Exists(FileScanner.ToAbsolutePath(rootDir, path)) || !seen.Add(path))
            {
                continue;
            }

            cleanPaths.Add(path);
            cleanLabels.Add(i < labels.Count ? labels[i].Trim() : (cleanPaths.Count == 1 ? phase.Label() : string.Empty));
        }

        if (cleanLabels.Count == 0)
        {
            cleanLabels.Add(labels.Count > 0 ? labels[0].Trim() : phase.Label());
        }

        return (cleanPaths, cleanLabels);
    }
}
