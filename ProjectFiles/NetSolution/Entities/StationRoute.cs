using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCode.Entities;
public class StationRoute
{
    public int Id { get; set; }
    public int StationId { get; set; }            // FK to stations.id
    public string Quality { get; set; }           // 'Rework' | 'Pass' | 'Any'
    public sbyte Destination { get; set; }        // 1=Forward, 2=Left, 3=Right
    public int Priority { get; set; } = 100;      // lower wins
    public bool Enabled { get; set; } = true;     // maps TINYINT (0/1)
}
