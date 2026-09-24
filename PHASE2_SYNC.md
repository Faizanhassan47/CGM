# Phase 2 reliable synchronization

The mobile application continues to use SQL Server through the existing API; SQLite was not added. Measurements awaiting upload are stored by the existing durable secure cache as structured `SyncQueueItem` records.

Each queue record contains an ID, entity type, measurement, status, retry count, creation time, last-attempt time, and last error. `SyncService` uses a zero-wait `SemaphoreSlim` gate so overlapping manual, immediate, and scheduled sync requests are skipped. It uploads at most 100 readings through the existing `POST /api/glucose/sync-bulk` contract and removes only IDs acknowledged by a successful response.

Failed uploads are retained and retried three times before being marked `Failed`. The one-minute scheduler uses `PeriodicTimer` and a lifetime cancellation token. `SyncService.Dispose` stops both safely.
