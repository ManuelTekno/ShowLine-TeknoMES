using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Stations.Operations
{
    public static class OperationBehaviors
    {
        public const int NoAction = 0;
        public const int WaitComplete = 1;
        public const int ValidateValue = 2;
        public const int SendCommand= 3;
        public const int DisplayOnly = 4;
        public const int UserConfirm = 5;
        public const int StartTimer = 6;
        public const int CheckContainsValue = 7;
        public const int CaptureResult = 8;
        public const int SendListAndWaitComplete = 9;

    }
}
