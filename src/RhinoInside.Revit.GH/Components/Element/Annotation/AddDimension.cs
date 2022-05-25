using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Annotation
{
  [ComponentVersion(introduced: "1.8")]
  public abstract class AddDimension : ElementTrackerComponent
  {
    protected AddDimension(string name, string nickname, string description, string category, string subCategory) :
      base(name, nickname, description, category, subCategory)
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
          Description = "View to add a specific dimension"
        }
      ),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
          Description = "Point to place a specific dimension",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "References",
          NickName = "R",
          Description = "References to create a specific dimension",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Element type of the given dimension",
          Optional = true
        }, ParamRelevance.Occasional
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Dimension";
    protected virtual bool IsHorizontal { get; }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.Dimension>
      (
        view.Document, _Output_, (dimension) =>
        {
          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;
          if (!Params.TryGetData(DA, "Type", out ARDB.DimensionType type)) return null;
          if (!Params.GetDataList(DA, "References", out IList<ARDB.Element> elements)) return null;

          if
          (
            view.ViewType is ARDB.ViewType.ThreeD ||
            view.ViewType is ARDB.ViewType.Schedule ||
            view.ViewType is ARDB.ViewType.ColumnSchedule ||
            view.ViewType is ARDB.ViewType.PanelSchedule
          )
            throw new Exceptions.RuntimeArgumentException("View", "This view does not support detail items creation", view);

          // Compute
          dimension = Reconstruct(dimension, view, point.Value, elements, type);

          DA.SetData(_Output_, dimension);
          return dimension;
        }
      );
    }

    bool Reuse(ARDB.Dimension dimension, ARDB.View view, Point3d point, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      if (dimension is null) return false;

      if (dimension.OwnerViewId != view.Id) return false;
      if (type != default && dimension.GetTypeId() != type.Id) return false;

      //Point
      var plane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
      var projectedPoint = plane.ClosestPoint(point);
      var curve = dimension.Curve.ToCurve();

      if (this.IsHorizontal)
      {
        if (Math.Abs(curve.PointAt(0).Y - projectedPoint.Y) > GeometryObjectTolerance.Model.VertexTolerance ||
            Math.Abs(curve.PointAt(0).Z - projectedPoint.Z) > GeometryObjectTolerance.Model.VertexTolerance)
          return false;
      }
      else
      {
        if (Math.Abs(curve.PointAt(0).X - projectedPoint.X) > GeometryObjectTolerance.Model.VertexTolerance ||
            Math.Abs(curve.PointAt(0).Z - projectedPoint.Z) > GeometryObjectTolerance.Model.VertexTolerance)
          return false;
      }

      //Elements
      if (dimension.References.Size != elements.Count) return false;
      foreach (ARDB.Element element in elements)
      {
        var isInReferences = false;

        for (int i = 0; i < dimension.References.Size; i++)
        {
          if (dimension.References.get_Item(i).ElementId == element.Id)
          {
            isInReferences = true;
            break;
          }
        }
          
        
        if (isInReferences == false) return false;
      }


      return true;
    }

    ARDB.Dimension Create(ARDB.View view, Point3d point, IList<ARDB.Element> elements, ARDB.DimensionType type)
    {
      var plane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
      var projectedPoint = plane.ClosestPoint(point);

      Line line;
      if (this.IsHorizontal) line = new Line(projectedPoint, plane.XAxis);
      else  line = new Line(projectedPoint, plane.YAxis);

      var references = this.GetReferences(view.Document, elements);      

      if (type == default)
        return view.Document.Create.NewDimension(view, line.ToLine(), references);
      else
        return view.Document.Create.NewDimension(view, line.ToLine(), references, type);

    }

    public ARDB.ReferenceArray GetReferences(ARDB.Document doc, IList<ARDB.Element> elements)
    {
      ICollection<ARDB.ElementId> AssElemIds = elements.Select(x => x.Id).ToList();
      ARDB.ReferenceArray ra = new ARDB.ReferenceArray();
      foreach (ARDB.ElementId aeId in AssElemIds)
      {
        ARDB.Element ae = doc.GetElement(aeId);
        ARDB.FamilyInstance fmly = ae as ARDB.FamilyInstance;
        if (null != fmly)
        {
          switch (fmly.Category.Id.IntegerValue)
          {
            case ((int) ARDB.BuiltInCategory.OST_Columns):
              ARDB.Reference refCol = fmly.GetReferenceByName("Center (Left/Right)");
              ra.Append(refCol);
              break;
            case ((int) ARDB.BuiltInCategory.OST_StructuralFraming):
              ARDB.Reference refBeamCen = fmly.GetReferenceByName("Center (Left/Right)");
              ra.Append(refBeamCen);
              break;
            case ((int) ARDB.BuiltInCategory.OST_StructuralColumns):
              ARDB.Reference refColCen = fmly.GetReferenceByName("Center (Left/Right)");
              ra.Append(refColCen);
              break;
            case ((int) ARDB.BuiltInCategory.OST_GenericModel):
              ARDB.Reference refGMCen = fmly.GetReferenceByName("Center (Left/Right)");
              ra.Append(refGMCen);
              break;
            case ((int) ARDB.BuiltInCategory.OST_StructConnections):
              ARDB.Reference refSCCen = fmly.GetReferenceByName("Center (Left/Right)");
              ra.Append(refSCCen);
              break;
          }
        }
        else
        {
          var ele = doc.GetElement(aeId);
          ra.Append(new ARDB.Reference(ele));
        }

      }
      return ra;

    }

    ARDB.Dimension Reconstruct(ARDB.Dimension dimension, ARDB.View view, Point3d point, IList<ARDB.Element> elements, ARDB.DimensionType type = default)
    {
      if (!Reuse(dimension, view, point, elements, type))
        dimension = Create(view, point, elements, type);

      return dimension;
    }

  }


}

