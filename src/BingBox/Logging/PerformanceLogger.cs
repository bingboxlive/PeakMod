using System;
using UnityEngine;

namespace BingBox.Logging;

internal static class PerformanceLogger
{
    private static float _lastLogTime;

    public static void Update()
    {
        if (Time.deltaTime > 0.055f)
        {
            if (Time.time - _lastLogTime < 1.0f) return;
            _lastLogTime = Time.time;

            var ms = Time.deltaTime * 1000f;
            var mem = GC.GetTotalMemory(false) / 1024;
            var g0 = GC.CollectionCount(0);
            if (Plugin.DebugConfig.Value)
            {
                Plugin.Log.LogWarning($"FPS DROP! ({ms:F1}ms) Mem: {mem}KB, G0: {g0}");
            }
        }
    }
}
