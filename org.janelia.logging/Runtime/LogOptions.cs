using System;
using UnityEngine;

namespace Janelia
{
    public class LogOptions : MonoBehaviour
    {
        public bool EnableLogging = true;

        public bool LogTotalMemory = false;

        private void Update()
        {
            if (LogTotalMemory)
            {
                _totalMemoryLog.totalMemory = GC.GetTotalMemory(false);
                Logger.Log(_totalMemoryLog);
            }
        }

        [Serializable]
        private struct TotalMemoryLog
        {
            public long totalMemory;
        };
        static private TotalMemoryLog _totalMemoryLog = new TotalMemoryLog();
    }
}
