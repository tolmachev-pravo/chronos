using System;

namespace Chronos.Application.Tracing
{
    public class Measure
    {
        private readonly object _syncRoot;

        public Measure(string category)
        {
            Category = category;
            Sum = TimeSpan.Zero;
            Max = TimeSpan.Zero;
            Min = TimeSpan.MaxValue;
            Count = 0;
            AllocatedSum = 0;
            AllocatedMax = 0;
            AllocatedMin = long.MaxValue;

            _syncRoot = new object();
        }

        public string Category { get; }
        public TimeSpan Sum { get; private set; }
        public TimeSpan Max { get; private set; }
        public TimeSpan Min { get; private set; }
        public int Count { get; private set; }

        public TimeSpan Average => Count == 0 ? TimeSpan.Zero : new(Sum.Ticks / Count);

        /// <summary>Total bytes allocated across all recorded calls (GC allocation throughput, not retained memory).</summary>
        public long AllocatedSum { get; private set; }
        public long AllocatedMax { get; private set; }
        public long AllocatedMin { get; private set; }

        public long AllocatedAverage => Count == 0 ? 0 : AllocatedSum / Count;

        public void Update(TimeSpan elapsed, long allocatedBytes)
        {
            lock (_syncRoot)
            {
                Count++;

                Sum += elapsed;

                if (elapsed > Max)
                {
                    Max = elapsed;
                }

                if (elapsed < Min)
                {
                    Min = elapsed;
                }

                AllocatedSum += allocatedBytes;

                if (allocatedBytes > AllocatedMax)
                {
                    AllocatedMax = allocatedBytes;
                }

                if (allocatedBytes < AllocatedMin)
                {
                    AllocatedMin = allocatedBytes;
                }
            }
        }
    }
}
