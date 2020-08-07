using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  public class GraphicalElementLocation : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("71F196F1-59E8-4714-B9C2-45FAFEEDF426");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "⌖";

    public GraphicalElementLocation() : base
    (
      name: "Graphical Element Location",
      nickname: "Location",
      description: "Queries element location information",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to extract location",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Element reference level",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Box()
        {
          Name = "Box",
          NickName = "B",
          Description = "Element oriented bounding box",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Location",
          NickName = "L",
          Description = "Element location",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Vector()
        {
          Name = "Orientation",
          NickName = "O",
          Description = "Element orientation direction",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Vector()
        {
          Name = "Handing",
          NickName = "H",
          Description = "Element handing direction",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "C",
          Description = "Element curve location",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.GraphicalElement element = null;
      if (!DA.GetData("Element", ref element))
        return;

      var _Level_ = Params.IndexOfOutputParam("Level");
      if (_Level_ >= 0)
        DA.SetData(_Level_, element.Level);

      var _Box_ = Params.IndexOfOutputParam("Box");
      if (_Box_ >= 0)
      {
        var box = element.Box;
        if(box.IsValid)
          DA.SetData(_Box_, box);
      }

      var _Location_ = Params.IndexOfOutputParam("Location");
      if (_Location_ >= 0)
      {
        var location = element.Location;
        if(location.IsValid)
          DA.SetData(_Location_, element.Location);
      }

      var _Orientation_ = Params.IndexOfOutputParam("Orientation");
      if (_Orientation_ >= 0)
      {
        var orientation = element.Orientation;
        if (orientation.IsValid && !orientation.IsZero)
          DA.SetData(_Orientation_, orientation);
      }

      var _Handing_ = Params.IndexOfOutputParam("Handing");
      if (_Handing_ >= 0)
      {
        var handing = element.Orientation;
        if (handing.IsValid && !handing.IsZero)
          DA.SetData(_Handing_, handing);
      }

      var _Curve_ = Params.IndexOfOutputParam("Curve");
      if (_Curve_ >= 0)
        DA.SetData(_Curve_, element.Curve);
    }
  }
}
