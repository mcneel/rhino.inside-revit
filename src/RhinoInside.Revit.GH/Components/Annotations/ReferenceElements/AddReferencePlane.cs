using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Annotations.ReferenceElements
{
  using External.DB.Extensions;
  using Convert.Geometry;

  [ComponentVersion(introduced: "1.8")]
  public class AddReferencePlane : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("4BE42EC7-5471-4448-8DD6-6F28F76BCB5F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddReferencePlane() : base
    (
      name: "Add Reference Plane",
      nickname: "R-Plane",
      description: "Given a plane definition, it adds a Reference Plane to the current Revit document",
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
        new Parameters.ReferencePlane()
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
            referencePlane = Reconstruct(referencePlane, doc.Value, plane.Value.Origin.ToXYZ(), (ERDB.UnitXYZ) plane.Value.XAxis.ToXYZ(), (ERDB.UnitXYZ) plane.Value.YAxis.ToXYZ(), name, template);

          DA.SetData(_ReferencePlane_, referencePlane);
          return untracked ? null : referencePlane;
        }
      );
    }

    bool Reuse
    (
      ARDB.ReferencePlane referencePlane,
      ARDB.XYZ origin, ERDB.UnitXYZ basisX, ERDB.UnitXYZ basisY,
      ARDB.ReferencePlane template
    )
    {
      if (referencePlane is null) return false;

      var pinned = referencePlane.Pinned;
      referencePlane.Pinned = false;
      referencePlane.SetLocation(origin, basisX, basisY);
      referencePlane.Pinned = pinned;

      referencePlane.CopyParametersFrom(template, ExcludeUniqueProperties);
      return true;
    }

    ARDB.ReferencePlane Create
    (
      ARDB.Document doc,
      ARDB.XYZ origin, ERDB.UnitXYZ basisX, ERDB.UnitXYZ basisY,
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
        using (var create = doc.Create())
          referencePlane = create.NewReferencePlane2(origin + basisX * 30.0, origin, origin - basisY * 30.0, default);

        referencePlane.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      return referencePlane;
    }

    ARDB.ReferencePlane Reconstruct
    (
      ARDB.ReferencePlane referencePlane, ARDB.Document doc,
      ARDB.XYZ origin, ERDB.UnitXYZ basisX, ERDB.UnitXYZ basisY,
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
