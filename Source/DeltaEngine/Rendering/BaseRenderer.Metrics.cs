using System;
using System.Diagnostics;

namespace DeltaEngine.Rendering;
public partial class BaseRenderer
{
    private readonly Stopwatch _waitSync = new();
    private ulong _framesCount;
    private ulong _framesSkip;
    public TimeSpan GetSyncMetric => _waitSync.Elapsed;
    public double SkippedPercent => (double)_framesSkip / _framesCount;

    public virtual void ClearCounters()
    {
        _waitSync.Reset();
        _framesCount = 0;
        _framesSkip = 0;
        foreach (var frame in _frames)
            frame.ClearMetrics();
    }
    public TimeSpan GetAcquireMetric()
    {
        TimeSpan res = TimeSpan.Zero;
        foreach (var frame in _frames)
            res += frame.AcquireMetric;
        return res;
    }
    public TimeSpan GetRecordDrawMetric()
    {
        TimeSpan res = TimeSpan.Zero;
        foreach (var frame in _frames)
            res += frame.RecordMetric;
        return res;
    }
    public TimeSpan GetSubmitDrawMetric()
    {
        TimeSpan res = TimeSpan.Zero;
        foreach (var frame in _frames)
            res += frame.SubmitDrawMetric;
        return res;
    }
    public TimeSpan GetSubmitPresentMetric()
    {
        TimeSpan res = TimeSpan.Zero;
        foreach (var frame in _frames)
            res += frame.SubmitPresentMetric;
        return res;
    }
}
