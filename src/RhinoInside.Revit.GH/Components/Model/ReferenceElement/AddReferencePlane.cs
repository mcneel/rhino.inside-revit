using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Levels
{
  using External.DB.Extensions;
  using RhinoInside.Revit.Convert.Geometry;

  [ComponentVersion(introduced: "1.8")]
  public class AddReferencePlane : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("4BE42EC7-5471-4448-8DD6-6F28F76BCB5F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddReferencePlane() : base
    (
      name: "Add Reference Plane",
      nickname: "Reference Plane",
      description: "Given its elevation, it adds a Reference Plane to the current Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Plane",
          NickName = "P",
          Description = "Plane definition",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Reference Plane Name",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      //new ParamDefinition
      //(
      //  new Parameters.GraphicalElement()
      //  {
      //    Name = "Template",
      //    NickName = "T",
      //    Description = "Template Reference Plane",
      //    Optional = true
      //  },
      //  ParamRelevance.Occasional
      //),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _ReferencePlane_,
          NickName = "RP",
          Description = $"Output {_ReferencePlane_}",
        }
      ),
    };

    const string _ReferencePlane_ = "Reference Plane";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.DATUM_TEXT
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ReferencePlane>
      (
        doc.Value, _ReferencePlane_, referencePlane =>
        {
          // Input
          if (!Params.GetData(DA, "Plane", out Plane? plane)) return null;
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          Params.TryGetData(DA, "Template", out ARDB.ReferencePlane template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_ReferencePlane_, out var untracked, ref referencePlane, doc.Value, name, categoryId: ARDB.BuiltInCategory.OST_CLines))
            referencePlane = Reconstruct(referencePlane, doc.Value, plane.Value.Origin.ToXYZ(), plane.Value.XAxis.ToXYZ(), plane.Value.YAxis.ToXYZ(), name, template);

          DA.SetData(_ReferencePlane_, referencePlane);
          return untracked ? null : referencePlane;
        }
      );
    }

    public static void SetLocation(ARDB.ReferencePlane element, ARDB.XYZ newOrigin, ARDB.XYZ newBasisX, ARDB.XYZ newBasisY)
    {
      var plane = element.GetPlane();
      var origin = plane.Origin;
      var basisX = plane.XVec;
      var basisY = plane.YVec;
      var basisZ = basisX.CrossProduct(basisY);

      var newBasisZ = newBasisX.CrossProduct(newBasisY);
      {
        if (!basisZ.IsCodirectionalTo(newBasisZ))
        {
          var angle = Math.PI;
          var axisDirection = basisZ.CrossProduct(newBasisZ);
          if (axisDirection.IsZeroLength())
            axisDirection = basisY;
          else
            angle = basisZ.AngleTo(newBasisZ);

          using (var axis = ARDB.Line.CreateUnbound(origin, axisDirection))
            ARDB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

          plane = element.GetPlane();
          origin = plane.Origin;
          basisX = plane.XVec;
          basisY = plane.YVec;
          basisZ = basisX.CrossProduct(basisY);
        }

        if (!basisX.IsAlmostEqualTo(newBasisX))
        {
          double angle = basisX.AngleOnPlaneTo(newBasisX, newBasisZ);
          using (var axis = ARDB.Line.CreateUnbound(origin, newBasisZ))
            ARDB.ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
        }

        {
          var trans = newOrigin - origin;
          if (!trans.IsZeroLength())
            ARDB.ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
        }
      }
    }


    bool Reuse
    (
      ARDB.ReferencePlane referencePlane,
      ARDB.XYZ origin, ARDB.XYZ basisX, ARDB.XYZ basisY,
      ARDB.ReferencePlane template
    )
    {
      if (referencePlane is null) return false;

      var pinned = referencePlane.Pinned;
      referencePlane.Pinned = false;
      SetLocation(referencePlane, origin, basisX, basisY);
      referencePlane.Pinned = pinned;

      referencePlane.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.ReferencePlane Create
    (
      ARDB.Document doc,
      ARDB.XYZ origin, ARDB.XYZ basisX, ARDB.XYZ basisY,
      ARDB.ReferencePlane template
    )
    {
      var referencePlane = default(ARDB.ReferencePlane);

      // Try to duplicate template
      //if (template is object)
      //{
      //  referencePlane = template.CloneElement(doc);
      //}

      // Else create a brand new
      if (referencePlane is null)
      {
        basisX *= 30.0;
        basisY *= 30.0;

        if (doc.IsFamilyDocument)
          referencePlane = doc.FamilyCreate.NewReferencePlane2(origin + basisX, origin, origin - basisY, default);
        else
          referencePlane = doc.Create.NewReferencePlane2(origin + basisX , origin, origin - basisY, default);

        referencePlane.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      return referencePlane;
    }

    ARDB.ReferencePlane Reconstruct
    (
      ARDB.ReferencePlane referencePlane, ARDB.Document doc,
      ARDB.XYZ origin, ARDB.XYZ basisX, ARDB.XYZ basisY,
      string name, ARDB.ReferencePlane template
    )
    {
      if (!Reuse(referencePlane, origin, basisX, basisY, template))
      {
        referencePlane = referencePlane.ReplaceElement
        (
          Create(doc, origin, basisX, basisY, template),
          ExcludeUniqueProperties
        );
      }

      if (name is object && referencePlane.Name != name)
        referencePlane.Name = name;

      return referencePlane;
    }
  }
}
