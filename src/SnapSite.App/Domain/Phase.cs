namespace NewGreen.Domain;

public enum Phase
{
    Before,
    Processing,
    After
}

public static class PhaseExtensions
{
    public static string Key(this Phase phase)
    {
        return phase switch
        {
            Phase.Before => "before",
            Phase.Processing => "processing",
            Phase.After => "after",
            _ => phase.ToString().ToLowerInvariant()
        };
    }

    public static string Label(this Phase phase)
    {
        return phase switch
        {
            Phase.Before => "전",
            Phase.Processing => "중",
            Phase.After => "후",
            _ => phase.ToString()
        };
    }
}
