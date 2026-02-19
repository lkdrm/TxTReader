# TxTReader

A high-performance desktop application for reading and viewing text files, built with WPF and .NET 10.

## 🚀 Features

- **Efficient File Reading**: Uses memory-mapped files for optimal performance with large text files
- **Modern UI**: Clean and intuitive WPF interface
- **Fast Loading**: Asynchronous file reading with chunked processing
- **Large File Support**: Handles large files without loading them entirely into memory
- **Text Filtering**: Built-in filtering capabilities for quick content search
- **Cross-platform Ready**: Built on .NET 10 for modern Windows systems

## 🛠️ Technologies

- **.NET 10** - Latest .NET framework
- **WPF** - Windows Presentation Foundation for rich desktop UI
- **C#** - Modern C# with nullable reference types enabled
- **Memory-Mapped Files** - Efficient large file handling
- **MVVM Pattern** - Clean architecture with MainViewModel

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

1. Launch the application
2. Open a text file using the file picker
3. View and navigate through the file content
4. Use filtering options to search within the text

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

## 📝 License

This project is open source and available under the [MIT License](LICENSE).

## 👤 Author

**lkdrm**
- GitHub: [@lkdrm](https://github.com/lkdrm)

## ⭐ Show your support

Give a ⭐️ if this project helped you!
