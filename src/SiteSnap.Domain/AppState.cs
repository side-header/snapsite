using System.Text.Json.Serialization;

namespace SiteSnap.Domain;

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
        return InsertGroup(Groups.Count);
    }

    public PhotoGroup InsertGroup(int index)
    {
        var number = NextGroupNumber();
        var group = new PhotoGroup
        {
            Id = $"group-{number}",
            Title = string.Empty,
            Omit = [],
            CntPerPage = 3
        };
        Groups.Insert(Math.Clamp(index, 0, Groups.Count), group);
        return group;
    }

    public PhotoGroup AddBlankPage()
    {
        return InsertBlankPage(Groups.Count);
    }

    public PhotoGroup InsertBlankPage(int index)
    {
        var blankPage = new PhotoGroup
        {
            Id = $"blank-page-{Guid.NewGuid():N}",
            Target = [],
            Omit = []
        };
        Groups.Insert(Math.Clamp(index, 0, Groups.Count), blankPage);
        return blankPage;
    }

    public void RemoveGroup(string id)
    {
        Groups.RemoveAll(group => group.Id == id);
    }

    public PhotoGroup? GroupById(string id)
    {
        return Groups.FirstOrDefault(group => group.Id == id);
    }

    public bool RemoveCell(string groupId, bool omit, int cellIndex, out string removedPhoto)
    {
        removedPhoto = string.Empty;
        var group = GroupById(groupId);
        if (group is null || group.IsBlankPage)
        {
            return false;
        }

        group.NormalizeCells();
        var cells = omit ? group.Omit : group.Target;
        if (cellIndex < 0 || cellIndex >= cells.Count)
        {
            return false;
        }

        removedPhoto = cells[cellIndex].Image;
        cells.RemoveAt(cellIndex);
        return true;
    }

    public bool InsertEmptyCell(string groupId, bool omit, int insertIndex)
    {
        var group = GroupById(groupId);
        if (group is null || group.IsBlankPage)
        {
            return false;
        }

        group.NormalizeCells();
        var cells = omit ? group.Omit : group.Target;
        cells.Insert(Math.Clamp(insertIndex, 0, cells.Count), new PhotoCell());
        return true;
    }

    public bool PlacePhotoAt(string groupId, bool omit, int cellIndex, string relativePath, out bool filledExistingCell)
    {
        filledExistingCell = false;
        relativePath = NormalizePath(relativePath);
        var group = GroupById(groupId);
        if (group is null || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        group.NormalizeCells();
        var cells = omit ? group.Omit : group.Target;
        if (cells.Count == 0 && cellIndex < 0)
        {
            ClearPhotoFromAllGroups(relativePath);
            cells.Add(new PhotoCell { Image = relativePath });
            return true;
        }

        if (cellIndex < 0 || cellIndex >= cells.Count)
        {
            return false;
        }

        var targetCell = cells[cellIndex];
        var reuseTargetCell = string.IsNullOrWhiteSpace(targetCell.Image);
        ClearPhotoFromAllGroups(relativePath);
        if (reuseTargetCell)
        {
            targetCell.Image = relativePath;
            filledExistingCell = true;
            return true;
        }

        var insertIndex = Math.Clamp(cellIndex + 1, 0, cells.Count);
        cells.Insert(insertIndex, new PhotoCell { Image = relativePath });
        return true;
    }

    public bool MoveAssignedPhotoCellToInsertionIndex(
        string targetGroupId,
        bool targetOmit,
        int targetInsertionIndex,
        string sourceGroupId,
        bool sourceOmit,
        int sourceCellIndex)
    {
        var targetGroup = GroupById(targetGroupId);
        var sourceGroup = GroupById(sourceGroupId);
        if (targetGroup is null || sourceGroup is null)
        {
            return false;
        }

        targetGroup.NormalizeCells();
        sourceGroup.NormalizeCells();
        var targetCells = targetOmit ? targetGroup.Omit : targetGroup.Target;
        var sourceCells = sourceOmit ? sourceGroup.Omit : sourceGroup.Target;
        if (sourceCellIndex < 0 || sourceCellIndex >= sourceCells.Count)
        {
            return false;
        }

        if (targetInsertionIndex < 0 || targetInsertionIndex > targetCells.Count)
        {
            return false;
        }

        var sourceCell = sourceCells[sourceCellIndex];
        var adjustedInsertionIndex = targetInsertionIndex;
        if (ReferenceEquals(sourceCells, targetCells) && sourceCellIndex < adjustedInsertionIndex)
        {
            adjustedInsertionIndex--;
        }

        if (ReferenceEquals(sourceCells, targetCells) && adjustedInsertionIndex == sourceCellIndex)
        {
            return false;
        }

        sourceCells.RemoveAt(sourceCellIndex);
        targetCells.Insert(adjustedInsertionIndex, sourceCell);
        return true;
    }

    public int PlacePhotosBesideCell(
        string groupId,
        bool omit,
        int cellIndex,
        IReadOnlyList<string> relativePaths)
    {
        var group = GroupById(groupId);
        if (group is null || relativePaths.Count == 0)
        {
            return 0;
        }

        group.NormalizeCells();
        var cells = omit ? group.Omit : group.Target;
        if (cellIndex < 0 || cellIndex >= cells.Count)
        {
            return 0;
        }

        var normalizedPaths = NormalizeUniquePaths(relativePaths);
        if (normalizedPaths.Count == 0)
        {
            return 0;
        }

        var targetCell = cells[cellIndex];
        var reuseTargetCell = string.IsNullOrWhiteSpace(targetCell.Image);
        var reusableCells = reuseTargetCell
            ? cells.Skip(cellIndex).Where(cell => string.IsNullOrWhiteSpace(cell.Image)).ToList()
            : [];
        var reusableOmitCells = reuseTargetCell && !omit
            ? group.Omit.Where(cell => string.IsNullOrWhiteSpace(cell.Image)).ToList()
            : [];
        foreach (var relativePath in normalizedPaths)
        {
            ClearPhotoFromAllGroups(relativePath);
        }

        if (!reuseTargetCell)
        {
            var insertIndex = cells.IndexOf(targetCell) + 1;
            foreach (var relativePath in normalizedPaths)
            {
                cells.Insert(insertIndex++, new PhotoCell { Image = relativePath });
            }

            return normalizedPaths.Count;
        }

        var pathIndex = 0;
        foreach (var cell in reusableCells)
        {
            if (pathIndex >= normalizedPaths.Count)
            {
                break;
            }

            cell.Image = normalizedPaths[pathIndex++];
        }

        foreach (var cell in reusableOmitCells)
        {
            if (pathIndex >= normalizedPaths.Count)
            {
                break;
            }

            cell.Image = normalizedPaths[pathIndex++];
        }

        var overflowCells = omit ? cells : group.Omit;
        while (pathIndex < normalizedPaths.Count)
        {
            overflowCells.Add(new PhotoCell { Image = normalizedPaths[pathIndex++] });
        }

        return normalizedPaths.Count;
    }

    public int PlacePhotosInCollection(
        string groupId,
        bool omit,
        IReadOnlyList<string> relativePaths)
    {
        var group = GroupById(groupId);
        if (group is null || relativePaths.Count == 0)
        {
            return 0;
        }

        group.NormalizeCells();
        var cells = omit ? group.Omit : group.Target;
        var normalizedPaths = NormalizeUniquePaths(relativePaths);

        foreach (var relativePath in normalizedPaths)
        {
            ClearPhotoFromAllGroups(relativePath);
        }

        var pathIndex = 0;
        var reusableCells = cells.Where(cell => string.IsNullOrWhiteSpace(cell.Image)).ToList();
        foreach (var cell in reusableCells)
        {
            if (pathIndex >= normalizedPaths.Count)
            {
                break;
            }

            cell.Image = normalizedPaths[pathIndex++];
        }

        while (pathIndex < normalizedPaths.Count)
        {
            cells.Add(new PhotoCell { Image = normalizedPaths[pathIndex++] });
        }

        return normalizedPaths.Count;
    }

    public void RemovePhoto(string relativePath)
    {
        relativePath = NormalizePath(relativePath);
        foreach (var group in Groups)
        {
            group.RemovePhotoCell(relativePath);
        }
    }

    public HashSet<string> AssignedSet()
    {
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in Groups)
        {
            if (group.IsBlankPage)
            {
                continue;
            }

            foreach (var cell in group.AllCells())
            {
                if (!string.IsNullOrWhiteSpace(cell.Image))
                {
                    assigned.Add(NormalizePath(cell.Image));
                }
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
            foreach (var cell in group.AllCells())
            {
                cell.Image = ReplacePath(cell.Image, oldPath, newPath, includeChildren);
            }
        }
    }

    private void ClearPhotoFromAllGroups(string relativePath)
    {
        foreach (var group in Groups)
        {
            group.ClearPhoto(relativePath);
        }
    }

    private static List<string> NormalizeUniquePaths(IEnumerable<string> relativePaths)
    {
        var normalizedPaths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in relativePaths)
        {
            var normalized = NormalizePath(relativePath);
            if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
            {
                normalizedPaths.Add(normalized);
            }
        }

        return normalizedPaths;
    }

    private static string ReplacePath(string path, string oldPath, string newPath, bool includeChildren)
    {
        path = NormalizePath(path);
        if (SamePath(path, oldPath))
        {
            return newPath;
        }

        var oldPrefix = oldPath + "/";
        return includeChildren && path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase)
            ? newPath + path[oldPath.Length..]
            : path;
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

    public static bool SamePath(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizePath(string path)
    {
        return (path ?? string.Empty).Replace('\\', '/');
    }
}
