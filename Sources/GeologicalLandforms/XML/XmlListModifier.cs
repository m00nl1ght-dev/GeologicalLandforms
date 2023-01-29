using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace GeologicalLandforms;

// ReSharper disable InconsistentNaming

[Serializable]
public class XmlListModifier<T>
{
    public List<T> entries;
    
    public Operation operation = Operation.Add;
    
    public void Apply(List<T> val, Func<T, object> keyFunc = null)
    {
        switch (operation)
        {
            case Operation.Add:
                foreach (var entry in entries)
                {
                    if (keyFunc != null) val.RemoveAll(e => keyFunc(e) == keyFunc(entry));
                    val.Add(entry);
                }
                break;
            case Operation.Replace:
                val.Clear();
                val.AddRange(entries);
                break;
        }
    }
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        entries = DirectXmlToObject.ObjectFromXml<List<T>>(xmlRoot, false);
        operation = Enum.TryParse(xmlRoot.Attributes?["operation"]?.Value?.CapitalizeFirst(), out Operation m) ? m : Operation.Add;
    }

    public enum Operation
    {
        Add, Replace
    }
}