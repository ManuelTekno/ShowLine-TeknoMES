using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTOptix.HMIProject;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Core;

public interface IPLCTagService
{
    public void WriteSingleTag(string tagFullPath, object value);
    public object ReadSingleTag(string tagName);
    public IUAVariable GetVariable(string relativePath);
    public void ClearAllTags();



    // Methods to simplify parsing
    int? ReadIntTag(string tagName);
    string ReadStringTag(string tagName);
    float? ReadFloatTag(string tagName);
    bool? ReadBoolTag(string tagName);
    public void WriteMultipleTags(List<RemoteChildVariableValue> tagList);

}
