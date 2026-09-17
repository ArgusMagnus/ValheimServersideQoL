using UnityEngine;

namespace ServersideQoL.Utilities;

sealed class DebouncedFileWatcher(string directoryPath, string filter) : IDisposable
{
  const int DebounceTimeMs = 500;
  readonly FileSystemWatcher _fileWatcher = new(EnsureDirectoryExists(directoryPath), filter);
  int _fileWatcherCounter;


  FileSystemEventHandler? _fileCreatedOrChanged;
  public event FileSystemEventHandler? FileCreatedOrChanged
  {
    add
    {
      if (_fileCreatedOrChanged is null && value is not null)
      {
        _fileWatcher.Created += OnFileCreatedOrChanged;
        _fileWatcher.Changed += OnFileCreatedOrChanged;
        _fileWatcher.Renamed += OnFileCreatedOrChanged;
      }
      _fileCreatedOrChanged += value;
    }
    remove
    {
      _fileCreatedOrChanged -= value;
      if (_fileCreatedOrChanged is null)
      {
        _fileWatcher.Created -= OnFileCreatedOrChanged;
        _fileWatcher.Changed -= OnFileCreatedOrChanged;
        _fileWatcher.Renamed -= OnFileCreatedOrChanged;
      }
    }
  }

  public bool Enabled { get => _fileWatcher.EnableRaisingEvents; set => _fileWatcher.EnableRaisingEvents = value; }

  public DebouncedFileWatcher(string filePath)
    : this(Path.GetDirectoryName(filePath) ?? throw new ArgumentException("Full path expected"), Path.GetFileName(filePath)) { }

  static string EnsureDirectoryExists(string path)
  {
    Directory.CreateDirectory(path);
    return path;
  }

  async void OnFileCreatedOrChanged(object sender, FileSystemEventArgs e)
  {
    var c = Interlocked.Increment(ref _fileWatcherCounter);
    await Task.Delay(DebounceTimeMs).ConfigureAwait(false);
    if (c != Volatile.Read(ref _fileWatcherCounter))
      return;

    await Awaitable.MainThreadAsync();
    if (c != Volatile.Read(ref _fileWatcherCounter))
      return;

    try { _fileCreatedOrChanged?.Invoke(this, e); }
    catch (Exception ex) { ServersideQoLPlugin.Logger.LogError(ex); }
  }

  public void Dispose() => _fileWatcher.Dispose();
}
