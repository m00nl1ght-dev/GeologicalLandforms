using System;
using System.Xml;
using Verse;

namespace GeologicalLandforms;

// ReSharper disable InconsistentNaming
[Serializable]
public class XmlValueModifier
{
    public float value;

    public Operation operation = Operation.Replace;

    public void Apply(ref float val)
    {
        val = operation switch
        {
            Operation.Add => val + value,
            Operation.Replace => value,
            Operation.Multiply => val * value,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        value = ParseHelper.ParseFloat(xmlRoot.FirstChild.Value);
        operation = Enum.TryParse(xmlRoot.Attributes?["operation"]?.Value?.CapitalizeFirst(), out Operation m) ? m : Operation.Replace;
    }

    public enum Operation
    {
        Add,
        Replace,
        Multiply
    }
}
