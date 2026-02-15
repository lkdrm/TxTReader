# FilesReader Tests

Comprehensive test suite for the `FilesReader` class covering all possible scenarios, edge cases, and potential issues.

## Test Coverage

### 1. Constructor Tests
- ✅ **Valid File Initialization**: Tests successful creation with a valid file
- ✅ **Non-Existent File**: Validates `FileNotFoundException` is thrown for missing files
- ✅ **Null FilePath**: Validates `ArgumentNullException` is thrown for null path
- ✅ **Empty File**: Tests initialization with zero-length file

### 2. Basic Reading Tests
- ✅ **Simple File Reading**: Reads multiple lines from a basic text file
- ✅ **Windows Line Endings (CRLF)**: Handles `\r\n` line endings correctly
- ✅ **Unix Line Endings (LF)**: Handles `\n` line endings correctly
- ✅ **Empty File Reading**: Returns empty list for empty files
- ✅ **Single Line Without Newline**: Reads file with no trailing newline

### 3. Position and Offset Tests
- ✅ **Reading from Middle**: Tests starting from middle of file (finds line start)
- ✅ **Reading from Zero**: Tests reading from beginning
- ✅ **Position Beyond File Length**: Returns empty list when position exceeds file size
- ✅ **Request More Lines Than Available**: Returns only available lines

### 4. Special Characters and Encoding Tests
- ✅ **UTF-8 Characters**: Handles Unicode characters (Chinese, Greek, Cyrillic)
- ✅ **Null Bytes**: Skips NULL characters (0x00) as per implementation
- ✅ **Empty Lines**: Correctly returns empty strings for blank lines
- ✅ **Special Characters**: Handles tabs, quotes, backslashes

### 5. Long Line Tests
- ✅ **Lines Exceeding 10,000 Bytes**: Tests truncation at 10,000 byte limit
- ✅ **Exactly 10,000 Bytes**: Tests boundary condition

### 6. Large File Tests
- ✅ **Large File Reading**: Tests efficient reading of files with 1000+ lines
- ✅ **Reading from Middle of Large File**: Tests offset reading in large files

### 7. Disposal Tests
- ✅ **Proper Resource Disposal**: Tests `DisposeAsync` releases resources
- ✅ **Multiple Disposal Calls**: Ensures idempotent disposal
- ✅ **Post-Disposal Access**: Validates `ObjectDisposedException` after disposal

### 8. Edge Cases
- ✅ **Zero Lines Request**: Returns empty list when requesting 0 lines
- ✅ **Only Newlines**: Handles files containing only newline characters
- ✅ **Mixed Line Endings**: Handles mixed `\n`, `\r\n`, and `\r` combinations
- ✅ **File Ending with Newline**: Doesn't add extra empty line
- ✅ **FileLength Property**: Returns correct byte count

### 9. Concurrent Access Tests
- ✅ **Multiple Concurrent Reads**: Tests thread-safety with simultaneous reads

## Test File Management

All tests use **mock files** that are:
- Created in a temporary directory (`%TEMP%\FilesReaderTests_<GUID>`)
- Automatically cleaned up after each test run
- Created dynamically for each test scenario

No external test files are required - all test data is generated in-memory.

## Running the Tests

### Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Click "Run All Tests"

### Command Line
```bash
dotnet test
```

### Running Specific Tests
```bash
dotnet test --filter "FullyQualifiedName~FilesReaderTests.Constructor"
dotnet test --filter "FullyQualifiedName~ReadAllLinesAsync"
```

## Test Scenarios

### Happy Path Scenarios
- Reading sequential lines from start
- Reading from arbitrary positions
- Handling different line endings
- UTF-8 encoded content

### Error Scenarios
- Missing files
- Null arguments
- Disposed objects
- Position beyond file length

### Edge Cases
- Empty files
- Single-line files
- Files without trailing newlines
- Very long lines (>10,000 bytes)
- Files with NULL bytes
- Mixed line endings

### Performance Scenarios
- Large file handling (1000+ lines)
- Concurrent access from multiple threads
- Memory-mapped file efficiency

## Known Behaviors Tested

1. **NULL Character Handling**: The implementation skips NULL bytes (0x00)
2. **Line Length Limit**: Lines are truncated at 10,000 bytes
3. **Carriage Return Handling**: Trailing `\r` characters are removed
4. **Line Start Finding**: When reading from middle, it backs up to find line start
5. **Async Disposal**: Must use `await using` pattern

## Test Data Examples

### Simple File
```
Line 1
Line 2
Line 3
```

### Windows Line Endings
```
Line 1\r\n
Line 2\r\n
Line 3\r\n
```

### UTF-8 Content
```
Hello 世界
Γειά σου κόσμε
Привет мир
```

### With NULL Bytes
```
Line[NULL]1
Line2
```

## Maintenance

When adding new tests:
1. Follow the existing naming convention: `MethodName_Scenario_ExpectedBehavior`
2. Use AAA pattern (Arrange, Act, Assert)
3. Add mock file creation using `CreateTestFile()` or `CreateTestFileBytes()`
4. Add documentation to this README
5. Group tests by category using `#region` blocks

## Dependencies

- **xUnit**: Testing framework
- **.NET 10 (Windows)**: Target framework
- **No external files required**: All test data is generated dynamically
