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
    /// Represents the default size, in bytes, of the buffer used for data operations.
    /// </summary>
    private const int BufferSize = 4096;

    /// <summary>
    /// Represents the average size, in bytes, of a line used for internal memory allocation estimates.
    /// </summary>
    /// <remarks>This constant is intended for internal calculations related to line processing and should not
    /// be modified.</remarks>
    private const long AverageLineSize = 65536L;

    /// <summary>
    /// The margin size, in bytes, to be used for chunk buffer allocations.
    /// </summary>
    private const int ChunkBufferMargin = 8192;

    /// <summary>
    /// Gets the default chunk size used for data processing, set to 1 MB.
    /// </summary>
    private const int DefaultChunkSize = 1024 * 1024;

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

        _memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
    }

    /// <summary>
    /// Asynchronously reads a specified number of lines from a memory-mapped file, starting at the given byte position.
    /// </summary>
    /// <remarks>This method may throw an ObjectDisposedException if the underlying file has been disposed. It
    /// is important to ensure that the specified position and count are valid to avoid unexpected behavior.</remarks>
    /// <param name="positionBytes">The byte position in the file from which to begin reading lines. Must be non-negative and within the bounds of
    /// the file length.</param>
    /// <param name="countLines">The number of lines to read from the file. Must be a positive integer.</param>
    /// <returns>A list of strings containing the lines read from the file. The list will be empty if no lines are read or if the
    /// specified position is beyond the end of the file.</returns>
    public async Task<List<string>> ReadAllLinesAsync(long positionBytes, int countLines)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await Task.Run(() =>
        {
            List<string> resultLines = [];

            long chunkStart = Math.Max(0, positionBytes - BufferSize);
            long estimatedChunkSize = Math.Max((countLines * AverageLineSize) + ChunkBufferMargin, DefaultChunkSize);
            long chunkSize = Math.Min(estimatedChunkSize, _fileLength - chunkStart);

            if (chunkSize <= 0)
            {
                return resultLines;
            }

            using (var accessor = _memoryMappedFile!.CreateViewAccessor(chunkStart, chunkSize, MemoryMappedFileAccess.Read))
            {
                long offsetInChunk = positionBytes - chunkStart;
                long safeOffset = FindStartLineInChunk(accessor, offsetInChunk, chunkSize);
                long currentOffset = safeOffset;

                for (int i = 0; i < countLines; i++)
                {
                    long absolutePosition = chunkStart + currentOffset;
                    if (absolutePosition >= _fileLength || currentOffset >= chunkSize)
                    {
                        break;
                    }

                    var (line, nextOffset) = ReadLineFromChunk(accessor, currentOffset, chunkSize);
                    resultLines.Add(line);
                    currentOffset = nextOffset;
                }
            }
            return resultLines;
        });
    }

    /// <summary>
    /// Finds the position of the first character of a line within a specified chunk of a memory-mapped file, scanning
    /// backwards from a given offset.
    /// </summary>
    /// <remarks>This method scans backwards from the specified offset to locate the first newline character.
    /// If the offset is less than or equal to zero, the method returns 0 immediately. The returned position can be used
    /// to identify the beginning of a line for further processing.</remarks>
    /// <param name="accessor">The memory-mapped view accessor used to read bytes from the chunk.</param>
    /// <param name="offsetInChunk">The offset within the chunk from which to begin searching for the start of a line. Must be greater than zero.</param>
    /// <param name="chunkSize">The total size, in bytes, of the chunk being accessed. Must be greater than zero.</param>
    /// <returns>The position of the start of the line within the chunk, or 0 if no newline character is found before the offset.</returns>
    private static long FindStartLineInChunk(MemoryMappedViewAccessor accessor, long offsetInChunk, long chunkSize)
    {
        if (offsetInChunk <= 0)
        {
            return 0;
        }

        long position = Math.Min(offsetInChunk, chunkSize - 1);

        while (position >= 0)
        {
            byte b = accessor.ReadByte(position);
            if (b == NewLineCharacter)
            {
                return position + 1;
            }
            position--;
        }
        return 0;
    }

    /// <summary>
    /// Reads a line of text from a specified position within a memory-mapped file chunk and returns the line along with
    /// the offset position immediately after the line.
    /// </summary>
    /// <remarks>The method stops reading at a new line character or after a maximum of 10,000 bytes to
    /// prevent excessive memory usage. Null characters are ignored during reading. If the line ends with a carriage
    /// return character, it is removed from the result.</remarks>
    /// <param name="accessor">The memory-mapped view accessor used to read bytes from the file chunk.</param>
    /// <param name="startOffset">The zero-based offset within the chunk at which to begin reading.</param>
    /// <param name="chunkSize">The total size, in bytes, of the chunk to be read. Reading will not exceed this limit.</param>
    /// <returns>A tuple containing the read line as a UTF-8 string and the next offset position after the line.</returns>
    private static (string line, long nextOffset) ReadLineFromChunk(MemoryMappedViewAccessor accessor, long startOffset, long chunkSize)
    {
        List<byte> bytes = [];
        long position = startOffset;

        while (position < chunkSize)
        {
            byte b = accessor.ReadByte(position);
            position++;

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

        return (Encoding.UTF8.GetString(bytes.ToArray()), position);
    }

    /// <summary>
    /// Searches asynchronously for the specified UTF-8 encoded byte pattern within the file, starting at the given
    /// offset.
    /// </summary>
    /// <remarks>The method reads the file in chunks to optimize performance. If the operation is canceled, an
    /// OperationCanceledException is thrown.</remarks>
    /// <param name="pattern">The string pattern to search for. The pattern is encoded as UTF-8 before searching.</param>
    /// <param name="startOffSet">The zero-based position in the file from which to begin the search. Must be non-negative and less than the file
    /// length.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the search operation.</param>
    /// <returns>The zero-based index of the first occurrence of the pattern in the file, or -1 if the pattern is not found.</returns>
    public async Task<long> SearchPatternAsync(string pattern, long startOffSet = 0, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] patternBytes = Encoding.UTF8.GetBytes(pattern);
        int overlap = patternBytes.Length - 1;
        int bufferSize = 4 * DefaultChunkSize;
        long currentPosition = startOffSet;

        while (currentPosition < _fileLength)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] buffer = await ReadBytesAsync(currentPosition, bufferSize);
            ReadOnlySpan<byte> searchBytes = buffer.AsSpan();

            int localIndex = searchBytes.IndexOf(patternBytes);

            if (localIndex != -1)
            {
                return currentPosition + localIndex;
            }

            if (buffer.Length < bufferSize)
            {
                break;
            }
            currentPosition += (buffer.Length - overlap);
        }
        return -1;
    }

    /// <summary>
    /// Searches for the specified string pattern in the file, starting from the given offset and moving backwards, and
    /// returns the position of the last occurrence.
    /// </summary>
    /// <remarks>The method reads the file in chunks and searches backwards for the specified pattern. Throws
    /// an ObjectDisposedException if the object has been disposed. The search is performed using UTF-8 encoding for the
    /// pattern.</remarks>
    /// <param name="pattern">The string pattern to search for. This parameter cannot be null or empty.</param>
    /// <param name="startOffSet">The zero-based position in the file from which to begin the search. Specify -1 to start from the end of the
    /// file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>The zero-based position of the last occurrence of the pattern in the file, or -1 if the pattern is not found.</returns>
    public async Task<long> SearchPatternBackwardsAsync(string pattern, long startOffSet, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] patternBytes = Encoding.UTF8.GetBytes(pattern);
        int overlap = patternBytes.Length - 1;
        int bufferSize = 4 * DefaultChunkSize;
        long currentEndPosition = (startOffSet == -1) ? _fileLength : startOffSet;

        while (currentEndPosition > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long readStartPosition = Math.Max(0, currentEndPosition - bufferSize);
            int bytesToRead = (int)(currentEndPosition - readStartPosition);

            byte[] buffer = await ReadBytesAsync(readStartPosition, bytesToRead);
            ReadOnlySpan<byte> searchBytes = buffer.AsSpan();

            int localIndex = searchBytes.LastIndexOf(patternBytes);

            if (localIndex != -1)
            {
                return readStartPosition + localIndex;
            }

            if (readStartPosition == 0)
            {
                break;
            }

            currentEndPosition = readStartPosition + overlap;
        }
        return -1;
    }

    /// <summary>
    /// Asynchronously reads a specified number of bytes from the memory-mapped file starting at the given position.
    /// </summary>
    /// <remarks>If the requested buffer size exceeds the remaining length of the file, only the available
    /// bytes are read. This method uses a memory-mapped file accessor to perform the read operation
    /// asynchronously.</remarks>
    /// <param name="currentPostition">The zero-based position in the file at which to begin reading. Must be less than the total length of the file.</param>
    /// <param name="bufferSize">The maximum number of bytes to read from the file. Must be a positive integer.</param>
    /// <returns>A byte array containing the bytes read from the file. The length of the array may be less than the specified
    /// buffer size if fewer bytes are available.</returns>
    private async Task<byte[]> ReadBytesAsync(long currentPostition, int bufferSize) =>
        await Task.Run(() =>
        {
            // Ensure bufferSize doesn't exceed remaining file length
            int actualBufferSize = (int)Math.Min(bufferSize, _fileLength - currentPostition);

            using var accessor = _memoryMappedFile!.CreateViewAccessor(currentPostition, actualBufferSize, MemoryMappedFileAccess.Read);
            byte[] buffer = new byte[actualBufferSize];
            int read = accessor.ReadArray(0, buffer, 0, actualBufferSize);

            if (read < actualBufferSize)
            {
                Array.Resize(ref buffer, read);
            }
            return buffer;
        });

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
