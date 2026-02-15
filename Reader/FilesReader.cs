using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Reader;

/// <summary>
/// Provides functionality to read lines from a file using memory-mapped file access.
/// </summary>
/// <remarks>This class allows asynchronous reading of lines from a specified file, enabling efficient access to
/// large files without loading them entirely into memory. The file length can be accessed through the `FileLength`
/// property. Ensure to dispose of the instance properly to release resources.</remarks>
public class FilesReader : IAsyncDisposable
{
    /// <summary>
    /// Pattern indicating a newline character (line feed, LF)
    /// <br>Decimal: 10</br>
    /// <br>Hexadecimal: 0x0A</br>
    /// <br>Binary: 00001010 (ASCII line feed)</br>
    /// </summary>
    private const int NewLineCharacter = 0x0A;

    /// <summary>
    /// Pattern indicating a null character (NULL)
    /// <br>Decimal: 0</br>
    /// <br>Hexadecimal: 0x00</br>
    /// <br>Binary: 00000000 (ASCII null)</br>
    /// </summary>
    private const int NullCharacter = 0x00;

    /// <summary>
    /// The total length of the file in bytes.
    /// </summary>
    private readonly long _fileLength;

    /// <summary>
    /// The memory-mapped file instance used for efficient file access without loading the entire file into memory.
    /// </summary>
    private MemoryMappedFile? _memoryMappedFile;

    /// <summary>
    /// Indicates whether the object has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Gets the total length of the file in bytes.
    /// </summary>
    public long FileLength => _fileLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesReader"/> class for reading lines from the specified file
    /// using memory-mapped file access.
    /// </summary>
    /// <param name="filePath">The path to the file to be read. Must be a valid, accessible file path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have the required permission to access the file.</exception>
    public FilesReader(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        _fileLength = fileInfo.Length;

        // Create a memory-mapped file for efficient read-only access to the entire file without loading it into memory
        _memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
    }

    /// <summary>
    /// Asynchronously reads a specified number of lines from a memory-mapped file, starting at the given byte position.
    /// </summary>
    /// <remarks>This method is intended for efficient, asynchronous reading of lines from large files without
    /// blocking the calling thread.</remarks>
    /// <param name="positionBytes">The byte position within the file at which to begin reading. Must be a non-negative value within the bounds of
    /// the file.</param>
    /// <param name="countLines">The maximum number of lines to read from the file. Must be a positive integer.</param>
    /// <returns>A list of strings containing the lines read from the file. The list may be empty if no lines are read.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the method is called after the FilesReader object has been disposed.</exception>
    public async Task<List<string>> ReadAllLinesAsync(long positionBytes, int countLines)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await Task.Run(() =>
        {
            List<string> resultLines = [];

            // Create a view accessor for the entire file with read-only access (0, 0 parameters map the full file length)
            using (var accessor = _memoryMappedFile!.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
            {
                // Find the start of the line to ensure we read complete lines from a safe boundary position
                long safePosition = FindStartLine(accessor, positionBytes);
                long currentPosition = safePosition;

                for (int i = 0; i < countLines; i++)
                {
                    if (currentPosition >= _fileLength)
                    {
                        break;
                    }

                    // Read a line from the current position and get the next position to continue reading
                    var (line, nextPosition) = ReadLineFromAccessor(accessor, currentPosition);
                    resultLines.Add(line);
                    currentPosition = nextPosition;
                }
            }
            return resultLines;
        });
    }

    /// <summary>
    /// Finds the byte position marking the start of the line in a memory-mapped file, scanning backwards from the
    /// specified position.
    /// </summary>
    /// <remarks>This method scans backwards from the specified position to locate the start of the line,
    /// stopping at the first byte that is not a continuation byte. It then searches for the last newline character
    /// (0x0A) to determine the start of the line.</remarks>
    /// <param name="accessor">The memory-mapped view accessor used to read bytes from the file.</param>
    /// <param name="positionBytes">The byte position from which to begin searching for the start of the line. Must be greater than zero.</param>
    /// <returns>The byte position of the start of the line, or 0 if no line start is found.</returns>
    private long FindStartLine(MemoryMappedViewAccessor accessor, long positionBytes)
    {
        if (positionBytes <= 0)
        {
            return 0;
        }

        long positions = positionBytes;

        // Scan backwards from the safe position to find the last newline character (0x0A)
        // Return the position immediately after the newline to mark the start of the current line
        while (positions >= 0)
        {
            byte b = accessor.ReadByte(positions);
            if (b == NewLineCharacter)
            {
                return positions + 1;
            }
            positions--;
        }
        return 0;
    }

    /// <summary>
    /// Reads a line of text from a memory-mapped file, starting at the specified position.
    /// </summary>
    /// <remarks>Reading stops when a newline character (NewLineCharacter) is encountered, a null byte (NullCharacter) is skipped,
    /// or the line exceeds 10,000 bytes. The method is intended for sequential reading of lines from large files using
    /// memory mapping.</remarks>
    /// <param name="accessor">The memory-mapped view accessor used to read bytes from the underlying file.</param>
    /// <param name="startPosition">The position within the memory-mapped view from which to begin reading the line.</param>
    /// <returns>A tuple containing the read line as a string and the position of the next byte to be read after the line.</returns>
    private (string line, long nextPosition) ReadLineFromAccessor(MemoryMappedViewAccessor accessor, long startPosition)
    {
        List<byte> bytes = [];

        long positions = startPosition;

        while (positions < _fileLength)
        {
            byte b = accessor.ReadByte(positions);
            positions++;
            if (b == NewLineCharacter)
            {
                break;
            }

            if (b == NullCharacter)
            {
                continue;
            }

            bytes.Add(b);

            if (bytes.Count > 10000)
            {
                break;
            }
        }

        if (bytes.Count > 0 && bytes[^1] == '\r')
        {
            bytes.RemoveAt(bytes.Count - 1);
        }
        return (Encoding.UTF8.GetString(bytes.ToArray()), positions);
    }

    /// <summary>
    /// Asynchronously releases the unmanaged resources used by the object and optionally releases the managed
    /// resources.
    /// </summary>
    /// <remarks>Call this method to clean up resources when the object is no longer needed. This method
    /// suppresses finalization to prevent the garbage collector from calling the finalizer for this object.</remarks>
    /// <returns>A value task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await Task.Run(() => Dispose(true));
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the class and, optionally, releases the managed resources.
    /// </summary>
    /// <remarks>This method is called by public Dispose methods and the finalizer. When disposing is true,
    /// this method releases all resources held by managed objects. Override this method in a derived class to release
    /// additional resources. Always call the base class implementation when overriding.</remarks>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _memoryMappedFile?.Dispose();
                _memoryMappedFile = null;
            }
            _disposed = true;
        }
    }
}
