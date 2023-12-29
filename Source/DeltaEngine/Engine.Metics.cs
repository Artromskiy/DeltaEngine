using System;

namespace Delta;

public partial class Engine
{

    public TimeSpan GetUpdateRendererMetric => _renderer.GetUpdateMetric;
    public TimeSpan GetCopyRendererMetric => _renderer.GetCopyMetric;
    public TimeSpan GetCopySetupRendererMetric => _renderer.GetCopySetupMetric;
    public TimeSpan GetSyncRendererMetric => _renderer.GetSyncMetric;
    public TimeSpan GetAcquireFrameRendererMetric => _renderer.GetAcquireMetric();
    public TimeSpan GetRecordDrawRenderMetric => _renderer.GetRecordDrawMetric();
    public TimeSpan GetSubmitDrawRenderMetric => _renderer.GetSubmitDrawMetric();
    public TimeSpan GetSubmitPresentRenderMetric => _renderer.GetSubmitPresentMetric();
    public TimeSpan GetSceneMetric => _scene.GetSceneMetric;
    public TimeSpan GetSceneTrsWriteMetric => _scene.TrsWriteMetric;
    public TimeSpan GetSceneJobSetupMetric => _scene.JobSetupMetric;
    public TimeSpan GetSceneJobWaitMetric => _scene.JobWaitMetric;
    public double GetRenderSkipPercent => _renderer.SkippedPercent;

    public void ClearRendererMetrics()
    {
        _renderer.ClearCounters();
        _scene.ClearSceneMetric();
    }
}