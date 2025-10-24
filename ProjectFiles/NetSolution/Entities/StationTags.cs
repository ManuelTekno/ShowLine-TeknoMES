using System;

namespace NETCode.Entities
{
    public class FromPLC
    {
        public sbyte Destination { get; set; }
        public short Response { get; set; }
        public short Step_Index { get; set; }
        public OperationFromPLC[] Operations { get; set; } = new OperationFromPLC[5];

        public FromPLC()
        {
            for (int i = 0; i < Operations.Length; i++)
                Operations[i] = new OperationFromPLC();
        }
    }

    public class OperationFromPLC
    {
        public bool Enable { get; set; }
        public short Type { get; set; }
        public short Behavior { get; set; }
        public string Value_STRING { get; set; }
        public float Value_REAL { get; set; }
    }

    public class ToPLC
    {
        public string ValidationCode { get; set; }
        public string PalletID { get; set; }
        public sbyte Command { get; set; }
        public ResultsToPLC[] Results { get; set; } = new ResultsToPLC[5];

        public OperationFromPLC CurrentOperation { get; set; } = new OperationFromPLC();

        public ToPLC()
        {
            for (int i = 0; i < Results.Length; i++)
                Results[i] = new ResultsToPLC();
        }

        public float CycleTIme { get; set; }
    }

    public class ResultsToPLC
    {
        public bool Complete { get; set; }
        public string Result_STRING { get; set; }
        public float Result_REAL { get; set; }
    }

    public class PLCTags
    {
        public FromPLC From { get; set; } = new FromPLC();
        public ToPLC To { get; set; } = new ToPLC();
    }
}
