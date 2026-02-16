using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace Reader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
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