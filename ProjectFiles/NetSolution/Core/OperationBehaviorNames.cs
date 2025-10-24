using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Core;

public enum OperationBehaviorNames
{
    WaitComplete = 1,
    ValidateValue = 2,
    SendCommand = 3,
    DisplayOnly = 4,
    UserConfirm = 5,
    StartTimer = 6,
    CheckContainsValue = 7,
    CaptureResult = 8,
    SendListAndWaitComplete = 9
}
