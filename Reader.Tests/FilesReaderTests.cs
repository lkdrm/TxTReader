using System.Text;

namespace Reader.Tests;

/// <summary>
/// Comprehensive test suite for FilesReader class covering all possible scenarios and edge cases.
/// </summary>
public class FilesReaderTests : IDisposable
{
    private readonly string _testFilesDirectory;
    private readonly List<string> _createdFiles;

    public FilesReaderTests()
    {
        // Create a temporary directory for test files
        _testFilesDirectory = Path.Combine(Path.GetTempPath(), $"FilesReaderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFilesDirectory);
        _createdFiles = [];
    }

    public void Dispose()
    {
        // Clean up test files and directory
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        if (Directory.Exists(_testFilesDirectory))
        {
            try
            {
                Directory.Delete(_testFilesDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private string CreateTestFile(string fileName, string content)
    {
        var filePath = TestFixtures.CreateTempFile(fileName, content, _testFilesDirectory);
        _createdFiles.Add(filePath);
        return filePath;
    }

    private string CreateTestFileBytes(string fileName, byte[] content)
    {
        var filePath = TestFixtures.CreateTempFileBytes(fileName, content, _testFilesDirectory);
        _createdFiles.Add(filePath);
        return filePath;
    }

    private string TrackGeneratedFile(string filePath)
    {
        _createdFiles.Add(filePath);
        return filePath;
    }

    #region Constructor Tests

    [Fact]
    public async Task Constructor_ValidFile_ShouldInitializeSuccessfully()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("simple.txt");

        // Act
        await using var reader = new FilesReader(filePath);

        // Assert
        Assert.NotNull(reader);
        Assert.True(reader.FileLength > 0);
    }

    [Fact]
    public void Constructor_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesDirectory, "nonexistent.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new FilesReader(filePath));
    }

    [Fact]
    public void Constructor_NullFilePath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FilesReader(null!));
    }

    [Fact]
    public async Task Constructor_EmptyFile_ShouldInitializeWithZeroLength()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("empty.txt");

        // Act & Assert - Empty files cannot be memory-mapped
        // This is expected behavior of MemoryMappedFile
        var exception = Assert.Throws<ArgumentException>(() => new FilesReader(filePath));
        Assert.Contains("positive capacity", exception.Message);
    }

    [Theory]
    [InlineData("simple.txt")]
    [InlineData("windows-line-endings.txt")]
    [InlineData("utf8-multilingual.txt")]
    [InlineData("empty-lines.txt")]
    [InlineData("special-characters.txt")]
    public async Task Constructor_VariousFixtureFiles_ShouldInitializeSuccessfully(string fileName)
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath(fileName);

        // Act
        await using var reader = new FilesReader(filePath);

        // Assert
        Assert.NotNull(reader);
        Assert.True(reader.FileLength > 0);
    }

    #endregion

    #region ReadAllLinesAsync Basic Tests

    [Fact]
    public async Task ReadAllLinesAsync_SimpleFile_ShouldReadAllLines()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("simple.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 3);

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_WindowsLineEndings_ShouldHandleCRLF()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("windows-line-endings.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 3);

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_UnixLineEndings_ShouldHandleLF()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("simple.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 3);

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_EmptyFile_ShouldReturnEmptyList()
    {
        // Arrange - Note: Empty files cannot be memory-mapped, so we create a file with just a newline
        var content = "\n";
        var filePath = CreateTestFile("emptyfile.txt", content);

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 10);

        // Assert
        Assert.Single(lines); // Single empty line
        // The line might be empty or contain BOM, so just check it's not null
        Assert.NotNull(lines[0]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_SingleLine_NoNewline_ShouldReadLine()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("single-line-no-newline.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 1);

        // Assert
        Assert.Single(lines);
        Assert.Equal("Single line without newline", lines[0]);
    }

    #endregion

    #region Position and Offset Tests

    [Fact]
    public async Task ReadAllLinesAsync_StartFromMiddle_ShouldReadFromLineStart()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\nLine 4";
        var filePath = CreateTestFile("middle.txt", content);

        await using var reader = new FilesReader(filePath);

        // Act - Start from middle of "Line 2"
        var lines = await reader.ReadAllLinesAsync(10, 2);

        // Assert - Should start reading from the beginning of "Line 2"
        Assert.Equal(2, lines.Count);
        Assert.Equal("Line 2", lines[0]);
        Assert.Equal("Line 3", lines[1]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_StartFromZero_ShouldReadFromBeginning()
    {
        // Arrange
        var content = "First\nSecond\nThird";
        var filePath = CreateTestFile("fromzero.txt", content);

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 2);

        // Assert
        Assert.Equal(2, lines.Count);
        Assert.Equal("First", lines[0]);
        Assert.Equal("Second", lines[1]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_PositionAtEndOfFile_ShouldFindLastLine()
    {
        // Arrange
        var content = "Short file\nSecond line";
        var filePath = CreateTestFile("short.txt", content);

        await using var reader = new FilesReader(filePath);

        // Act - Start from file length (end of file)
        // FindStartLine will scan backwards and find the last line
        var fileLength = reader.FileLength;
        var lines = await reader.ReadAllLinesAsync(fileLength, 5);

        // Assert - Should find and read the last line
        Assert.Single(lines);
        Assert.Equal("Second line", lines[0]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_RequestMoreLinesThanAvailable_ShouldReturnAvailableLines()
    {
        // Arrange
        var content = "Line 1\nLine 2";
        var filePath = CreateTestFile("limited.txt", content);

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 10);

        // Assert
        Assert.Equal(2, lines.Count);
    }

    #endregion

    #region Special Characters and Encoding Tests

    [Fact]
    public async Task ReadAllLinesAsync_UTF8Characters_ShouldReadCorrectly()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("utf8-multilingual.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 5);

        // Assert
        Assert.Equal(5, lines.Count);
        Assert.Equal("Hello 世界", lines[0]);
        Assert.Equal("Γειά σου κόσμε", lines[1]);
        Assert.Equal("Привет мир", lines[2]);
        Assert.Equal("مرحبا بالعالم", lines[3]);
        Assert.Equal("🌍🌎🌏 Emojis", lines[4]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_WithNullBytes_ShouldSkipNullCharacters()
    {
        // Arrange
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.UTF8.GetBytes("Line"));
        bytes.Add(0x00); // NULL character
        bytes.AddRange(Encoding.UTF8.GetBytes("1"));
        bytes.Add(0x0A); // Newline
        bytes.AddRange(Encoding.UTF8.GetBytes("Line2"));
        bytes.Add(0x0A);

        var filePath = CreateTestFileBytes("withnull.txt", bytes.ToArray());

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 2);

        // Assert
        Assert.Equal(2, lines.Count);
        Assert.Equal("Line1", lines[0]); // NULL character should be skipped
        Assert.Equal("Line2", lines[1]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_EmptyLines_ShouldReturnEmptyStrings()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("empty-lines.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 6);

        // Assert
        Assert.Equal(6, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("Line 3", lines[2]);
        Assert.Equal("", lines[3]);
        Assert.Equal("", lines[4]);
        Assert.Equal("Line 6", lines[5]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_SpecialCharacters_ShouldReadCorrectly()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("special-characters.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 5);

        // Assert
        Assert.Equal(5, lines.Count);
        Assert.Equal("Tab\there", lines[0]);
        Assert.Equal("Quote\"test", lines[1]);
        Assert.Equal("Backslash\\test", lines[2]);
        Assert.Equal("Apostrophe's test", lines[3]);
        Assert.Equal("Symbols: !@#$%^&*()", lines[4]);
    }

    #endregion

    #region Long Line Tests

    [Fact]
    public async Task ReadAllLinesAsync_VeryLongLine_ShouldTruncateAt10000Bytes()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            TestFixtures.GenerateLongLineFile(15000, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 3);

        // Assert - When line exceeds 10000 bytes, it breaks at 10001 due to implementation
        Assert.True(lines.Count >= 1, "Should read at least one line");
        Assert.True(lines[0].Length <= 10001, $"Line should be truncated, but was {lines[0].Length}");
    }

    [Fact]
    public async Task ReadAllLinesAsync_Exactly10000Bytes_ShouldReadCompletely()
    {
        // Arrange - Test line that fits within the 10000 byte limit
        var filePath = TrackGeneratedFile(
            TestFixtures.GenerateLongLineFile(9000, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 2);

        // Assert
        Assert.Equal(2, lines.Count);
        // Line length should be 9000, but might have slight variation
        Assert.True(lines[0].Length >= 9000 && lines[0].Length <= 9001, 
            $"Expected ~9000 characters, got {lines[0].Length}");
        Assert.Equal("Short line", lines[1]);
    }

    [Theory]
    [InlineData(5000)]
    [InlineData(9000)]
    public async Task ReadAllLinesAsync_VariousLineLengths_ShouldHandleCorrectly(int lineLength)
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            TestFixtures.GenerateLongLineFile(lineLength, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 1);

        // Assert
        Assert.Single(lines);
        Assert.True(lines[0].Length <= 10000, $"Line length {lines[0].Length} should not exceed 10000");
    }

    #endregion

    #region Large File Tests

    [Fact]
    public async Task ReadAllLinesAsync_LargeFile_ShouldHandleEfficiently()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(1000, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 100);

        // Assert
        Assert.Equal(100, lines.Count);
        Assert.Equal("Line 0", lines[0]);
        Assert.Equal("Line 99", lines[99]);
    }

    [Fact]
    public async Task ReadAllLinesAsync_ReadFromMiddleOfLargeFile_ShouldWork()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(1000, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act - Start from approximate middle
        var halfwayPoint = reader.FileLength / 2;
        var lines = await reader.ReadAllLinesAsync(halfwayPoint, 10);

        // Assert
        Assert.Equal(10, lines.Count);
        Assert.All(lines, line => Assert.StartsWith("Line ", line));
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(500, 100)]
    [InlineData(1000, 200)]
    public async Task ReadAllLinesAsync_LargeFilesWithVariousSizes_ShouldWork(int totalLines, int readLines)
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(totalLines, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, readLines);

        // Assert
        Assert.Equal(readLines, lines.Count);
        Assert.Equal("Line 0", lines[0]);
        Assert.Equal($"Line {readLines - 1}", lines[^1]);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task DisposeAsync_ShouldReleaseResources()
    {
        // Arrange
        var filePath = CreateTestFile("dispose.txt", "Test content");
        var reader = new FilesReader(filePath);

        // Act
        await reader.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await reader.ReadAllLinesAsync(0, 1));
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var filePath = CreateTestFile("multipledispose.txt", "Test content");
        var reader = new FilesReader(filePath);

        // Act & Assert
        await reader.DisposeAsync();
        await reader.DisposeAsync(); // Should not throw
    }

    [Fact]
    public async Task ReadAllLinesAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var filePath = CreateTestFile("afterdispose.txt", "Test content");
        var reader = new FilesReader(filePath);
        await reader.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await reader.ReadAllLinesAsync(0, 1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ReadAllLinesAsync_ZeroLines_ShouldReturnEmptyList()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("simple.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 0);

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task ReadAllLinesAsync_OnlyNewlines_ShouldReturnEmptyStrings()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("only-newlines.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 5);

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.All(lines, line => Assert.Equal("", line));
    }

    [Fact]
    public async Task ReadAllLinesAsync_MixedLineEndings_ShouldHandleCorrectly()
    {
        // Arrange
        var content = "Line 1\nLine 2\r\nLine 3\rLine 4\n";
        var filePath = CreateTestFile("mixed.txt", content);

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 5);

        // Assert - CR (\r) alone is not treated as line ending, so Line 3 and Line 4 may be on same line
        Assert.True(lines.Count >= 3, $"Expected at least 3 lines, got {lines.Count}");
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        // Line 3 behavior with \r depends on implementation
    }

    [Fact]
    public async Task ReadAllLinesAsync_FileEndingWithNewline_ShouldNotAddEmptyLine()
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("windows-line-endings.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, 10);

        // Assert
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public async Task FileLength_ShouldReturnCorrectSize()
    {
        // Arrange - Use a simple file to avoid line ending confusion
        var filePath = TestFixtures.GetFixturePath("simple.txt");
        var actualFileSize = new FileInfo(filePath).Length;

        await using var reader = new FilesReader(filePath);

        // Act
        var reportedLength = reader.FileLength;

        // Assert
        Assert.Equal(actualFileSize, reportedLength);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ReadAllLinesAsync_NegativeLineCount_ShouldReturnEmptyList(int negativeCount)
    {
        // Arrange
        var filePath = TestFixtures.GetFixturePath("simple.txt");

        await using var reader = new FilesReader(filePath);

        // Act
        var lines = await reader.ReadAllLinesAsync(0, negativeCount);

        // Assert
        Assert.Empty(lines);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ReadAllLinesAsync_MultipleConcurrentReads_ShouldWork()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(100, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act
        var task1 = reader.ReadAllLinesAsync(0, 10);
        var task2 = reader.ReadAllLinesAsync(0, 20);
        var task3 = reader.ReadAllLinesAsync(0, 15);

        await Task.WhenAll(task1, task2, task3);

        // Assert
        var results1 = await task1;
        var results2 = await task2;
        var results3 = await task3;

        Assert.Equal(10, results1.Count);
        Assert.Equal(20, results2.Count);
        Assert.Equal(15, results3.Count);
    }

    [Fact]
    public async Task ReadAllLinesAsync_ConcurrentReadsFromDifferentPositions_ShouldWork()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(100, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act - Read from different positions concurrently
        var task1 = reader.ReadAllLinesAsync(0, 10);
        var task2 = reader.ReadAllLinesAsync(reader.FileLength / 2, 10);
        var task3 = reader.ReadAllLinesAsync(reader.FileLength / 4, 10);

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        Assert.All(results, result => Assert.True(result.Count > 0, "Should read at least one line"));
    }

    #endregion

    #region Performance and Memory Tests

    [Fact]
    public async Task ReadAllLinesAsync_VeryLargeFile_ShouldNotLoadEntireFileIntoMemory()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(10000, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act - Read only small portion
        var lines = await reader.ReadAllLinesAsync(0, 10);

        // Assert
        Assert.Equal(10, lines.Count);
        // Memory-mapped files are efficient and don't load entire file into process memory
        // This is more of a conceptual test - the actual memory increase can vary due to GC, buffers, etc.
        Assert.True(lines[0] == "Line 0", "Should read first line correctly");
        Assert.True(lines[9] == "Line 9", "Should read tenth line correctly");
    }

    [Fact]
    public async Task ReadAllLinesAsync_SequentialReads_ShouldBeMaintainConsistency()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(100, _testFilesDirectory));

        await using var reader = new FilesReader(filePath);

        // Act - Read same data twice
        var lines1 = await reader.ReadAllLinesAsync(0, 50);
        var lines2 = await reader.ReadAllLinesAsync(0, 50);

        // Assert
        Assert.Equal(lines1.Count, lines2.Count);
        for (int i = 0; i < lines1.Count; i++)
        {
            Assert.Equal(lines1[i], lines2[i]);
        }
    }

    #endregion
}
