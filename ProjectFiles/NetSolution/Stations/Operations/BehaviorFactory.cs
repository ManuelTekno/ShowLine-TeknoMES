using NETCode.Core;
using NETCode.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Stations.Operations;

public static class BehaviorFactory
{
    public static IOperationBehavior GetBehavior(int behaviorId)
    {
        return behaviorId switch
        {
            1 => new WaitForCompleteBehavior(),
            2 => new ValidateValueBehavior(),
            3 => new SendCommandBehavior(),
            4 => new DisplayOnlyBehavior(),
            5 => new UserConfirmBehavior(),
            6 => new StartTimerBehavior(),
            7 => new CheckContainsValueBehavior(),
            8 => new CaptureResultBehavior(),
            9 => new SendListAndWaitOkBehavior(),         // <-- NEW
            _ => throw new ArgumentException($"Unrecognized behavior type: {behaviorId}")
        };
    }
}
