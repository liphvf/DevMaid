namespace DevMaid.Services;

/// <summary>
/// Provides methods to locate PostgreSQL binary executables.
/// This is a compatibility wrapper using DevMaid.Core.Services.PostgresBinaryLocator.
/// </summary>
public static class PostgresBinaryLocator
{
    /// <summary>
    /// Finds the pg_dump executable in the system PATH or common installation directories.
    /// </summary>
    /// <returns>The full path to pg_dump, or null if not found.</returns>
    public static string? FindPgDump()
    {
        return DevMaid.Core.Services.PostgresBinaryLocator.FindPgDump();
    }

    /// <summary>
    /// Finds the pg_restore executable in the system PATH or common installation directories.
    /// </summary>
    /// <returns>The full path to pg_restore, or null if not found.</returns>
    public static string? FindPgRestore()
    {
        return DevMaid.Core.Services.PostgresBinaryLocator.FindPgRestore();
    }

    /// <summary>
    /// Finds the psql executable in the system PATH or common installation directories.
    /// </summary>
    /// <returns>The full path to psql, or null if not found.</returns>
    public static string? FindPsql()
    {
        return DevMaid.Core.Services.PostgresBinaryLocator.FindPsql();
    }
}
