using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Core;

public enum StationCommand
{
    Initialize = 1,
    RegisterUnit = 2,
    CheckUnit = 3,
    LoadOperations = 4,
    ExecuteOperations = 5,
    SaveUnitResults = 6,
    ArchiveUnit = 7,
    MarkUnitAsRework = 8
   }
