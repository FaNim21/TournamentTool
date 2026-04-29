using ObsWebSocket.Core.Protocol;
using ObsWebSocket.Core.Protocol.Requests;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.Obs;

public class ObsUpdateBatcher : IObsUpdateBatcher
{
    private const int _UPDATE_DEBOUNCE_TIME = 500;
    
    private readonly IObsController _obsController;
    private readonly ILoggingService _logger;

    private Lock _lock = new();
    private Queue<BatchRequestItem> _pendingUpdates = [];
    
    private TimeSpan _updateTime;
    private bool _flushScheduled;

    
    public ObsUpdateBatcher(IObsController obsController, ILoggingService logger)
    {
        _obsController = obsController;
        _logger = logger;

        _updateTime = TimeSpan.FromMilliseconds(_UPDATE_DEBOUNCE_TIME);
    }
    
    public void Queue(SetInputSettingsRequestData requestData)
    {
        lock (_lock)
        {
            _pendingUpdates.Enqueue(new BatchRequestItem("SetInputSettings", requestData));

            if (_flushScheduled) return;

            _flushScheduled = true;
            _ = ScheduleFlush();
        }
    }

    private async Task ScheduleFlush()
    {
        await Task.Delay(_updateTime);

        List<BatchRequestItem> batch = [];

        lock (_lock)
        {
            int count = _pendingUpdates.Count;
            for (int i = 0; i < count; i++)
            {
                batch.Add(_pendingUpdates.Dequeue());
            }
            
            _flushScheduled = false;
        }

        try
        {
            await _obsController.CallBatchAsync(batch);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
}