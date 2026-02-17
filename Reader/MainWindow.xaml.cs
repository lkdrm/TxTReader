using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Reader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Provides a static instance of HttpClient for making HTTP requests.
    /// </summary>
    /// <remarks>This instance is intended to be reused throughout the application to take advantage of
    /// connection pooling and reduce resource consumption. It is recommended to use a single instance of HttpClient for
    /// the lifetime of the application to avoid socket exhaustion issues.</remarks>
    private static readonly HttpClient _httpClient = new();

    public MainWindow()
    {
        InitializeComponent();

        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    /// <summary>
    /// Handles the PreviewKeyDown event for the main window to capture keyboard shortcuts.
    /// </summary>
    /// <remarks>This method listens for Ctrl+F to focus the search box, allowing users to quickly
    /// initiate a search operation.</remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data containing information about the key press.</param>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.F3)
        {
            string textToFind = SearchBox.Text;
            if (string.IsNullOrEmpty(textToFind))
            {
                return;
            }

            var viewModel = this.DataContext as MainViewModel;
            if (viewModel != null)
            {
                bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                _ = viewModel.SearchAsync(textToFind, !isShiftPressed);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.PageUp || e.Key == Key.PageDown)
        {
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel == null)
            {
                return;
            }

            long offset = 50;

            if (e.Key is Key.PageUp)
            {
                viewModel.ScrollPosition = Math.Max(0, viewModel.ScrollPosition - offset);
            }
            else
            {
                viewModel.ScrollPosition = Math.Min(viewModel.MaxScroll, viewModel.ScrollPosition + offset);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.End || e.Key == Key.Home)
        {
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel == null)
            {
                return;
            }
            if (e.Key is Key.End)
            {
                viewModel.ScrollPosition = viewModel.MaxScroll;
            }
            else
            {
                viewModel.ScrollPosition = 0;
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the click event to display an open file dialog and asynchronously opens the selected file using the
    /// associated view model.
    /// </summary>
    /// <remarks>This method presents an open file dialog to the user. If a file is selected and the data
    /// context is a MainViewModel, it calls the OpenFilesAsync method to process the selected file
    /// asynchronously.</remarks>
    /// <param name="sender">The source of the event, typically the UI element that was clicked.</param>
    /// <param name="e">The event data associated with the click event.</param>
    private async void OpenFile_click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.OpenFilesAsync(openFileDialog.FileName);
            }
        }
    }

    /// <summary>
    /// Handles the Click event of the search button and initiates an asynchronous search operation using the text
    /// entered in the search box.
    /// </summary>
    /// <remarks>If the search box is empty, the method does not perform a search. The search operation is
    /// executed asynchronously by calling the SearchAsync method of the MainViewModel.</remarks>
    /// <param name="sender">The source of the event, typically the search button that was clicked.</param>
    /// <param name="e">The event data associated with the button click.</param>
    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        string textToFind = SearchBox.Text;

        if (string.IsNullOrEmpty(textToFind))
        {
            return;
        }

        var viewModel = this.DataContext as MainViewModel;

        if (viewModel != null)
        {
            await viewModel.SearchAsync(textToFind);
        }
    }

    /// <summary>
    /// Handles the click event for the download button, initiating an asynchronous download of a file from the URL
    /// specified in the input box.
    /// </summary>
    /// <remarks>If the URL input is empty or invalid, the user is prompted to enter a valid link. The method
    /// disables the button during the download process and restores its state upon completion or error. Any exceptions
    /// encountered during the download are caught and displayed to the user.</remarks>
    /// <param name="sender">The source of the event, typically the button that was clicked to start the download operation.</param>
    /// <param name="e">The event data associated with the button click, containing information relevant to the routed event.</param>
    private async void DownloadUrl_Click(object sender, RoutedEventArgs e)
    {
        string url = UrlBox.Text;
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Enter please valid link");
            return;
        }

        try
        {
            var button = sender as Button;
            string originalText = button.Content.ToString();
            button.Content = "Loading...";
            button.IsEnabled = false;

            string tempFilePath = Path.GetTempFileName();

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            {
                using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
                using var streamToWrite = File.Open(tempFilePath, FileMode.Create);
                await streamToReadFrom.CopyToAsync(streamToWrite);
            }
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel != null)
            {
                await viewModel.OpenFilesAsync(tempFilePath);
            }

            button.Content = originalText;
            button.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during the downloading: {ex.Message}");
            (sender as Button).Content = "Download";
            (sender as Button).IsEnabled = true;
        }
    }

    /// <summary>
    /// Handles the click event to generate a large file containing random text and opens it in the application.
    /// </summary>
    /// <remarks>This method generates a temporary file with a fixed number of lines, each containing randomly
    /// selected words from a predefined list. The button is disabled during the operation to prevent multiple
    /// invocations and re-enabled upon completion. Any errors encountered during the process are displayed to the
    /// user.</remarks>
    /// <param name="sender">The source of the event, typically the button that was clicked.</param>
    /// <param name="e">The event data associated with the click event.</param>
    private async void GenerateRandom_click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        button.IsEnabled = false;
        button.Content = "Generating";

        Random rnd = new();

        string[] words = { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur",
                       "adipiscing", "elit", "sed", "do", "eiusmod", "tempor",
                       "incididunt", "ut", "labore", "et", "dolore", "magna",
                       "aliqua", "ut", "enim", "ad", "minim", "veniam", "quis",
                       "nostrud", "exercitation", "ullamco", "laboris", "nisi",
                       "ut", "aliquip", "ex", "ea", "commodo", "consequat" };

        try
        {
            string tempFilePath = Path.GetTempFileName();
            int linesCount = 500000;

            await Task.Run(() =>
            {
                using var writer = new StreamWriter(tempFilePath);
                {
                    var sb = new StringBuilder();

                    for (int i = 0; i < linesCount; i++)
                    {
                        sb.Clear();
                        sb.Append($"Row {i + 1}:");
                        int wordsInLine = rnd.Next(5, 20);

                        for (int j = 0; j < wordsInLine; j++)
                        {
                            string word = words[rnd.Next(words.Length)];
                            sb.Append(word).Append(' ');
                        }

                        writer.WriteLine(sb.ToString());
                    }
                }
            });

            var viewModel = this.DataContext as MainViewModel;
            if (viewModel != null)
            {
                await viewModel.OpenFilesAsync(tempFilePath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during text generation: {ex}");
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "Random";
        }
    }

    /// <summary>
    /// Handles the click event for the Save button and prompts the user to select a location to save the current
    /// document as a text file.
    /// </summary>
    /// <remarks>If the current file path is not set in the data context, an error message is displayed and
    /// the save operation is not performed. The method uses a SaveFileDialog to allow the user to specify the file name
    /// and location. If the save operation fails, an error message is shown to the user.</remarks>
    /// <param name="sender">The source of the event, typically the Save button that was clicked.</param>
    /// <param name="e">The event data associated with the click event.</param>
    private async void Save_click(object sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as MainViewModel;

        if (viewModel == null || string.IsNullOrEmpty(viewModel.CurrentFilePath))
        {
            MessageBox.Show("Mistake was happens during the saving");
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            FileName = "BigText",
            DefaultExt = ".txt",
            Filter = "Text documents (.txt)|*.txt|All files (*.*)|*.*",
            InitialDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.Copy(viewModel.CurrentFilePath, saveDialog.FileName, true);
                MessageBox.Show("File has been saved");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Can`t save a file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles the PreviewMouseWheel event for the content grid, updating the scroll position in the associated view
    /// model based on mouse wheel movement.
    /// </summary>
    /// <remarks>This method adjusts the scroll position in the view model according to the mouse wheel delta,
    /// ensuring the position remains within valid bounds. The event is marked as handled to suppress the default
    /// scrolling behavior.</remarks>
    /// <param name="sender">The source of the event, typically the content grid that received the mouse wheel input.</param>
    /// <param name="e">The event data containing information about the mouse wheel movement.</param>
    private void ContentGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Calculate scroll amount (negative delta means scroll down)
            // Multiply by 3 for more responsive scrolling
            long scrollAmount = -e.Delta * 3;

            // Update scroll position, ensuring it stays within bounds
            long newPosition = viewModel.ScrollPosition + scrollAmount;
            newPosition = Math.Max(0, Math.Min(newPosition, viewModel.MaxScroll));

            viewModel.ScrollPosition = newPosition;

            // Mark event as handled to prevent default scrolling behavior
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the KeyDown event for the search box, triggering a search when the Enter key is pressed.
    /// </summary>
    /// <remarks>This method allows users to initiate a search by pressing Enter while the search box has
    /// focus.</remarks>
    /// <param name="sender">The source of the event, typically the search box.</param>
    /// <param name="e">The event data containing information about the key press.</param>
    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Cancel the routed event, preventing further processing
            e.Handled = true;

            // Retrieve the search text from the SearchBox TextBox
            string searchText = SearchBox.Text;

            // If search text is not empty, execute the search command
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                // Get the view model from the DataContext
                var viewModel = DataContext as MainViewModel;

                // If the view model is available, execute the search asynchronously
                if (viewModel != null)
                {
                    await viewModel.SearchAsync(searchText);
                }
            }
        }
    }
}