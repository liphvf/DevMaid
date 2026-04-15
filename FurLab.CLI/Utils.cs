using System.Text;

namespace FurLab.CLI;

/// <summary>
/// General utility methods for the FurLab CLI.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Detects and returns the encoding of a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The detected <see cref="Encoding"/> of the file.</returns>
    public static Encoding GetCurrentFileEncoding(string filePath)
    {
        using var sr = new StreamReader(filePath, true);
        while (sr.Peek() >= 0)
        {
            sr.Read();
        }

        return sr.CurrentEncoding;
    }
}
