namespace Herald.Core.Writing;

/// <summary>
/// The new file, and whether making it left everything outside the results block alone. The two
/// travel together so that nobody can take the content without having seen the verdict.
/// </summary>
/// <param name="Content">The file as it would be committed.</param>
/// <param name="ChangesOnlyTheResults">
/// False means herald would change more of the file than its own block.
/// That is a fault in herald, and the content must not be written anywhere.
/// </param>
internal sealed record WriteResult(string Content, bool ChangesOnlyTheResults);
