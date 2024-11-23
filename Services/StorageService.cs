using System.IO;
using System.Text.Json;
using Noats.Models;

namespace Noats.Services;

public class StorageService
{
    private readonly string _appDataPath;
    private readonly string _statePath;
    private readonly string _backupPath;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public StorageService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Noats"
        );
        _statePath = Path.Combine(_appDataPath, "state.json");
        _backupPath = Path.Combine(_appDataPath, "state.backup.json");

        System.Diagnostics.Debug.WriteLine($"Using storage path: {_appDataPath}");
        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_appDataPath))
        {
            Directory.CreateDirectory(_appDataPath);
        }
    }

    public async Task SaveStateAsync(AppState state)
    {
        try
        {
            // If we have an existing state file, make it the backup
            if (File.Exists(_statePath))
            {
                File.Copy(_statePath, _backupPath, true);
            }

            // Save new state
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await File.WriteAllTextAsync(_statePath, json);
        }
        catch (Exception ex)
        {
            // Log error and potentially notify user
            throw new InvalidOperationException("Failed to save application state", ex);
        }
    }

    public async Task<AppState> LoadStateAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Loading state from: {_statePath}");

            // Try loading main state file
            if (File.Exists(_statePath))
            {
                var json = await File.ReadAllTextAsync(_statePath);
                System.Diagnostics.Debug.WriteLine($"Loaded json: {json}");
                var state = JsonSerializer.Deserialize<AppState>(json, _jsonOptions);

                if (state != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded {state.Noats.Count} noats");
                    return state;
                }
            }

            // If main file fails, try backup
            if (File.Exists(_backupPath))
            {
                var json = await File.ReadAllTextAsync(_backupPath);
                var state = JsonSerializer.Deserialize<AppState>(json, _jsonOptions);

                if (state != null)
                {
                    // Restore backup as main file
                    File.Copy(_backupPath, _statePath, true);
                    return state;
                }
            }

            // If all else fails, start fresh
            return new AppState
            {
                LastSaved = DateTime.UtcNow,
                Version = 1,
                Noats = []
            };
        }
        catch (Exception ex)
        {
            // Basic file system logging for diagnostic purposes
            var errorPath = Path.Combine(_appDataPath, "load_error.log");
            await File.AppendAllTextAsync(errorPath,
                $"[{DateTime.UtcNow:u}] Failed to load state: {ex.Message}\n{ex.StackTrace}\n\n");

            return new AppState
            {
                LastSaved = DateTime.UtcNow,
                Version = 1,
                Noats = []
            };
        }
    }

    public bool HasSavedStateAsync()
    {
        return File.Exists(_statePath) || File.Exists(_backupPath);
    }

    public void DeleteState()
    {
        if (File.Exists(_statePath))
        {
            File.Delete(_statePath);
        }
        if (File.Exists(_backupPath))
        {
            File.Delete(_backupPath);
        }
    }
}
