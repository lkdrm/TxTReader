using System.Text;

namespace Reader.Tests;

/// <summary>
/// Helper class for managing test fixture files.
/// </summary>
public static class TestFixtures
{
    private static readonly string FixturesPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "..", "..", "..", "TestFixtures");

    /// <summary>
    /// Gets the full path to a test fixture file.
    /// </summary>
    public static string GetFixturePath(string fileName)
    {
        var path = Path.Combine(FixturesPath, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Test fixture file not found: {fileName}", path);
        }
        return path;
    }

    /// <summary>
    /// Creates a temporary file with specific content for testing.
    /// </summary>
    public static string CreateTempFile(string fileName, string content, string? tempDir = null)
    {
        tempDir ??= Path.GetTempPath();
        var filePath = Path.Combine(tempDir, fileName);
        // Use UTF8 without BOM to avoid BOM issues in tests
        var encoding = new UTF8Encoding(false);
        File.WriteAllText(filePath, content, encoding);
        return filePath;
    }

    /// <summary>
    /// Creates a temporary file with binary content for testing.
    /// </summary>
    public static string CreateTempFileBytes(string fileName, byte[] content, string? tempDir = null)
    {
        tempDir ??= Path.GetTempPath();
        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllBytes(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Generates a large file with specified number of lines.
    /// </summary>
    public static string GenerateLargeFile(int lineCount, string? tempDir = null)
    {
        tempDir ??= Path.GetTempPath();
        var fileName = $"large-{lineCount}-{Guid.NewGuid()}.txt";
        var filePath = Path.Combine(tempDir, fileName);

        // Use UTF8 without BOM
        var encoding = new UTF8Encoding(false);
        using var writer = new StreamWriter(filePath, false, encoding);
        for (int i = 0; i < lineCount; i++)
        {
            writer.WriteLine($"Line {i}");
        }

        return filePath;
    }

    /// <summary>
    /// Generates a file with a very long line for testing truncation.
    /// </summary>
    public static string GenerateLongLineFile(int lineLength, string? tempDir = null)
    {
        tempDir ??= Path.GetTempPath();
        var fileName = $"longline-{lineLength}-{Guid.NewGuid()}.txt";
        var filePath = Path.Combine(tempDir, fileName);

        var longLine = new string('A', lineLength);
        // Use UTF8 without BOM
        var encoding = new UTF8Encoding(false);
        File.WriteAllText(filePath, $"{longLine}\nShort line", encoding);

        return filePath;
    }
}
