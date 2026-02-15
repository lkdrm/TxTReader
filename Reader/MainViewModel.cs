using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Reader;

/// <summary>
/// Represents the main view model for the application, providing properties and commands for managing file content
/// display and user interactions.
/// </summary>
/// <remarks>MainViewModel supports asynchronous file operations and notifies the view of property changes to
/// enable responsive UI updates. It maintains the current scroll position, the maximum scrollable range, and the window
/// title, and exposes a collection of visible lines from the currently opened file. This class implements
/// INotifyPropertyChanged to support data binding in UI frameworks such as WPF.</remarks>
public class MainViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the instance of the FilesReader used for reading file data.
    /// </summary>
    /// <remarks>This field may be null if no FilesReader instance has been initialized. Ensure to check for
    /// null before accessing its methods.</remarks>
    private FilesReader? _filesReader;

    /// <summary>
    /// Holds the cancellation token source used to signal cancellation requests for asynchronous operations.
    /// </summary>
    /// <remarks>Set this field to null if cancellation is not required. This field is typically used to
    /// manage the lifetime of asynchronous tasks and to allow cooperative cancellation.</remarks>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Gets the collection of lines currently visible to the user in the interface.
    /// </summary>
    /// <remarks>The collection is updated dynamically to reflect changes in the displayed content. Modifying
    /// this collection directly may not update the user interface; use appropriate view model methods to change visible
    /// lines.</remarks>
    public ObservableCollection<string> VisibleLines { get; } = [];

    /// <summary>
    /// Represents the current vertical scroll position of the control, measured in pixels.
    /// </summary>
    /// <remarks>This field stores the number of pixels by which the content has been scrolled vertically.
    /// Ensure that the value remains within the valid range for the content's height to prevent unexpected
    /// behavior.</remarks>
    private long _scrollPosition;

    /// <summary>
    /// Stores the maximum scroll value allowed for the control.
    /// </summary>
    private long _maxScroll;

    /// <summary>
    /// Gets or sets the title of the window displayed to the user.
    /// </summary>
    /// <remarks>The default value is "Reader". This property can be modified to change the window title
    /// dynamically based on the application's state or context.</remarks>
    private string _windowTitle = "Reader";

    /// <summary>
    /// Gets or sets the current scroll position within the content.
    /// </summary>
    /// <remarks>Setting this property raises a property change notification and initiates an asynchronous
    /// load of the relevant content lines based on the new position. This property is typically used to control or
    /// track the visible portion of the content in a scrolling interface.</remarks>
    public long ScrollPosition
    {
        get => _scrollPosition;
        set
        {
            if (_scrollPosition != value)
            {
                _scrollPosition = value;
                OnPropertyChanged();
                LoadLinesAsync();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum scroll position allowed for the control.
    /// </summary>
    /// <remarks>Setting this property raises the property changed notification, which can be used to update
    /// UI elements or trigger other logic in response to changes.</remarks>
    public long MaxScroll
    {
        get => _maxScroll;
        set
        {
            _maxScroll = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the title of the window displayed to the user.
    /// </summary>
    /// <remarks>Changing this property raises a property change notification, allowing data-bound UI elements
    /// to update the displayed window title automatically.</remarks>
    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            _windowTitle = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Asynchronously opens the file at the specified path and initializes the file reader for subsequent operations.
    /// </summary>
    /// <remarks>If a file reader is already open, it is disposed of before opening the new file. The method
    /// updates the maximum scroll length, resets the scroll position, and sets the window title to reflect the opened
    /// file. If an error occurs while opening the file, an error message is displayed to the user.</remarks>
    /// <param name="path">The path to the file to be opened. This value must not be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task OpenFilesAsync(string path)
    {
        try
        {
            if (_filesReader != null)
            {
                await _filesReader.DisposeAsync();
            }

            _filesReader = new FilesReader(path);

            MaxScroll = _filesReader.FileLength;
            ScrollPosition = 0;
            WindowTitle = $"Reader - {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during opening the file: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously loads a set of lines from the file reader starting at the current scroll position and updates the
    /// collection of visible lines.
    /// </summary>
    /// <remarks>If the operation is canceled before completion, the visible lines collection is not updated.
    /// Any errors encountered during the reading process are logged for diagnostic purposes. This method ensures that
    /// only one load operation is active at a time by canceling any previous operation before starting a new
    /// one.</remarks>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadLinesAsync()
    {
        if (_filesReader == null)
        {
            return;
        }

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            await Task.Delay(15, token);
            var lines = await _filesReader.ReadAllLinesAsync(_scrollPosition, 50);

            if (!token.IsCancellationRequested)
            {
                VisibleLines.Clear();
                foreach (var line in lines)
                {
                    VisibleLines.Add(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during the reading: {ex.Message}");
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    /// <remarks>This event is typically used in data binding scenarios to notify subscribers that a property
    /// value has changed, enabling user interface elements to update automatically. Implementing this event is
    /// essential for classes that support property change notifications, such as those that implement the
    /// INotifyPropertyChanged interface.</remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event to notify listeners that a property value has changed.
    /// </summary>
    /// <remarks>Call this method within a property's setter to inform data binding clients, such as UI
    /// elements, that the property value has changed. This is essential for implementing the INotifyPropertyChanged
    /// interface and ensuring that bound controls update automatically.</remarks>
    /// <param name="name">The name of the property that changed. If not specified, the caller's member name is used.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
