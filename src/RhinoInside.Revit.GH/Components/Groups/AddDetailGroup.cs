using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.11")]
  public class AddDetailGroup : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("CC7790A0-1BD7-4DA6-ABB5-CE0BB553381E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddDetailGroup() : base
    (
      name: "Add Detail Group",
      nickname: "DetGroup",
      description: "Given its Location, it adds a detail group element to the active Revit document",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific detail component"
        }
      ),
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Point",
          NickName = "P",
          Description = "Detail Component center.",
        }
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Rotation",
          NickName = "R",
          Description = "Detail Component rotation",
          Optional = true,
          AngleParameter = true,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Detail group type.",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_IOSDetailGroups
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Group()
        {
          Name = _Group_,
          NickName = "G",
          Description = $"Output {_Group_}",
        }
      )
    };

    const string _Group_ = "Group";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM
    };

    internal override TransactionExtent TransactionExtent => TransactionExtent.Scope;

    ARDB.View ActiveView = default;
    readonly List<ARDB.View> ViewsToClose = new List<ARDB.View>();

    protected override void BeforeSolveInstance()
    {
      ActiveView = Revit.ActiveUIDocument?.ActiveView;

      base.BeforeSolveInstance();
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();

      ActiveView?.Document.SetActiveGraphicalView(ActiveView);
      ActiveView = default;

      if (ViewsToClose.Count > 0)
      {
        foreach (var view in (ViewsToClose as IEnumerable<ARDB.View>).Reverse())
          view.Close();

        ViewsToClose.Clear();
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;

      if (!view.Document.ActiveView.IsEquivalent(view.Value))
      {
        if (!view.Document.SetActiveGraphicalView(view.Value, out var viewWasOpen))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to activate view '{view.Nomen}'");
          return;
        }
        else if (!viewWasOpen)
        {
          ViewsToClose.Add(view.Value);
        }
      }

      ReconstructElement<ARDB.Group>
      (
        view.Document, _Group_, group =>
        {
          var tol = GeometryTolerance.Model;

          // Input
          if (!view.Value.IsAnnotationView()) throw new Exceptions.RuntimeArgumentException("View", $"View '{view.Nomen}' does not support detail items creation", view);
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Rotation", out double? rotation)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.ElementType type, Types.Document.FromValue(view.Document), ARDB.BuiltInCategory.OST_IOSDetailGroups)) return null;

          var viewPlane = view.Location;
          point = viewPlane.ClosestPoint(point.Value);

          if (rotation.HasValue && Params.Input<Param_Number>("Rotation")?.UseDegrees == true)
            rotation = Rhino.RhinoMath.ToRadians(rotation.Value);

          // Compute
          group = Reconstruct
          (
            group,
            view.Value,
            point.Value.ToXYZ(),
            rotation ?? 0.0,
            type.Value as ARDB.GroupType
          );

          DA.SetData(_Group_, group);
          return group;
        }
      );
    }

    bool Reuse
    (
      ref ARDB.Group group,
      ARDB.View view,
      ARDB.GroupType type
    )
    {
      if (group is null) return false;

      if (group.OwnerViewId != view.Id) return false;
      if (group.GetTypeId() != type.Id)
      {
        if (!ARDB.Element.IsValidType(group.Document, new ARDB.ElementId[] { group.Id }, type.Id))
          return false;

        group.GroupType = type;
      }

      return false;
    }

    ARDB.Group Create(ARDB.View view, ARDB.XYZ point, ARDB.GroupType type)
    {
      using (var create = view.Document.Create())
        return create.PlaceGroup(point, type);
    }

    ARDB.Group Reconstruct
    (
      ARDB.Group group,
      ARDB.View view,
      ARDB.XYZ origin,
      double angle,
      ARDB.GroupType type
    )
    {
      if (!Reuse(ref group, view, type))
      {
        group = group.ReplaceElement
        (
          Create(view, origin, type),
          ExcludeUniqueProperties
        );
      }

      // Set Location and Rotation
      {
        var goo = Types.Group.FromValue(group) as Types.Group;
        var location = new Plane
        (
          origin.ToPoint3d(),
          view.RightDirection.ToVector3d(),
          view.UpDirection.ToVector3d()
        );

        location.Rotate(angle, view.ViewDirection.ToVector3d());
        goo.Location = location;
      }

      return group;
    }
  }
}
