namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves resource paths declared inside notable-date XML resources.
/// </summary>
public interface IResourcePathResolver
{
    /// <summary>
    /// Resolves a child resource path relative to the specified document resource path.
    /// </summary>
    /// <param name="documentPath">The fully qualified resource path of the document declaring the reference.</param>
    /// <param name="childPath">The referenced child resource path.</param>
    /// <returns>The fully qualified resolved resource path.</returns>
    string Resolve(string documentPath, string childPath);
}