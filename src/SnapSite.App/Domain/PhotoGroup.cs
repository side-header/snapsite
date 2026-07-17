using System.Text.Json.Serialization;

namespace NewGreen.Domain;

public sealed class PhotoGroup
{
    public const int MinCntPerPage = 1;
    public const int MaxCntPerPage = 10;
    public const int DefaultCntPerPage = 3;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isBlankPage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyIsBlankPage { get; set; }

    [JsonPropertyName("target")]
    public List<PhotoCell> Target { get; set; } = DefaultTargetCells();

    [JsonPropertyName("omit")]
    public List<PhotoCell> Omit { get; set; } = DefaultOmitCells();

    [JsonPropertyName("cntPerPage")]
    public int CntPerPage { get; set; } = DefaultCntPerPage;

    [JsonIgnore]
    public bool IsBlankPage =>
        string.IsNullOrWhiteSpace(Title) &&
        Target is { Count: 0 } &&
        Omit is { Count: 0 };

    [JsonPropertyName("before")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyBefore { get; set; }

    [JsonPropertyName("beforeLabels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyBeforeLabels { get; set; }

    [JsonPropertyName("processing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyProcessing { get; set; }

    [JsonPropertyName("processingLabels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyProcessingLabels { get; set; }

    [JsonPropertyName("after")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyAfter { get; set; }

    [JsonPropertyName("afterLabels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyAfterLabels { get; set; }

    [JsonPropertyName("other")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyOther { get; set; }

    [JsonPropertyName("otherLabels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? LegacyOtherLabels { get; set; }

    public IEnumerable<PhotoCell> AllCells()
    {
        return Target.Concat(Omit);
    }

    public static int NormalizeCntPerPage(int count)
    {
        return Math.Clamp(count, MinCntPerPage, MaxCntPerPage);
    }

    public bool HasAnyPhoto()
    {
        return AllCells().Any(cell => !string.IsNullOrWhiteSpace(cell.Image));
    }

    public bool ClearPhoto(string relativePath)
    {
        var changed = false;
        foreach (var cell in AllCells())
        {
            if (!string.IsNullOrWhiteSpace(cell.Image) && AppState.SamePath(cell.Image, relativePath))
            {
                cell.Image = string.Empty;
                changed = true;
            }
        }

        return changed;
    }

    public bool RemovePhotoCell(string relativePath)
    {
        var removedTarget = Target.RemoveAll(cell =>
            !string.IsNullOrWhiteSpace(cell.Image) && AppState.SamePath(cell.Image, relativePath));
        var removedOmit = Omit.RemoveAll(cell =>
            !string.IsNullOrWhiteSpace(cell.Image) && AppState.SamePath(cell.Image, relativePath));
        return removedTarget + removedOmit > 0;
    }

    public void NormalizeCells()
    {
        Target ??= [];
        Omit ??= [];

        if (LegacyIsBlankPage == true)
        {
            Title = string.Empty;
            Target.Clear();
            Omit.Clear();
            LegacyIsBlankPage = null;
            ClearLegacyFields();
            return;
        }

        LegacyIsBlankPage = null;

        foreach (var cell in AllCells())
        {
            cell.Image = AppState.NormalizePath(cell.Image ?? string.Empty);
            cell.Label = cell.Label?.Trim() ?? string.Empty;
        }
    }

    public void MigrateLegacyCells()
    {
        if (LegacyIsBlankPage == true)
        {
            NormalizeCells();
            return;
        }

        if (!HasLegacyFields())
        {
            NormalizeCells();
            return;
        }

        Target = [];
        AppendLegacyPhase(Target, LegacyBefore, LegacyBeforeLabels, "전");
        AppendLegacyPhase(Target, LegacyProcessing, LegacyProcessingLabels, "중");
        AppendLegacyPhase(Target, LegacyAfter, LegacyAfterLabels, "후");

        Omit = [];
        AppendLegacyPhase(Omit, LegacyOther, LegacyOtherLabels, string.Empty);

        ClearLegacyFields();
        NormalizeCells();
    }

    public static List<PhotoCell> DefaultTargetCells()
    {
        return
        [
            new PhotoCell { Label = "전" },
            new PhotoCell { Label = "중" },
            new PhotoCell { Label = "후" }
        ];
    }

    public static List<PhotoCell> DefaultOmitCells()
    {
        return [new PhotoCell()];
    }

    private bool HasLegacyFields()
    {
        return LegacyBefore is not null ||
               LegacyBeforeLabels is not null ||
               LegacyProcessing is not null ||
               LegacyProcessingLabels is not null ||
               LegacyAfter is not null ||
               LegacyAfterLabels is not null ||
               LegacyOther is not null ||
               LegacyOtherLabels is not null;
    }

    private void ClearLegacyFields()
    {
        LegacyBefore = null;
        LegacyBeforeLabels = null;
        LegacyProcessing = null;
        LegacyProcessingLabels = null;
        LegacyAfter = null;
        LegacyAfterLabels = null;
        LegacyOther = null;
        LegacyOtherLabels = null;
    }

    private static void AppendLegacyPhase(
        List<PhotoCell> destination,
        IReadOnlyList<string>? photos,
        IReadOnlyList<string>? labels,
        string defaultLabel)
    {
        photos ??= [];
        labels ??= [];
        var count = Math.Max(photos.Count, 1);
        for (var index = 0; index < count; index++)
        {
            destination.Add(new PhotoCell
            {
                Image = index < photos.Count ? AppState.NormalizePath(photos[index]) : string.Empty,
                Label = index < labels.Count
                    ? labels[index]?.Trim() ?? string.Empty
                    : index == 0 ? defaultLabel : string.Empty
            });
        }
    }

}
