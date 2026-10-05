namespace Herald.Core.Publishing;

/// <summary>
/// Where a post stands against the clock on one run.
/// It says what herald would do with the file now.
/// </summary>
public enum PostDue
{
    /// <summary>
    /// Not approved. herald reads the file and does nothing with it.
    /// </summary>
    NotApproved = 0,

    /// <summary>
    /// Approved, and its time has not come.
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Due now, on the targets it has not been served on.
    /// </summary>
    Due = 2,

    /// <summary>
    /// Its time passed longer ago than the overdue limit, so herald leaves it alone.
    /// </summary>
    Overdue = 3,

    /// <summary>
    /// Every target the post names carries a result already.
    /// </summary>
    Served = 4,
}
