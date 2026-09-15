namespace Herald.Core.Configuration;

/// <summary>
/// Configuration section names shared by the options classes. Every setting of the app lives under
/// <see cref="Root"/>. An application setting spells the section separator as <c>__</c>.
/// </summary>
public static class SectionNames
{
    public const string Root = "Herald";
    public const string Content = $"{Root}:Content";
}
