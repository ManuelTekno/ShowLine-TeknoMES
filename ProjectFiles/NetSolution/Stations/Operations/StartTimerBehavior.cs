using NETCode.Core;
using NETCode.Entities;
using NETCode.Stations;

namespace NETCode.Stations.Operations
{
    public class StartTimerBehavior : IOperationBehavior
    {
        private const string REMAINING_TAG = "From/RemainingTime";
        private const double DEFAULT_SECONDS = 1.0;

        public OperationResult Execute(Station_Base context, Operation operation)
        {
            // 1) Get the configured time (ValueReal) or use the default
            double seconds = operation.ValueReal.HasValue ? operation.ValueReal.Value : DEFAULT_SECONDS;

            // 2) If the time is invalid or zero, pass immediately
            if (seconds <= 0)
            {
                context.WriteSingleTag(REMAINING_TAG, 0);
                return OperationResult.Passed;
            }

            // 3) Use Station_Base internal countdown logic
            //    - On the first call, it initializes and writes the rounded value
            //    - On subsequent calls, it decrements by one each cycle
            bool completed = context.StartCountdown(seconds);

            // 4) When finished, Station_Base already writes 0 and resets its state
            return completed ? OperationResult.Passed : OperationResult.Waiting;
        }
    }
}
