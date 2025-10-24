using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Entities;

public class Operation
{
    public int ID { get; set; }
    public int RecipeID { get; set; }
    public int OperationTypeID { get; set; }
    public int BehaviorID { get; set; }
    public int Index { get; set; }
    public string ValueString { get; set; }
    public float? ValueReal { get; set; }
    public string Description { get; set; }
}
