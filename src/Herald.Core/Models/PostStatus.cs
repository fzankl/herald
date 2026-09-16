namespace Herald.Core.Models;

/// <summary>
/// The status the author sets at the top of a post file. What herald records per target is
/// <see cref="PublishStatus"/>, so that the two owners never write the same field.
/// </summary>
public enum PostStatus
{
    /// <summary>
    /// Not released. herald reads the file and does nothing with it.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Released. herald publishes it to every target once its time has passed.
    /// </summary>
    Approved = 1,
}
