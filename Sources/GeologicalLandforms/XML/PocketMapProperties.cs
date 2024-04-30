using System.Collections.Generic;
using LunarFramework.XML;
using Verse;

namespace GeologicalLandforms;

public class PocketMapProperties : DefModExtension
{
    public XmlDynamicValue<List<string>, ICtxTile> landforms;
    public XmlDynamicValue<List<string>, ICtxTile> biomeVariants;
}
