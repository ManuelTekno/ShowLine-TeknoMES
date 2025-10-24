using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Entities;

public class Recipe
{
    public int ID { get; set; }
    public int StationId { get; set; }
    public int VariantId { get; set; }
    public string Name { get; set; }
}
