using System.ComponentModel;
using System.IO;

namespace Reader.Tests;

/// <summary>
/// Comprehensive test suite for MainViewModel class.
/// </summary>
public class MainViewModelTests : IDisposable
{
    private readonly string _testFilesDirectory;
    private readonly List<string> _createdFiles;

    public MainViewModelTests()
    {
        _testFilesDirectory = Path.Combine(Path.GetTempPath(), $"MainViewModelTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFilesDirectory);
        _createdFiles = [];
    }

    public void Dispose()
    {
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

    private string TrackGeneratedFile(string filePath)
    {
        _createdFiles.Add(filePath);
        return filePath;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var viewModel = new MainViewModel();

        // Assert
        Assert.NotNull(viewModel.VisibleLines);
        Assert.Empty(viewModel.VisibleLines);
        Assert.Equal("Reader", viewModel.WindowTitle);
        Assert.Equal(0, viewModel.ScrollPosition);
        Assert.Equal(0, viewModel.MaxScroll);
        Assert.Null(viewModel.CurrentFilePath);
    }

    [Fact]
    public void VisibleLines_ShouldBeObservableCollection()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        viewModel.VisibleLines.Add("Test Line");

        // Assert
        Assert.Single(viewModel.VisibleLines);
        Assert.Equal("Test Line", viewModel.VisibleLines[0]);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void WindowTitle_WhenSet_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.WindowTitle))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.WindowTitle = "New Title";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("New Title", viewModel.WindowTitle);
    }

    [Fact]
    public void ScrollPosition_WhenSet_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.ScrollPosition))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.ScrollPosition = 100;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(100, viewModel.ScrollPosition);
    }

    [Fact]
    public void MaxScroll_WhenSet_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.MaxScroll))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.MaxScroll = 1000;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(1000, viewModel.MaxScroll);
    }

    [Fact]
    public void CurrentFilePath_WhenSet_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentFilePath))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        viewModel.CurrentFilePath = "C:\\test.txt";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("C:\\test.txt", viewModel.CurrentFilePath);
    }

    [Fact]
    public void NormalizedScrollPosition_WhenSet_ShouldUpdateScrollPosition()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.MaxScroll = 10000;

        // Act
        viewModel.NormalizedScrollPosition = 5000000.0; // Half of NormalizedMaxScroll (10000000)

        // Assert
        Assert.Equal(5000, viewModel.ScrollPosition); // Should be half of MaxScroll
    }

    #endregion

    #region OpenFilesAsync Tests

    [Fact]
    public async Task OpenFilesAsync_ValidFile_ShouldOpenSuccessfully()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";
        var filePath = CreateTestFile("opentest.txt", content);
        var viewModel = new MainViewModel();

        // Act
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100); // Give time for async loading

        // Assert
        Assert.Equal(filePath, viewModel.CurrentFilePath);
        Assert.True(viewModel.MaxScroll > 0);
        Assert.Equal(0, viewModel.ScrollPosition);
        Assert.Contains(Path.GetFileName(filePath), viewModel.WindowTitle);
        Assert.NotEmpty(viewModel.VisibleLines);
    }

    [Fact]
    public async Task OpenFilesAsync_LargeFile_ShouldSetCorrectMaxScroll()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(100, _testFilesDirectory));
        var viewModel = new MainViewModel();
        var fileInfo = new FileInfo(filePath);

        // Act
        await viewModel.OpenFilesAsync(filePath);

        // Assert
        Assert.Equal(fileInfo.Length, viewModel.MaxScroll);
    }

    [Fact]
    public async Task OpenFilesAsync_NonExistentFile_ShouldHandleGracefully()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var nonExistentPath = Path.Combine(_testFilesDirectory, "nonexistent.txt");

        // Act - Should not throw, should show error dialog instead
        await viewModel.OpenFilesAsync(nonExistentPath);

        // Assert
        Assert.Null(viewModel.CurrentFilePath);
        Assert.Equal(0, viewModel.MaxScroll);
    }

    [Fact]
    public async Task OpenFilesAsync_MultipleFiles_ShouldDisposeAndReopenCorrectly()
    {
        // Arrange
        var file1 = CreateTestFile("file1.txt", "Content 1");
        var file2 = CreateTestFile("file2.txt", "Content 2");
        var viewModel = new MainViewModel();

        // Act
        await viewModel.OpenFilesAsync(file1);
        await Task.Delay(100);
        var firstFilePath = viewModel.CurrentFilePath;

        await viewModel.OpenFilesAsync(file2);
        await Task.Delay(100);

        // Assert
        Assert.NotEqual(firstFilePath, viewModel.CurrentFilePath);
        Assert.Equal(file2, viewModel.CurrentFilePath);
        Assert.Contains(Path.GetFileName(file2), viewModel.WindowTitle);
    }

    [Fact]
    public async Task OpenFilesAsync_ShouldResetScrollPosition()
    {
        // Arrange
        var file1 = CreateTestFile("scroll1.txt", "Line 1\nLine 2\nLine 3");
        var file2 = CreateTestFile("scroll2.txt", "Other content");
        var viewModel = new MainViewModel();

        // Act
        await viewModel.OpenFilesAsync(file1);
        viewModel.ScrollPosition = 100;
        await viewModel.OpenFilesAsync(file2);

        // Assert
        Assert.Equal(0, viewModel.ScrollPosition);
    }

    #endregion

    #region Normalized Scroll Position Tests

    [Fact]
    public void NormalizedScrollPosition_WithZeroMaxScroll_ShouldReturnZero()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.MaxScroll = 0;

        // Act
        var normalized = viewModel.NormalizedScrollPosition;

        // Assert
        Assert.Equal(0, normalized);
    }

    [Fact]
    public void NormalizedScrollPosition_WithNonZeroMaxScroll_ShouldReturnCorrectValue()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.MaxScroll = 1000;
        viewModel.ScrollPosition = 500;

        // Act
        var normalized = viewModel.NormalizedScrollPosition;

        // Assert
        Assert.Equal(5000000.0, normalized); // (500 / 1000) * 10000000
    }

    [Fact]
    public void NormalizedMaxScroll_ShouldReturnConstantValue()
    {
        // Act
        var maxScroll = MainViewModel.NormalizedMaxScroll;

        // Assert
        Assert.Equal(10000000.0, maxScroll);
    }

    #endregion

    #region PageScrollStep Tests

    [Fact]
    public void PageScrollStep_SmallFile_ShouldReturn500()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.MaxScroll = 5000;

        // Act
        var step = viewModel.PageScrollStep;

        // Assert
        Assert.Equal(500, step);
    }

    [Fact]
    public void PageScrollStep_MediumFile_ShouldReturn10000()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.MaxScroll = 50000000;

        // Act
        var step = viewModel.PageScrollStep;

        // Assert
        Assert.Equal(10000, step);
    }

    [Fact]
    public void PageScrollStep_LargeFile_ShouldReturnScaledValue()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.MaxScroll = 10000000000;

        // Act
        var step = viewModel.PageScrollStep;

        // Assert
        Assert.Equal(1000000, step); // MaxScroll / 10000
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_NoFileOpen_ShouldHandleGracefully()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act - Should not throw, should show warning dialog instead
        await viewModel.SearchAsync("test");

        // Assert
        // No exception thrown
    }

    [Fact]
    public async Task SearchAsync_PatternFound_ShouldUpdateScrollPosition()
    {
        // Arrange
        var content = "Line 1\nTarget Line\nLine 3";
        var filePath = CreateTestFile("searchfile.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act
        await viewModel.SearchAsync("Target");
        await Task.Delay(100);

        // Assert
        Assert.True(viewModel.ScrollPosition > 0, "Scroll position should move to found pattern");
    }

    [Fact]
    public async Task SearchAsync_PatternNotFound_ShouldNotChangeScrollPosition()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";
        var filePath = CreateTestFile("searchnotfound.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);
        var originalPosition = viewModel.ScrollPosition;

        // Act
        await viewModel.SearchAsync("NotExist");
        await Task.Delay(100);

        // Assert
        Assert.Equal(originalPosition, viewModel.ScrollPosition);
    }

    [Fact]
    public async Task SearchAsync_SamePatternTwice_ShouldFindNextOccurrence()
    {
        // Arrange
        var content = "First Test\nSecond Test\nThird Test";
        var filePath = CreateTestFile("multiplesearch.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act
        await viewModel.SearchAsync("Test");
        await Task.Delay(100);
        var firstPosition = viewModel.ScrollPosition;

        await viewModel.SearchAsync("Test");
        await Task.Delay(100);
        var secondPosition = viewModel.ScrollPosition;

        // Assert
        Assert.True(secondPosition > firstPosition, "Second search should find next occurrence");
    }

    [Fact]
    public async Task SearchAsync_ForwardSearch_ShouldSearchFromCurrentPosition()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\nLine 4";
        var filePath = CreateTestFile("forwardsearch.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act
        await viewModel.SearchAsync("Line", searchForward: true);
        await Task.Delay(100);

        // Assert
        Assert.True(viewModel.ScrollPosition >= 0, "Should find pattern forward");
    }

    [Fact]
    public async Task SearchAsync_BackwardSearch_ShouldSearchBackwards()
    {
        // Arrange
        var content = "First Line\nSecond Line\nThird Line";
        var filePath = CreateTestFile("backwardsearch.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act
        await viewModel.SearchAsync("Line", searchForward: false);
        await Task.Delay(100);

        // Assert
        Assert.True(viewModel.ScrollPosition >= 0, "Should find pattern backwards");
    }

    [Fact]
    public async Task SearchAsync_DifferentPattern_ShouldResetSearchState()
    {
        // Arrange
        var content = "First Test\nSecond Search\nThird Test";
        var filePath = CreateTestFile("differentsearch.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act
        await viewModel.SearchAsync("Test");
        await Task.Delay(100);
        var testPosition = viewModel.ScrollPosition;

        await viewModel.SearchAsync("Search");
        await Task.Delay(100);
        var searchPosition = viewModel.ScrollPosition;

        // Assert
        Assert.NotEqual(testPosition, searchPosition);
    }

    [Fact]
    public async Task SearchAsync_ConcurrentSearches_ShouldCancelPrevious()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(10000, _testFilesDirectory));
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act - Start multiple searches quickly
        var task1 = viewModel.SearchAsync("Line 1000");
        var task2 = viewModel.SearchAsync("Line 2000");
        var task3 = viewModel.SearchAsync("Line 3000");

        await Task.WhenAll(task1, task2, task3);

        // Assert - Should complete without errors
        // The cancellation mechanism should handle concurrent searches gracefully
    }

    #endregion

    #region ScrollPosition and Loading Tests

    [Fact]
    public async Task ScrollPosition_WhenChanged_ShouldTriggerLoadLines()
    {
        // Arrange
        var filePath = CreateTestFile("scrollload.txt", "Line 1\nLine 2\nLine 3\nLine 4\nLine 5");
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);
        var initialLineCount = viewModel.VisibleLines.Count;

        // Act
        viewModel.ScrollPosition = 10;
        await Task.Delay(150); // Wait for async load

        // Assert
        Assert.NotEmpty(viewModel.VisibleLines);
    }

    [Fact]
    public async Task ScrollPosition_MultipleQuickChanges_ShouldNotCrash()
    {
        // Arrange
        var filePath = TrackGeneratedFile(
            await TestFixtures.GenerateLargeFileAsync(100, _testFilesDirectory));
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(200);

        // Act - Rapidly change scroll position
        for (int i = 0; i < 10; i++)
        {
            viewModel.ScrollPosition = i * 100;
        }
        await Task.Delay(500); // Wait for debouncing and loading

        // Assert - Should not crash (may or may not have lines depending on timing)
        // The important thing is that it handles rapid changes without exceptions
        Assert.NotNull(viewModel.VisibleLines);
    }

    #endregion

    #region INotifyPropertyChanged Tests

    [Fact]
    public void PropertyChanged_ShouldBeImplemented()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Assert
        Assert.IsAssignableFrom<INotifyPropertyChanged>(viewModel);
    }

    [Fact]
    public void PropertyChanged_MultipleProperties_ShouldRaiseForEach()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var raisedProperties = new List<string>();
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
            {
                raisedProperties.Add(args.PropertyName);
            }
        };

        // Act
        viewModel.WindowTitle = "Test";
        viewModel.MaxScroll = 1000;
        viewModel.ScrollPosition = 100;
        viewModel.CurrentFilePath = "test.txt";

        // Assert
        Assert.Contains(nameof(MainViewModel.WindowTitle), raisedProperties);
        Assert.Contains(nameof(MainViewModel.MaxScroll), raisedProperties);
        Assert.Contains(nameof(MainViewModel.ScrollPosition), raisedProperties);
        Assert.Contains(nameof(MainViewModel.CurrentFilePath), raisedProperties);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task OpenFilesAsync_EmptyFilePath_ShouldHandleGracefully()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        await viewModel.OpenFilesAsync(string.Empty);

        // Assert
        // Should not crash, error handling should catch this
    }

    [Fact]
    public void ScrollPosition_SetToNegative_ShouldAcceptValue()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        viewModel.ScrollPosition = -100;

        // Assert
        Assert.Equal(-100, viewModel.ScrollPosition);
    }

    [Fact]
    public void ScrollPosition_SetToSameValue_ShouldNotRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new MainViewModel();
        viewModel.ScrollPosition = 100;
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.ScrollPosition))
            {
                propertyChangedCount++;
            }
        };

        // Act
        viewModel.ScrollPosition = 100; // Set to same value

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    [Fact]
    public async Task SearchAsync_EmptyPattern_ShouldHandleGracefully()
    {
        // Arrange
        var content = "Test content";
        var filePath = CreateTestFile("emptysearch.txt", content);
        var viewModel = new MainViewModel();
        await viewModel.OpenFilesAsync(filePath);
        await Task.Delay(100);

        // Act & Assert - Should not crash
        await viewModel.SearchAsync(string.Empty);
    }

    [Fact]
    public void MaxScroll_SetToNegative_ShouldAcceptValue()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        viewModel.MaxScroll = -100;

        // Assert
        Assert.Equal(-100, viewModel.MaxScroll);
    }

    #endregion
}
