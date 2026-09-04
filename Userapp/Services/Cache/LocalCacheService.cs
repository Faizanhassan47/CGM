using System.Collections.Concurrent;
using System.Text.Json;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Services.Cache;

/// <summary>Encrypted, expiring cache for patient data. Cache keys are scoped and never clear app-wide preferences.</summary>
public sealed class LocalCacheService : ILocalCacheService
{
    private readonly ConcurrentDictionary<string, CacheEnvelope> _memory = new();
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new();

    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        if (!_memory.TryGetValue(key, out var envelope))
        {
            try
            {
                var stored = await SecureStorage.Default.GetAsync(StorageKey(key));
                envelope = string.IsNullOrWhiteSpace(stored) ? null : JsonSerializer.Deserialize<CacheEnvelope>(stored);
            }
            catch { return default; }
        }

        if (envelope is null) return default;
        if (envelope.ExpiresAt <= DateTime.UtcNow)
        {
            await RemoveAsync(key);
            return default;
        }

        try
        {
            _memory[key] = envelope;
            _knownKeys[key] = 0;
            return JsonSerializer.Deserialize<T>(envelope.Json);
        }
        catch
        {
            await RemoveAsync(key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var envelope = new CacheEnvelope(
            JsonSerializer.Serialize(value),
            expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue);
        _memory[key] = envelope;
        _knownKeys[key] = 0;
        await SecureStorage.Default.SetAsync(StorageKey(key), JsonSerializer.Serialize(envelope));
    }

    public Task RemoveAsync(string key)
    {
        _memory.TryRemove(key, out _);
        _knownKeys.TryRemove(key, out _);
        SecureStorage.Default.Remove(StorageKey(key));
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        foreach (var key in _knownKeys.Keys) SecureStorage.Default.Remove(StorageKey(key));
        _knownKeys.Clear();
        _memory.Clear();
        return Task.CompletedTask;
    }

    private static string StorageKey(string key) => $"cgm_secure_cache_{key}";
    private sealed record CacheEnvelope(string Json, DateTime ExpiresAt);
}
