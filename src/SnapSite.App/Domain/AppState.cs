using System.Text.Json.Serialization;

namespace NewGreen.Domain;

public sealed class AppState
{
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = AppInfo.Version;

    [JsonPropertyName("rootDir")]
    public string RootDir { get; set; } = string.Empty;

    [JsonPropertyName("groups")]
    public List<PhotoGroup> Groups { get; set; } = [];

    [JsonPropertyName("exportSettings")]
    public ExportSettings ExportSettings { get; set; } = new();

    [JsonPropertyName("paperTemplate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaperTemplateSettings? PaperTemplate { get; set; }

    [JsonPropertyName("paperTemplates")]
    public PaperTemplateFormatSettings? PaperTemplates { get; set; }

    public void NormalizePaperTemplates()
    {
        if (PaperTemplates is null && PaperTemplate is not null)
        {
            PaperTemplate.Normalize();
            PaperTemplates = new PaperTemplateFormatSettings
            {
                Hwpx = PaperTemplate.Clone(),
                Docx = PaperTemplate.Clone()
            };
        }

        PaperTemplates ??= new PaperTemplateFormatSettings();
        PaperTemplates.Normalize();
        PaperTemplate = null;
    }

    public PhotoGroup AddGroup()
    {
        var number = NextGroupNumber();
        var group = new PhotoGroup
        {
            Id = $"group-{number}",
            Title = string.Empty,
            CntPerPage = 3,
            BeforeLabels = [Phase.Before.Label()],
            ProcessingLabels = [Phase.Processing.Label()],
            AfterLabels = [Phase.After.Label()]
        };
        Groups.Add(group);
        return group;
    }

    public void RemoveGroup(string id)
    {
        Groups.RemoveAll(group => group.Id == id);
    }

    public PhotoGroup? GroupById(string id)
    {
        return Groups.FirstOrDefault(group => group.Id == id);
    }

    public void AssignPhoto(string groupId, Phase phase, string relativePath)
    {
        relativePath = NormalizePath(relativePath);
        var movedLabel = RemovePhotoWithLabel(relativePath);

        var group = GroupById(groupId);
        if (group is null)
        {
            return;
        }

        group.InsertPhoto(phase, relativePath, group.Photos(phase).Count, movedLabel);
    }

    public void RemovePhoto(string relativePath)
    {
        RemovePhotoWithLabel(relativePath);
    }

    public string? RemovePhotoWithLabel(string relativePath)
    {
        relativePath = NormalizePath(relativePath);
        foreach (var group in Groups)
        {
            var removedLabel = group.RemovePhotoWithLabel(relativePath);
            if (removedLabel is not null)
            {
                return removedLabel;
            }
        }

        return null;
    }

    public HashSet<string> AssignedSet()
    {
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in Groups)
        {
            foreach (var path in group.Before.Concat(group.Processing).Concat(group.After))
            {
                assigned.Add(NormalizePath(path));
            }
        }

        return assigned;
    }

    public void ReplaceAssignedPath(string oldPath, string newPath, bool includeChildren)
    {
        oldPath = NormalizePath(oldPath).TrimEnd('/');
        newPath = NormalizePath(newPath).TrimEnd('/');
        foreach (var group in Groups)
        {
            ReplacePaths(group.Before, oldPath, newPath, includeChildren);
            ReplacePaths(group.Processing, oldPath, newPath, includeChildren);
            ReplacePaths(group.After, oldPath, newPath, includeChildren);
        }
    }

    private static void ReplacePaths(List<string> paths, string oldPath, string newPath, bool includeChildren)
    {
        var oldPrefix = oldPath + "/";
        for (var i = 0; i < paths.Count; i++)
        {
            var path = NormalizePath(paths[i]);
            if (SamePath(path, oldPath))
            {
                paths[i] = newPath;
                continue;
            }

            if (includeChildren && path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
            {
                paths[i] = newPath + path[oldPath.Length..];
            }
        }
    }

    private int NextGroupNumber()
    {
        var max = 0;
        foreach (var group in Groups)
        {
            if (group.Id.StartsWith("group-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(group.Id["group-".Length..], out var number))
            {
                max = Math.Max(max, number);
            }
        }

        return max + 1;
    }

    internal static bool SamePath(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
