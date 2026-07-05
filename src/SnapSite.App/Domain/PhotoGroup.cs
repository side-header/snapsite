using System.Text.Json.Serialization;

namespace NewGreen.Domain;

public sealed class PhotoGroup
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("before")]
    public List<string> Before { get; set; } = [];

    [JsonPropertyName("beforeLabels")]
    public List<string> BeforeLabels { get; set; } = [];

    [JsonPropertyName("processing")]
    public List<string> Processing { get; set; } = [];

    [JsonPropertyName("processingLabels")]
    public List<string> ProcessingLabels { get; set; } = [];

    [JsonPropertyName("after")]
    public List<string> After { get; set; } = [];

    [JsonPropertyName("afterLabels")]
    public List<string> AfterLabels { get; set; } = [];

    [JsonPropertyName("cntPerPage")]
    public int CntPerPage { get; set; } = 3;

    public List<string> Photos(Phase phase)
    {
        return phase switch
        {
            Phase.Before => Before,
            Phase.Processing => Processing,
            Phase.After => After,
            _ => Before
        };
    }

    public List<string> Labels(Phase phase)
    {
        return phase switch
        {
            Phase.Before => BeforeLabels,
            Phase.Processing => ProcessingLabels,
            Phase.After => AfterLabels,
            _ => BeforeLabels
        };
    }

    public string LabelAt(Phase phase, int index)
    {
        NormalizeLabels();
        var labels = Labels(phase);
        return index >= 0 && index < labels.Count ? labels[index] : string.Empty;
    }

    public void SetLabel(Phase phase, int index, string label)
    {
        EnsureLabelCount(phase, index + 1);
        Labels(phase)[index] = label.Trim();
    }

    public void InsertPhoto(Phase phase, string relativePath, int targetIndex, string? label)
    {
        NormalizeLabels();
        var photos = Photos(phase);
        var labels = Labels(phase);
        targetIndex = Math.Clamp(targetIndex, 0, photos.Count);

        var nextLabel = label;
        if (nextLabel is null && photos.Count == 0 && targetIndex == 0 && labels.Count > 0)
        {
            nextLabel = labels[0];
        }

        photos.Insert(targetIndex, relativePath);
        if (photos.Count == 1 && labels.Count == 1)
        {
            labels[0] = nextLabel ?? string.Empty;
        }
        else
        {
            labels.Insert(targetIndex, nextLabel ?? string.Empty);
        }

        NormalizeLabels();
    }

    public string? RemovePhotoWithLabel(string relativePath)
    {
        foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
        {
            var photos = Photos(phase);
            var index = photos.FindIndex(path => AppState.SamePath(path, relativePath));
            if (index < 0)
            {
                continue;
            }

            photos.RemoveAt(index);
            var labels = Labels(phase);
            var label = index < labels.Count ? labels[index] : string.Empty;
            if (index < labels.Count)
            {
                labels.RemoveAt(index);
            }

            NormalizeLabels();
            return label;
        }

        NormalizeLabels();
        return null;
    }

    public void NormalizeLabels()
    {
        NormalizeLabels(Phase.Before);
        NormalizeLabels(Phase.Processing);
        NormalizeLabels(Phase.After);
    }

    private void EnsureLabelCount(Phase phase, int count)
    {
        var labels = Labels(phase);
        while (labels.Count < count)
        {
            labels.Add(labels.Count == 0 ? phase.Label() : string.Empty);
        }
    }

    private void NormalizeLabels(Phase phase)
    {
        var labels = Labels(phase);
        var count = Math.Max(Photos(phase).Count, 1);
        EnsureLabelCount(phase, count);
        if (labels.Count > count)
        {
            labels.RemoveRange(count, labels.Count - count);
        }
    }
}
