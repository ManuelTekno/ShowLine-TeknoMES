using System;

namespace NETCode.Entities
{
    public class PickToLightBin
    {
        public int Id { get; set; }                  // Primary key
        public int BinPosition { get; set; }         // Physical bin index (0..N)
        public string BinLabel { get; set; }         // Optional label for UI (e.g., "A1", "B3")
        public string PartNumber { get; set; }       // Part number assigned to the bin
        public bool Active { get; set; }             // True = enabled, False = inactive
        public DateTime LastUpdated { get; set; }    // Timestamp of last update

        // Default constructor
        public PickToLightBin()
        {
            Active = true;
            LastUpdated = DateTime.Now;
        }

        // Optional helper method for debugging or logs
        public override string ToString()
        {
            return $"Bin {BinPosition} - Part: {PartNumber} (Active: {Active})";
        }
    }
}
