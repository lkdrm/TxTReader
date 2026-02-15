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
}