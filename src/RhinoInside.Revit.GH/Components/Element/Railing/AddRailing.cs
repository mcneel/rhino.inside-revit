using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public class AddRailing : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("601AC666-E369-464E-AE6F-34E01B9DBA3B");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    public AddRailing() : base
    (
      name: "Add Railing",
      nickname: "Railing",
      description: "Given a curve, it adds a Railing element to the active Revit document",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementType
        {
          Name = "Type",
          NickName = "T",
          Description = "Railing type",
          Optional = true,
          SelectedBuiltInCategory = ARDB.BuiltInCategory.OST_Railings
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Level
        {
          Name = "Level",
          NickName = "L",
          Description = "Railing base level",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement
        {
          Name = "Host",
          NickName = "H",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Flipped",
          NickName = "F",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Railing_,
          NickName = _Railing_.Substring(0, 1),
          Description = $"Output {_Railing_}",
        }
      )
    };

    const string _Railing_ = "Railing";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM,
      ARDB.BuiltInParameter.STAIRS_RAILING_HEIGHT_OFFSET,
    };

    static void ChangeElementTypeId<T>(ref T element, ARDB.ElementId elementTypeId) where T : ARDB.Element
    {
      if (element is object && elementTypeId != element.GetTypeId())
      {
        var doc = element.Document;
        if (element.IsValidType(elementTypeId))
        {
          var newElmentId = element.ChangeTypeId(elementTypeId);
          if (newElmentId != ARDB.ElementId.InvalidElementId)
            element = (T) doc.GetElement(newElmentId);
        }
        else element = null;
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Architecture.Railing>
      (
        doc.Value, _Railing_, railing =>
        {
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;
          if (!Parameters.ElementType.GetDataOrDefault(this, DA, "Type", out Types.ElementType type, doc, ARDB.ElementTypeGroup.StairsRailingType)) return null;

          var bbox = curve.GetBoundingBox(accurate: true);
          if (!Parameters.Level.GetDataOrDefault(this, DA, "Level", out Types.Level level, doc, bbox.IsValid ? bbox.Min.Z : double.NaN)) return null;
          if (!Params.TryGetData(DA, "Host", out Types.GraphicalElement host)) return null;
          if (!Params.TryGetData(DA, "Flipped", out bool? flipped)) return null;

          var tol = GeometryTolerance.Model;
          var levelPlane = new Plane
          (
            new Point3d(0.0, 0.0, level.Value.GetElevation() * GeometryDecoder.ModelScaleFactor),
            Vector3d.ZAxis
          );
          curve = curve.ProjectToPlane(levelPlane);
          curve = curve.Simplify(CurveSimplifyOptions.All & ~CurveSimplifyOptions.Merge, tol.VertexTolerance, tol.AngleTolerance) ?? curve;

          ChangeElementTypeId(ref railing, type.Value.Id);

          if (railing is ARDB.Architecture.Railing)
          {
            railing.SetPath(curve.ToBoundedCurveLoop());
          }
          else
          {
            var newRail = ARDB.Architecture.Railing.Create
            (
              doc.Value,
              curve.ToBoundedCurveLoop(),
              type.Value.Id,
              level.Value.Id
            );
            railing = railing.ReplaceElement(newRail, ExcludeUniqueProperties);
          }

          if (railing is object)
          {
            using (var baseLevel = railing.get_Parameter(ARDB.BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM))
            {
              if (!baseLevel.IsReadOnly)
                baseLevel.Update(level.Value.Id);
            }
            using (var heightOffset = railing.get_Parameter(ARDB.BuiltInParameter.STAIRS_RAILING_HEIGHT_OFFSET))
            {
              if (!heightOffset.IsReadOnly)
                heightOffset.Update(bbox.Min.Z * GeometryEncoder.ModelScaleFactor - level.Value.GetElevation());
            }

            railing.HostId = host?.Id ?? ARDB.ElementId.InvalidElementId;

            if (railing.Flipped != flipped)
              railing.Flip();
          }

          DA.SetData(_Railing_, railing);
          return railing;
        }
      );
    }
  }
}
