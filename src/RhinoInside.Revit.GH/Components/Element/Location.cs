using System;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class GraphicalElementTransform : TransactionalChainComponent
  {
    protected GraphicalElementTransform(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override bool FixUnhandledFailures => false;

    protected bool KeepJoins { get; set; } = false;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      Menu_AppendItem(menu, "Keep Joins", Menu_KeepJoinsClicked, true, KeepJoins);
    }

    private void Menu_KeepJoinsClicked(object sender, EventArgs e)
    {
      if (sender is ToolStripMenuItem)
      {
        RecordUndoEvent($"Set: KeepJoins");
        KeepJoins = !KeepJoins;

        ClearData();
        ExpireDownStreamObjects();
        ExpireSolution(true);
      }
    }
    #endregion

    #region IO
    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      bool keepJoins = false;
      reader.TryGetBoolean("KeepJoins", ref keepJoins);
      KeepJoins = keepJoins;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (KeepJoins != default)
        writer.SetBoolean("KeepJoins", KeepJoins);

      return true;
    }
    #endregion

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      Message = KeepJoins ? "Keep Joins" : string.Empty;
    }
  }

  public class GraphicalElementLocation : GraphicalElementTransform
  {
    public override Guid ComponentGuid => new Guid("A5C63076-679C-4083-ABF2-882A2EEE3016");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public GraphicalElementLocation() : base
    (
      name: "Element Location",
      nickname: "Location",
      description: "Element Get-Set location",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>
      (
        name: "Element",
        nickname: "E",
        description: "Element to access location"
      ),
      ParamDefinition.Create<Param_Plane>
      (
        name: "Location",
        nickname: "L",
        description:  "Element location",
        access: GH_ParamAccess.item,
        optional:  true,
        ParamVisibility.Default
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access location",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Location",
          NickName = "L",
          Description = "Element location",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Vector()
        {
          Name = "Hand",
          NickName = "H",
          Description = "Element hand orientation",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Vector()
        {
          Name = "Facing",
          NickName = "F",
          Description = "Element facing orientation",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Vector()
        {
          Name = "Work Plane",
          NickName = "W",
          Description = "Element work plane orientation",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.GraphicalElement);
      if (!DA.GetData("Element", ref element))
        return;

      if(Params.TryGetData(DA, "Location", out Plane? location) && location.HasValue)
      {
        StartTransaction(element.Document);

        using (!KeepJoins ? element.DisableJoinsScope() : default)
        {
          var pinned = element.Pinned;
          element.Pinned = false;
          element.Location = location.Value;
          element.Pinned = pinned;
        }
      }

      DA.SetData("Element", element);
      DA.SetData("Location", element.Location);
      Params.TrySetData(DA, "Hand", () => element.HandOrientation);
      Params.TrySetData(DA, "Facing", () => element.FacingOrientation);
      Params.TrySetData(DA, "Work Plane", () => element.Location.ZAxis);
    }
  }

  public class GraphicalElementCurve : GraphicalElementTransform
  {
    public override Guid ComponentGuid => new Guid("DCC82ECA-74CD-4B2E-98A2-D655AF06836A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public GraphicalElementCurve() : base
    (
      name: "Element Curve",
      nickname: "Location",
      description: "Element Get-Set curve",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>
      (
        name: "Element",
        nickname: "E",
        description: "Element to access curve"
      ),
      ParamDefinition.Create<Param_Curve>
      (
        name : "Curve",
        nickname: "C",
        description: "Element curve",
        access: GH_ParamAccess.item,
        optional : true,
        ParamVisibility.Default
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>
      (
        name: "Element",
        nickname: "E",
        description: "Element to access curve"
      ),
      ParamDefinition.Create<Param_Curve>
      (
        name : "Curve",
        nickname: "C",
        description: "Element curve"
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.GraphicalElement);
      if (!DA.GetData("Element", ref element))
        return;

      if (Params.TryGetData(DA, "Curve", out Curve curve))
      {
        StartTransaction(element.Document);

        using (!KeepJoins ? element.DisableJoinsScope() : default)
        {
          element.Curve = curve;
        }
      }

      DA.SetData("Element", element);
      DA.SetData("Curve", element.Curve);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-10-19")]
  public class ElementPlacement : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("71F196F1-59E8-4714-B9C2-45FAFEEDF426");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    protected override string IconTag => "⌖";

    public ElementPlacement() : base
    (
      name: "Element Placement",
      nickname: "Placement",
      description: "Queries element placement information",
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
          Description = "Element to extract information",
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
          Name = "Facing Orientation",
          NickName = "F",
          Description = "Element facing orientation",
          Access = GH_ParamAccess.item
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Vector()
        {
          Name = "Hand Orientation",
          NickName = "H",
          Description = "Element hand orientation",
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
        if (box.IsValid)
          DA.SetData(_Box_, box);
      }

      var _Location_ = Params.IndexOfOutputParam("Location");
      if (_Location_ >= 0)
      {
        var location = element.Location;
        if (location.IsValid)
          DA.SetData(_Location_, element.Location);
      }

      var _Orientation_ = Params.IndexOfOutputParam("Facing Orientation");
      if (_Orientation_ >= 0)
      {
        var orientation = element.FacingOrientation;
        if (orientation.IsValid && !orientation.IsZero)
          DA.SetData(_Orientation_, orientation);
      }

      var _Handing_ = Params.IndexOfOutputParam("Hand Orientation");
      if (_Handing_ >= 0)
      {
        var handing = element.HandOrientation;
        if (handing.IsValid && !handing.IsZero)
          DA.SetData(_Handing_, handing);
      }

      var _Curve_ = Params.IndexOfOutputParam("Curve");
      if (_Curve_ >= 0)
        DA.SetData(_Curve_, element.Curve);
    }
  }
}
