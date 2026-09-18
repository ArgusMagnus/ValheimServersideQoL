using UnityEngine;

namespace ServersideQoL.Utilities;

sealed class DebouncedFileWatcher(string filePath) : IDisposable
{
  const int DebounceTimeMs = 500;
  readonly FileSystemWatcher _fileWatcher = new(EnsureDirectoryExists(Path.GetDirectoryName(filePath) ?? throw new ArgumentException("Full path expected")), Path.GetFileName(filePath));
  int _fileWatcherCounter;
  CancellationTokenSource? _cts;

  static string EnsureDirectoryExists(string path)
  {
    Directory.CreateDirectory(path);
    return path;
  }

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

  public bool Enabled
  {
    get => _fileWatcher.EnableRaisingEvents;
    set
    {
      if (_fileWatcher.EnableRaisingEvents == value)
        return;

      _fileWatcher.EnableRaisingEvents = value;
      if (_cts is not null)
      {
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
      }
      if (!value)
        Interlocked.Increment(ref _fileWatcherCounter);
      else if (!Config.IsInitialized || Config.Instance.AutoReloadPollingIntervalSeconds.Value > 0)
      {
        var cts = _cts = new();
        Task.Run(() => Poll(cts.Token), cts.Token);
      }
    }
  }

  async void OnFileCreatedOrChanged(object sender, FileSystemEventArgs e)
  {
    try
    {
      var c = Interlocked.Increment(ref _fileWatcherCounter);
      await Task.Delay(DebounceTimeMs).ConfigureAwait(false);
      if (c != Volatile.Read(ref _fileWatcherCounter))
        return;

      await Awaitable.MainThreadAsync();
      if (c != Volatile.Read(ref _fileWatcherCounter))
        return;
      _fileCreatedOrChanged?.Invoke(this, e);
    }
    catch (Exception ex) { ServersideQoLPlugin.Logger.LogError(ex); }
  }

  public void Dispose()
  {
    Enabled = false;
    _fileWatcher.Dispose();
  }

  async Task Poll(CancellationToken cancellationToken)
  {
    try
    {
      var fileInfo = new FileInfo(Path.Combine(_fileWatcher.Path, _fileWatcher.Filter));
      fileInfo.Refresh();
      var existed = fileInfo.Exists;
      var wasEnabled = false;
      while (!cancellationToken.IsCancellationRequested)
      {
        if (!Config.IsInitialized)
        {
          await Task.Delay(200, cancellationToken);
          continue;
        }

        var interval = Config.Instance.AutoReloadPollingIntervalSeconds.Value;
        if (interval <= 0)
        {
          if (!wasEnabled)
            break;
          await Task.Delay(5000, cancellationToken);
          continue;
        }
        wasEnabled = true;

        if (!File.Exists(fileInfo.FullName))
          existed = false;
        else
        {
          try
          {
            var (length, lastWrite) = (fileInfo.Length, fileInfo.LastWriteTimeUtc);
            fileInfo.Refresh();
            if (!existed)
              OnFileCreatedOrChanged(this, new(WatcherChangeTypes.Created, _fileWatcher.Path, _fileWatcher.Filter));
            else if ((length, lastWrite) != (fileInfo.Length, fileInfo.LastWriteTimeUtc))
              OnFileCreatedOrChanged(this, new(WatcherChangeTypes.Changed, _fileWatcher.Path, _fileWatcher.Filter));
            existed = true;
          }
          catch (Exception ex) { ServersideQoLPlugin.Logger.LogError(ex); }
        }

        await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
      }
    }
    catch (OperationCanceledException) { }
    catch (Exception ex) { ServersideQoLPlugin.Logger.LogError(ex); }
  }
}
