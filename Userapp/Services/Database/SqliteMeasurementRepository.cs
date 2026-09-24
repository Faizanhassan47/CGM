using SQLite;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Database;

public sealed class SqliteMeasurementRepository : ILocalMeasurementRepository
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqliteMeasurementRepository(string? customDbPath = null)
    {
        var basePath = customDbPath;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            basePath = Path.Combine(folder, "cgm_local_cache.db3");
        }
        _dbPath = basePath;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
            _database = new SQLiteAsyncConnection(_dbPath, flags);
            await _database.CreateTableAsync<LocalMeasurement>();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }
        return _database ?? throw new InvalidOperationException("SQLite Database could not be initialized.");
    }

    public async Task<int> SaveMeasurementAsync(LocalMeasurement measurement)
    {
        var db = await GetDatabaseAsync();
        if (measurement.Id != 0)
        {
            await db.UpdateAsync(measurement);
            return measurement.Id;
        }
        return await db.InsertAsync(measurement);
    }

    public async Task<List<LocalMeasurement>> GetPendingMeasurementsAsync(int limit = 100)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalMeasurement>()
            .Where(m => m.SyncStatus == "Pending")
            .OrderBy(m => m.MeasuredAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task MarkAsSyncedAsync(IEnumerable<int> ids)
    {
        var db = await GetDatabaseAsync();
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var id in idList)
        {
            var item = await db.Table<LocalMeasurement>().FirstOrDefaultAsync(m => m.Id == id);
            if (item != null)
            {
                item.SyncStatus = "Synced";
                item.SyncedAt = now;
                await db.UpdateAsync(item);
            }
        }
    }

    public async Task MarkAsFailedAsync(int id, string error)
    {
        var db = await GetDatabaseAsync();
        var item = await db.Table<LocalMeasurement>().FirstOrDefaultAsync(m => m.Id == id);
        if (item != null)
        {
            item.RetryCount++;
            item.LastError = error;
            if (item.RetryCount >= 5)
            {
                item.SyncStatus = "Failed";
            }
            await db.UpdateAsync(item);
        }
    }

    public async Task<List<LocalMeasurement>> GetRecentMeasurementsAsync(int limit = 50)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalMeasurement>()
            .OrderByDescending(m => m.MeasuredAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<LocalMeasurement>> GetMeasurementsForDateAsync(DateTime date, int limit = 100)
    {
        var db = await GetDatabaseAsync();
        var localStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);
        var startUtc = localStart.ToUniversalTime();
        var endUtc = localStart.AddDays(1).ToUniversalTime();

        var list = await db.Table<LocalMeasurement>()
            .Where(m => m.MeasuredAt >= startUtc && m.MeasuredAt < endUtc)
            .OrderByDescending(m => m.MeasuredAt)
            .Take(limit)
            .ToListAsync();

        if (list.Count == 0 && date.Date == DateTime.Today)
        {
            return await db.Table<LocalMeasurement>()
                .OrderByDescending(m => m.MeasuredAt)
                .Take(limit)
                .ToListAsync();
        }

        return list;
    }

    public async Task<int> GetPendingCountAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalMeasurement>()
            .Where(m => m.SyncStatus == "Pending")
            .CountAsync();
    }
}
