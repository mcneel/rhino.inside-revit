using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;

  public class CompoundStructureGeometry : Component
  {
    public override Guid ComponentGuid => new Guid("3DBAAAE8-90D2-465E-A88B-FCC2B64E3BB3");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "SG";

    public CompoundStructureGeometry() : base
    (
      name: "Element Parts Geometry",
      nickname: "PartGeom",
      description: "Extracts the parts geometry of the given element",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.GraphicalElement(),
        name: "Element",
        nickname: "E",
        description: "Element with Compound Structure e.g. Basic Wall, Floor, Ceiling, etc",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Grasshopper.Kernel.Parameters.Param_Brep(),
        name: "Geometry",
        nickname: "G",
        description: "Compound structure layer geometry sorted by layer index",
        access: GH_ParamAccess.tree
        );
    }

    private void GetCompoundStructureGeometry
    (
      ARDB.HostObject element,
      GH_Path basePath,
      out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      geometries = new GH_Structure<IGH_GeometricGoo>();
      var elementIds = new List<ARDB.ElementId>() { element.Id };

      bool createParts = ARDB.PartUtils.AreElementsValidForCreateParts(element.Document, elementIds);
      try
      {
        using (var transaction = new ARDB.Transaction(element.Document, nameof(GetCompoundStructureGeometry)))
        {
          transaction.Start();

          var type = element.Document.GetElement(element.GetTypeId()) as ARDB.HostObjAttributes;
          using (var structure = type.GetCompoundStructure())
          {
            var count = structure.LayerCount;
            var materials = new Dictionary<ARDB.ElementId, int>();
            for (int l = 0; l < count; ++l)
            {
              var material = ARDB.Material.Create(element.Document, Guid.NewGuid().ToString("N"));
              materials.Add(material, l);
              structure.SetMaterialId(l, material);
            }

            type.SetCompoundStructure(structure);

            if (createParts)
              ARDB.PartUtils.CreateParts(element.Document, elementIds);

            element.Document.Regenerate();

            // get the exploded parts
            foreach (var partId in ARDB.PartUtils.GetAssociatedParts(element.Document, element.Id, includePartsWithAssociatedParts: true, includeAllChildren: true))
            {
              if (element.Document.GetElement(partId) is ARDB.Part part)
              {
                var materialId = part.get_Parameter(ARDB.BuiltInParameter.DPART_MATERIAL_ID_PARAM).AsElementId();
                if (!materials.TryGetValue(materialId, out var layerIndex))
                  layerIndex = -1;

                var path = basePath.AppendElement(layerIndex);

                using (var options = new ARDB.Options())
                {
                  // extract geometry for each part
                  if (part.get_Geometry(options) is ARDB.GeometryElement geometryElement)
                  {
                    var list = geometryElement?.
                      ToGeometryBaseMany().
                      OfType<Brep>().
                      Where(x => !x.IsNullOrEmpty()).
                      Select(ElementGeometryComponent.ToGeometricGoo).
                      ToList();

                    geometries.AppendRange(list, path);
                  }
                }
              }
            }
          }
       }
      }
      catch { }
    }


    private void GetCompoundStructureGeometry
    (
      ARDB.Element element,
      GH_Path basePath,
      out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      geometries = new GH_Structure<IGH_GeometricGoo>();
      var elementIds = new List<ARDB.ElementId>() { element.Id };

      bool createParts = ARDB.PartUtils.AreElementsValidForCreateParts(element.Document, elementIds);
      try
      {
        // start a dry transaction that will be rolled back automatically
        // when execution goes out of next using statment
        using (var transaction = createParts ? new ARDB.Transaction(element.Document, nameof(GetCompoundStructureGeometry)) : default)
        {
          transaction?.Start();

          // explode the element into parts
          if (createParts)
          {
            ARDB.PartUtils.CreateParts(element.Document, elementIds);
            element.Document.Regenerate();
          }

          foreach (var partId in ARDB.PartUtils.GetAssociatedParts(element.Document, element.Id, includePartsWithAssociatedParts: true, includeAllChildren: true))
          {
            if (element.Document.GetElement(partId) is ARDB.Element part)
            {
              using (var options = new ARDB.Options())
              {
                // extract geometry for each part
                if (part.get_Geometry(options) is ARDB.GeometryElement geometryElement)
                {
                  var list = geometryElement?.
                    ToGeometryBaseMany().
                    OfType<Brep>().
                    Where(x => !x.IsNullOrEmpty()).
                    Select(ElementGeometryComponent.ToGeometricGoo).
                    ToList();

                  geometries.AppendRange(list, basePath);
                }
              }
            }
          }
        }
      }
      catch {}
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      ARDB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      var geometry = default(GH_Structure<IGH_GeometricGoo>);
      var _Geometry_ = Params.IndexOfOutputParam("Geometry");
      switch (element)
      {
        case ARDB.HostObject h: GetCompoundStructureGeometry(h, DA.ParameterTargetPath(_Geometry_), out geometry); break;
        case ARDB.Element e: GetCompoundStructureGeometry(e, DA.ParameterTargetPath(_Geometry_), out geometry); break;
      }

      DA.SetDataTree(_Geometry_, geometry);
    }
  }
}
