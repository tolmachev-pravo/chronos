using System;

namespace Pet.Jira.Application.Tracing
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

            _syncRoot = new object();
        }

        public string Category { get; }
        public TimeSpan Sum { get; private set; }
        public TimeSpan Max { get; private set; }
        public TimeSpan Min { get; private set; }
        public int Count { get; private set; }

        public TimeSpan Average => Count == 0 ? TimeSpan.Zero : new(Sum.Ticks / Count);

        public void Update(TimeSpan elapsed)
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
            }
        }
    }
}
