using System;
using NodeEditorFramework;
using NodeEditorFramework.Utilities;
using UnityEngine;

namespace TerrainGraph;

[Serializable]
[Node(false, "Grid/Linear Function")]
public class NodeGridLinear : NodeBase
{
    public const string ID = "gridLinear";
    public override string GetID => ID;

    public override string Title => Circular ? "Circular Function" : "Linear Function";
    
    [NonSerialized]
    public ValueConnectionKnob BiasKnob;
    
    [NonSerialized]
    public ValueConnectionKnob OriginXKnob;
    
    [NonSerialized]
    public ValueConnectionKnob OriginZKnob;
    
    [NonSerialized]
    public ValueConnectionKnob SpanPxKnob;
    
    [NonSerialized]
    public ValueConnectionKnob SpanNxKnob;
    
    [NonSerialized]
    public ValueConnectionKnob SpanPzKnob;
    
    [NonSerialized]
    public ValueConnectionKnob SpanNzKnob;
    
    private static readonly ValueConnectionKnobAttribute BiasKnobAttribute = new("Bias", Direction.In, ValueFunctionConnection.Id);
    private static readonly ValueConnectionKnobAttribute OriginXKnobAttribute = new("Origin x", Direction.In, ValueFunctionConnection.Id);
    private static readonly ValueConnectionKnobAttribute OriginZKnobAttribute = new("Origin z", Direction.In, ValueFunctionConnection.Id);
    private static readonly ValueConnectionKnobAttribute SpanPxKnobAttribute = new("Span px", Direction.In, ValueFunctionConnection.Id);
    private static readonly ValueConnectionKnobAttribute SpanNxKnobAttribute = new("Span nx", Direction.In, ValueFunctionConnection.Id);
    private static readonly ValueConnectionKnobAttribute SpanPzKnobAttribute = new("Span pz", Direction.In, ValueFunctionConnection.Id);
    private static readonly ValueConnectionKnobAttribute SpanNzKnobAttribute = new("Span nz", Direction.In, ValueFunctionConnection.Id);

    [ValueConnectionKnob("Output", Direction.Out, GridFunctionConnection.Id)]
    public ValueConnectionKnob OutputKnob;
    
    public double Bias;
    public double OriginX;
    public double OriginZ;
    public double SpanPx;
    public double SpanNx;
    public double SpanPz;
    public double SpanNz;
    
    public bool Circular;
    
    public override void RefreshDynamicKnobs()
    {
        BiasKnob = FindDynamicKnob(BiasKnobAttribute);
        OriginXKnob = FindDynamicKnob(OriginXKnobAttribute);
        OriginZKnob = FindDynamicKnob(OriginZKnobAttribute);
        SpanPxKnob = FindDynamicKnob(SpanPxKnobAttribute);
        SpanNxKnob = FindDynamicKnob(SpanNxKnobAttribute);
        SpanPzKnob = FindDynamicKnob(SpanPzKnobAttribute);
        SpanNzKnob = FindDynamicKnob(SpanNzKnobAttribute);
    }

    public override void OnCreate(bool fromGUI)
    {
        base.OnCreate(fromGUI);
        if (fromGUI)
        {
            BiasKnob ??= (ValueConnectionKnob) CreateConnectionKnob(BiasKnobAttribute);
            OriginXKnob ??= (ValueConnectionKnob) CreateConnectionKnob(OriginXKnobAttribute);
            SpanPxKnob ??= (ValueConnectionKnob) CreateConnectionKnob(SpanPxKnobAttribute);
        }
    }

    public override void NodeGUI()
    {
        OutputKnob.SetPosition(FirstKnobPosition);
        
        GUILayout.BeginVertical(BoxStyle);
        
        if (BiasKnob != null) KnobValueField(BiasKnob, ref Bias);
        if (OriginXKnob != null) KnobValueField(OriginXKnob, ref OriginX, KnobName("Origin", "x", ""));
        if (OriginZKnob != null) KnobValueField(OriginZKnob, ref OriginZ, KnobName("Origin", "z", ""));
        if (SpanPxKnob != null) KnobValueField(SpanPxKnob, ref SpanPx, KnobName("Span", "x", "p"));
        if (SpanNxKnob != null) KnobValueField(SpanNxKnob, ref SpanNx, KnobName("Span", "x", "n"));
        if (SpanPzKnob != null) KnobValueField(SpanPzKnob, ref SpanPz, KnobName("Span", "z", "p"));
        if (SpanNzKnob != null) KnobValueField(SpanNzKnob, ref SpanNz, KnobName("Span", "z", "n"));
        
        GUILayout.EndVertical();

        if (GUI.changed)
            canvas.OnNodeChange(this);
    }

    private string KnobName(string n, string d, string p)
    {
        n += " ";
        if (SpanNxKnob != null) n += p;
        if (OriginZKnob != null) n += d;
        return n.Trim();
    }

    public override void RefreshPreview()
    {
        RefreshIfConnected(BiasKnob, ref Bias);
        RefreshIfConnected(OriginXKnob, ref OriginX);
        RefreshIfConnected(OriginZKnob, ref OriginZ);
        RefreshIfConnected(SpanPxKnob, ref SpanPx);
        RefreshIfConnected(SpanNxKnob, ref SpanNx);
        RefreshIfConnected(SpanPzKnob, ref SpanPz);
        RefreshIfConnected(SpanNzKnob, ref SpanNz);
    }
    
    public override void FillNodeActionsMenu(NodeEditorInputInfo inputInfo, GenericMenu menu)
    {
        base.FillNodeActionsMenu(inputInfo, menu);
        menu.AddSeparator("");

        if (OriginZKnob != null)
        {
            menu.AddItem(new GUIContent ("Switch to 1d"), false, () =>
            {
                DeleteConnectionPort(OriginZKnob);
                DeleteConnectionPort(SpanPzKnob);
                DeleteConnectionPort(SpanNzKnob);
                RefreshDynamicKnobs();
                OriginZ = 0f;
                SpanPz = 0f;
                SpanNz = 0f;
                canvas.OnNodeChange(this);
            });
        }
        else
        {
            menu.AddItem(new GUIContent ("Switch to 2d"), false, () =>
            {
                OriginZKnob ??= (ValueConnectionKnob) CreateConnectionKnob(OriginZKnobAttribute);
                SpanPzKnob ??= (ValueConnectionKnob) CreateConnectionKnob(SpanPzKnobAttribute);
                if (SpanNxKnob != null) SpanNzKnob ??= (ValueConnectionKnob) CreateConnectionKnob(SpanNzKnobAttribute);
                canvas.OnNodeChange(this);
            });
        }
        
        if (SpanNxKnob != null)
        {
            menu.AddItem(new GUIContent ("Switch to symmetric"), false, () =>
            {
                DeleteConnectionPort(SpanNxKnob);
                DeleteConnectionPort(SpanNzKnob);
                RefreshDynamicKnobs();
                SpanNx = SpanPx;
                SpanNz = SpanPz;
                canvas.OnNodeChange(this);
            });
        }
        else
        {
            menu.AddItem(new GUIContent ("Switch to asymmetric"), false, () =>
            {
                SpanNxKnob ??= (ValueConnectionKnob) CreateConnectionKnob(SpanNxKnobAttribute);
                if (OriginZKnob != null) SpanNzKnob ??= (ValueConnectionKnob) CreateConnectionKnob(SpanNzKnobAttribute);
                SpanNx = SpanPx;
                SpanNz = SpanPz;
                canvas.OnNodeChange(this);
            });
        }
        
        menu.AddSeparator("");
        
        if (Circular)
        {
            menu.AddItem(new GUIContent ("Switch to linear"), false, () =>
            {
                Circular = false;
                canvas.OnNodeChange(this);
            });
        }
        else
        {
            menu.AddItem(new GUIContent ("Switch to circular"), false, () =>
            {
                Circular = true;
                canvas.OnNodeChange(this);
            });
        }
    }

    public override bool Calculate()
    {
        OutputKnob.SetValue<ISupplier<IGridFunction<double>>>(new Output(
            SupplierOrValueFixed(BiasKnob, Bias),
            SupplierOrValueFixed(OriginXKnob, OriginX),
            OriginZKnob == null ? Supplier.Zero : SupplierOrValueFixed(OriginZKnob, OriginZ),
            SupplierOrValueFixed(SpanPxKnob, SpanPx),
            SpanNxKnob == null ? SupplierOrValueFixed(SpanPxKnob, SpanPx) : SupplierOrValueFixed(SpanNxKnob, SpanNx),
            SpanPzKnob == null ? Supplier.Zero : SupplierOrValueFixed(SpanPzKnob, SpanPz),
            SpanNzKnob == null ? SpanPzKnob == null ? Supplier.Zero : SupplierOrValueFixed(SpanPzKnob, SpanPz) : SupplierOrValueFixed(SpanNzKnob, SpanNz),
            Circular, GridSize
        ));
        return true;
    }
    
    private class Output : ISupplier<IGridFunction<double>>
    {
        private readonly ISupplier<double> _bias;
        private readonly ISupplier<double> _originX;
        private readonly ISupplier<double> _originZ;
        private readonly ISupplier<double> _spanPx;
        private readonly ISupplier<double> _spanNx;
        private readonly ISupplier<double> _spanPz;
        private readonly ISupplier<double> _spanNz;
        private readonly bool _circular;
        private readonly double _gridSize;

        public Output(ISupplier<double> bias, ISupplier<double> originX, ISupplier<double> originZ, ISupplier<double> spanPx, 
            ISupplier<double> spanNx, ISupplier<double> spanPz, ISupplier<double> spanNz, bool circular, double gridSize)
        {
            _bias = bias;
            _originX = originX;
            _originZ = originZ;
            _spanPx = spanPx;
            _spanNx = spanNx;
            _spanPz = spanPz;
            _spanNz = spanNz;
            _circular = circular;
            _gridSize = gridSize;
        }

        public IGridFunction<double> Get()
        {
            return new GridFunction.SpanFunction(
                _bias.Get(), _originX.Get() * _gridSize, _originZ.Get() * _gridSize, _spanPx.Get(), _spanNx.Get(), _spanPz.Get(), _spanNz.Get(), _circular
            );
        }

        public void ResetState()
        {
            _bias.ResetState();
            _originX.ResetState();
            _originZ.ResetState();
            _spanPx.ResetState();
            _spanNx.ResetState();
            _spanPz.ResetState();
            _spanNz.ResetState();
        }
    }
}