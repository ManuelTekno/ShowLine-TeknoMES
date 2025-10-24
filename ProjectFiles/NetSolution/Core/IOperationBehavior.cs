using NETCode.Entities;
using NETCode.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Core;

public interface IOperationBehavior
{
    OperationResult Execute(Station_Base context, Operation operation);
}
