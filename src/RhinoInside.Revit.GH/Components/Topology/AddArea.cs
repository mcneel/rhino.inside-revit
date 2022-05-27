using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.7")]
  public class AddArea : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("2EE360F3-A023-44C0-8922-1890B7629B4A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddArea() : base
    (
      name: "Add Area",
      nickname: "AddArea",
      description: "Given an internal point, it adds an Area to the given Area Plan",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AreaPlan()
        {
          Name = "Area Plan",
          NickName = "AP",
          Description = "Area Plan to add a specific area",
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Location",
          NickName = "L",
          Description = $"{_Area_} location point",
        }
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Number",
          NickName = "NUM",
          Description = $"{_Area_} number"
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = $"{_Area_} name",
          Optional = true
        }, ParamRelevance.Tertiary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.SpatialElement()
        {
          Name = _Area_,
          NickName = _Area_.Substring(0, 1),
          Description = $"Output {_Area_}",
        }
      )
    };

    const string _Area_ = "Area";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ROOM_NUMBER
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Area Plan", out ARDB.ViewPlan viewPlan)) return;

      ReconstructElement<ARDB.Area>
      (
        viewPlan.Document, _Area_, (area) =>
        {
          // Input
          if (!Params.GetData(DA, "Location", out Point3d? location)) return null;
          if (!Params.TryGetData(DA, "Number", out string number)) return null;
          if (!Params.TryGetData(DA, "Name", out string name)) return null;

          // Snap Location to the 'Level' 'Elevation'
          location = new Point3d
          (
            location.Value.X,
            location.Value.Y,
            viewPlan.GenLevel.ProjectElevation * Revit.ModelUnits
          );

          // Compute
          if (CanReconstruct(_Area_, out var untracked, ref area, viewPlan.Document, number, categoryId: ARDB.BuiltInCategory.OST_Areas))
            area = Reconstruct(viewPlan, area, location.Value.ToXYZ(), number, name);

          DA.SetData(_Area_, area);
          return untracked ? null : area;
        }
      );
    }

    bool Reuse(ARDB.Area area, ARDB.ViewPlan viewPlan, ARDB.XYZ location)
    {
      if (area is null) return false;
      if (!area.AreaScheme.IsEquivalent(viewPlan.AreaScheme)) return false;
      if (area.Location is ARDB.LocationPoint areaLocation)
      {
        var target = new ARDB.XYZ(location.X, location.Y, areaLocation.Point.Z);
        var position = areaLocation.Point;
        if (!target.IsAlmostEqualTo(position))
        {
          var pinned = area.Pinned;
          area.Pinned = false;
          areaLocation.Move(target - position);
          area.Pinned = pinned;
        }
      }

      return true;
    }

    ARDB.Area Create(ARDB.ViewPlan viewPlan, ARDB.XYZ location)
    {
      return viewPlan.Document.Create.NewArea(viewPlan, new ARDB.UV(location.X, location.Y));
    }

    ARDB.Area Reconstruct
    (
      ARDB.ViewPlan viewPlan,
      ARDB.Area area, ARDB.XYZ location,
      string number, string name
    )
    {
      var isNew = area is null;
      if (!Reuse(area, viewPlan, location))
      {
        area = area.ReplaceElement
        (
          Create(viewPlan, location),
          ExcludeUniqueProperties
        );
      }

      // We use ROOM_NAME here because Area.Name returns us a werid combination of "{Name} {Number}".
      if (number is object) area.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER).Update(number);
      if (name is object) area.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME).Update(name);

      if (!isNew && area.Location is object && area.Area == 0.0)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{area.Name}' is not in a properly enclosed region. {{{area.Id}}}");

      return area;
    }
  }
}
