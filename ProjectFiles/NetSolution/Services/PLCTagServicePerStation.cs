using FTOptix.HMIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UAManagedCore;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using NETCode.Core;
using NETCode.Entities;

namespace NETCode.Services;

public class PlcTagServicePerStation : IPLCTagService
{
    private readonly string _basePath;
    private readonly string _stationTag;
    public PLCTags Tags { get; private set; }

    public PlcTagServicePerStation(string basePath, string stationTag)
    {
        _basePath = basePath;
        _stationTag = stationTag;
        Tags = new PLCTags();
    }

    private string GetFullPath() => $"{_basePath}/{_stationTag}";

    private IUANode TryGetPlcNode()
    {
        IUANode plcNode = null;
        int retry = 0;
        string fullPath = GetFullPath();

        while (plcNode == null && retry < 5)
        {
            plcNode = Project.Current.Get(fullPath);
            if (plcNode == null)
            {
                Log.Warning($"Retry {retry + 1}: PLC Node still NULL for station {_stationTag} at {fullPath}...");
                Thread.Sleep(250);
                retry++;
            }
        }
        return plcNode;
    }

    public object ReadSingleTag(string tagRelativePath)
    {
        var plcNode = TryGetPlcNode();
        if (plcNode == null)
        {
            Log.Error($"PLC Node is NULL for {_stationTag}. Path: {GetFullPath()}");
            return null;
        }

        try
        {
            var reads = plcNode.ChildrenRemoteRead(new List<RemoteChildVariable>
            {
                new RemoteChildVariable(tagRelativePath)
            });

            var read = reads.FirstOrDefault();
            return read.Value is UAValue uaValue ? uaValue.Value : read.Value;
        }
        catch (Exception ex)
        {
            Log.Error($"ReadSingleTag failed for {_stationTag}, tag: {tagRelativePath}: {ex}");
            return null;
        }
    }

    public void WriteSingleTag(string tagRelativePath, object value)
    {
        var plcNode = TryGetPlcNode();
        if (plcNode == null)
        {
            Log.Error($"PLC Node is NULL for {_stationTag}. Path: {GetFullPath()}");
            return;
        }

        try
        {
            plcNode.ChildrenRemoteWrite(new List<RemoteChildVariableValue>
            {
                new RemoteChildVariableValue(tagRelativePath, new UAValue(value))
            });
        }
        catch (Exception ex)
        {
            Log.Error($"WriteSingleTag failed for {_stationTag}, tag: {tagRelativePath}: {ex}");
        }
    }

    public IUAVariable GetVariable(string relativePath)
    {
        var fullPath = $"{_basePath}/{_stationTag}/{relativePath}";
        return Project.Current.Get(fullPath) as IUAVariable;
    }

    public int? ReadIntTag(string tagRelativePath)
    {
        var value = ReadSingleTag(tagRelativePath);
        try { return value != null ? Convert.ToInt32(value) : null; }
        catch { Log.Error($"[{_stationTag}] Error converting '{tagRelativePath}' to int"); return null; }
    }

    public string ReadStringTag(string tagRelativePath)
    {
        var value = ReadSingleTag(tagRelativePath);
        return value?.ToString();
    }

    public float? ReadFloatTag(string tagRelativePath)
    {
        var value = ReadSingleTag(tagRelativePath);
        try { return value != null ? Convert.ToSingle(value) : null; }
        catch { Log.Error($"[{_stationTag}] Error converting '{tagRelativePath}' to float"); return null; }
    }

    public bool? ReadBoolTag(string tagRelativePath)
    {
        var value = ReadSingleTag(tagRelativePath);
        try { return value != null ? Convert.ToBoolean(value) : null; }
        catch { Log.Error($"[{_stationTag}] Error converting '{tagRelativePath}' to bool"); return null; }
    }
    public void ClearAllTags()
    {
        var plcNode = TryGetPlcNode();
        if (plcNode == null)
        {
            Log.Error($"PLC Node is NULL for ClearAllTags on station {_stationTag}.");
            return;
        }

        try
        {
            var tagsToClear = new List<RemoteChildVariableValue>
        {
            new RemoteChildVariableValue("From/Destination", new UAValue((sbyte)0)),
            new RemoteChildVariableValue("From/Response", new UAValue((short)0)),
            new RemoteChildVariableValue("From/Operation_Index", new UAValue((short)0))
        };

            for (int i = 0; i < 10; i++)
            {
                string prefix = $"From/Operations/{i}/";
                tagsToClear.Add(new RemoteChildVariableValue(prefix + "Enable", new UAValue(false)));
                tagsToClear.Add(new RemoteChildVariableValue(prefix + "Type", new UAValue((short)0)));
                tagsToClear.Add(new RemoteChildVariableValue(prefix + "Behavior", new UAValue((short)0)));
                tagsToClear.Add(new RemoteChildVariableValue(prefix + "Value_STRING", new UAValue("")));
                tagsToClear.Add(new RemoteChildVariableValue(prefix + "Value_REAL", new UAValue(0.0f)));
            }

            plcNode.ChildrenRemoteWrite(tagsToClear);
            Log.Info($"[{_stationTag}] All 'From' tags and Operations cleared successfully.");
        }
        catch (Exception ex)
        {
            Log.Error($"[{_stationTag}] ClearAllTags failed: {ex.Message}");
        }
    }
    public void WriteMultipleTags(List<RemoteChildVariableValue> tagList)
    {
        var plcNode = TryGetPlcNode();
        if (plcNode == null)
        {
            Log.Error($"PLC Node is NULL for WriteMultipleTags on station {_stationTag}. Path: {GetFullPath()}");
            return;
        }

        try
        {
            plcNode.ChildrenRemoteWrite(tagList);
            Log.Info($"[{_stationTag}] Successfully wrote {tagList.Count} tags to PLC.");
        }
        catch (Exception ex)
        {
            Log.Error($"WriteMultipleTags failed for {_stationTag}: {ex}");
        }
    }


}
