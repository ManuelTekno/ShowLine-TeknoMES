using System;

namespace NETCode.Entities;

public class PickToLightSchedule
{
    public int Id { get; set; }                 // pk
    public int SequenceNo { get; set; }         // orden de ciclo
    public string PayloadCsv { get; set; }      // "1,2" o "PN123,PN456"
    public string Status { get; set; }          // 'pending' | 'completed' (ENUM en MySQL)
    public DateTime UpdatedAt { get; set; }     // timestamp
}
