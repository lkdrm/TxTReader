# TxTReader

A high-performance desktop application for reading and viewing text files, built with WPF and .NET 10.

## 📌 Version

**Current Version: 1.2**

### Changelog

- **v1.2** - Enhanced Features & Download Support
  - Added file download from URL support with HttpClient
  - Added random text file generator for testing
  - Improved keyboard shortcuts (Ctrl+F for search, F3 for next, PageUp/PageDown for navigation)
  - Added keyboard shortcuts for End/Home navigation
  - Optimized file operations with 1 MB buffer size for better performance
  - Added temporary file management

- **v1.1** - Dark Mode Support
  - Added dark and light theme support
  - Theme toggle control in the UI

- **v1.0** - Initial Release
  - Core text file reading functionality
  - File filtering and search capabilities

## 🚀 Features

- **Efficient File Reading**: Optimized with 1 MB buffer size for high-performance reading of large text files
- **Modern UI**: Clean and intuitive WPF interface with dark/light theme support
- **Fast Navigation**: Asynchronous file reading with chunked processing and smooth scrolling
- **Large File Support**: Handles large files efficiently without loading them entirely into memory
- **Advanced Search & Filter**: 
  - Text search with regex support
  - Line filtering with case-insensitive matching
  - F3 or Enter to find next occurrence
  - Shift+F3 to find previous occurrence
- **URL Download Support**: Download and open files directly from URLs with progress indication
- **Test Data Generator**: Generate random large text files for testing and benchmarking
- **Keyboard Navigation**: 
  - Ctrl+F: Focus search box
  - F3: Find next match
  - Shift+F3: Find previous match
  - PageUp/PageDown: Scroll by page
  - Home/End: Jump to start/end of file
- **Theme Customization**: Toggle between dark and light themes
- **File Operations**: Open, save, filter, and download files with proper error handling
- **Cross-platform Ready**: Built on .NET 10 for modern Windows systems

## 🛠️ Technologies

- **.NET 10** - Latest .NET framework
- **C# 14** - Modern C# with advanced language features
- **WPF** - Windows Presentation Foundation for rich desktop UI
- **HttpClient** - Built-in HTTP client for downloading files
- **StreamWriter/StreamReader** - Efficient file I/O operations with buffering
- **MVVM Pattern** - Clean architecture with MainViewModel and separated concerns

## 📋 Prerequisites

- Windows OS
- .NET 10 Runtime or SDK

## 🔧 Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/lkdrm/TxTReader.git
   cd TxTReader
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project Reader.csproj
   ```

## 🧪 Testing

The project includes a test suite in the `Reader.Tests` project:

```bash
dotnet test
```

## 📁 Project Structure

```
TxTReader/
├── Reader.csproj           # Main application project
├── MainWindow.xaml         # Main UI window
├── MainWindow.xaml.cs      # Main window code-behind
├── MainViewModel.cs        # ViewModel for MVVM pattern
├── FilesReader.cs          # Core file reading logic with memory-mapped files
├── App.xaml                # Application resources
├── Styles/
│   └── app.ico            # Application icon
└── Reader.Tests/          # Unit tests
    └── Reader.Tests.csproj
```

## 🎯 Usage

### Opening Files
1. Launch the application
2. Click "Open" to browse and select a text file, or use the "Download" feature to open files from URLs

### Navigation
- **Scroll**: Use mouse wheel, PageUp/PageDown, or Home/End keys
- **Search**: Press Ctrl+F to focus the search box, then press Enter or F3 to find matches
- **Find Next**: Press F3 (or Shift+F3 to find previous)

### Filtering
- Check the "Filter" checkbox to show only lines matching the search text
- Uncheck to return to the original file

### Testing
- Click "Random" to generate a large test file with random text data

### Theme
- Toggle between dark and light themes using the theme toggle control

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| Ctrl+F | Focus search box |
| Enter | Perform search |
| F3 | Find next match |
| Shift+F3 | Find previous match |
| PageUp | Scroll up by page |
| PageDown | Scroll down by page |
| Home | Jump to start of file |
| End | Jump to end of file |

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

## 📝 License

This project is open source and available under the [MIT License](LICENSE).

## 👤 Author

**lkdrm**
- GitHub: [@lkdrm](https://github.com/lkdrm)

## ⭐ Show your support

Give a ⭐️ if this project helped you!
