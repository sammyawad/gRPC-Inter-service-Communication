using System.Collections.Concurrent;

namespace GrpcService.Services;

public class DataStore
{
    // Store latest value per client for quick lookup
    private readonly ConcurrentDictionary<string, decimal> _latest = new();

    // Store recent time-series per client
    private readonly ConcurrentDictionary<string, List<DataPoint>> _history = new();

    // Retention settings
    private readonly TimeSpan _retention = TimeSpan.FromMinutes(5); // keep last 5 minutes
    private readonly int _maxPointsPerSeries = 3000; // safety cap

    public IReadOnlyDictionary<string, decimal> SnapshotLatest()
        => _latest;

    public IReadOnlyDictionary<string, IReadOnlyList<DataPoint>> SnapshotHistory()
        => _history.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<DataPoint>)kvp.Value.AsReadOnly());

    public void Update(string clientId, decimal value)
    {
        var now = DateTime.UtcNow;
        _latest[clientId] = value;

        var list = _history.GetOrAdd(clientId, _ => new List<DataPoint>(_maxPointsPerSeries));
        lock (list)
        {
            list.Add(new DataPoint { Timestamp = now, Value = value });
            // Trim by time window
            var minTime = now - _retention;
            var removeCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Timestamp < minTime) removeCount++;
                else break;
            }
            if (removeCount > 0 && removeCount <= list.Count)
            {
                list.RemoveRange(0, removeCount);
            }
            // Safety cap
            if (list.Count > _maxPointsPerSeries)
            {
                var extra = list.Count - _maxPointsPerSeries;
                list.RemoveRange(0, extra);
            }
        }
    }
}

public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
}

