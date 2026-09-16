namespace Herald.Core.Models;

/// <summary>
/// What herald records for a post or a comment on one target. It is written by herald only, and
/// it lives in the results block, never at the top of the file.
/// </summary>
public enum PublishStatus
{
    /// <summary>
    /// Live on the target. Requires the id the target returned.
    /// </summary>
    Published = 0,

    /// <summary>
    /// Not published, and not attempted again. Requires an error.
    /// </summary>
    Failed = 1,
}
