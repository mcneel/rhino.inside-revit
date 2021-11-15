using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CompoundStructureGeometry : Component
  {
    public override Guid ComponentGuid => new Guid("3DBAAAE8-90D2-465E-A88B-FCC2B64E3BB3");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "PG";

    public CompoundStructureGeometry() : base
    (
      name: "Element Parts Geometry",
      nickname: "PartGeom",
      description: "Extracts the parts geometry of the given element",
      category: "Revit",
      subCategory: "Element"
    )
    {
    }

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
      DB.HostObject element,
      GH_Path basePath,
      out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      geometries = new GH_Structure<IGH_GeometricGoo>();
      var elementIds = new List<DB.ElementId>() { element.Id };

      bool createParts = DB.PartUtils.AreElementsValidForCreateParts(element.Document, elementIds);
      try
      {
        using (var transaction = new DB.Transaction(element.Document, nameof(GetCompoundStructureGeometry)))
        {
          transaction.Start();

          var type = element.Document.GetElement(element.GetTypeId()) as DB.HostObjAttributes;
          using (var structure = type.GetCompoundStructure())
          {
            var count = structure.LayerCount;
            var materials = new Dictionary<DB.ElementId, int>();
            for (int l = 0; l < count; ++l)
            {
              var material = DB.Material.Create(element.Document, Guid.NewGuid().ToString("N"));
              materials.Add(material, l);
              structure.SetMaterialId(l, material);
            }

            type.SetCompoundStructure(structure);

            if (createParts)
              DB.PartUtils.CreateParts(element.Document, elementIds);

            element.Document.Regenerate();

            // get the exploded parts
            foreach (var partId in DB.PartUtils.GetAssociatedParts(element.Document, element.Id, includePartsWithAssociatedParts: true, includeAllChildren: true))
            {
              if (element.Document.GetElement(partId) is DB.Part part)
              {
                var materialId = part.get_Parameter(DB.BuiltInParameter.DPART_MATERIAL_ID_PARAM).AsElementId();
                if (!materials.TryGetValue(materialId, out var layerIndex))
                  layerIndex = -1;

                var path = basePath.AppendElement(layerIndex);

                using (var options = new DB.Options())
                {
                  // extract geometry for each part
                  if (part.get_Geometry(options) is DB.GeometryElement geometryElement)
                  {
                    var list = geometryElement?.
                      ToGeometryBaseMany().
                      OfType<Brep>().
                      Where(x => !x.IsNullOrEmpty()).
                      Convert(ElementGeometryComponent.ToGeometricGoo).
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
      DB.Element element,
      GH_Path basePath,
      out GH_Structure<IGH_GeometricGoo> geometries
    )
    {
      geometries = new GH_Structure<IGH_GeometricGoo>();
      var elementIds = new List<DB.ElementId>() { element.Id };

      bool createParts = DB.PartUtils.AreElementsValidForCreateParts(element.Document, elementIds);
      try
      {
        // start a dry transaction that will be rolled back automatically
        // when execution goes out of next using statment
        using (var transaction = createParts ? new DB.Transaction(element.Document, nameof(GetCompoundStructureGeometry)) : default)
        {
          transaction?.Start();

          // explode the element into parts
          if (createParts)
          {
            DB.PartUtils.CreateParts(element.Document, elementIds);
            element.Document.Regenerate();
          }

          foreach (var partId in DB.PartUtils.GetAssociatedParts(element.Document, element.Id, includePartsWithAssociatedParts: true, includeAllChildren: true))
          {
            if (element.Document.GetElement(partId) is DB.Element part)
            {
              using (var options = new DB.Options())
              {
                // extract geometry for each part
                if (part.get_Geometry(options) is DB.GeometryElement geometryElement)
                {
                  var list = geometryElement?.
                    ToGeometryBaseMany().
                    OfType<Brep>().
                    Where(x => !x.IsNullOrEmpty()).
                    Convert(ElementGeometryComponent.ToGeometricGoo).
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
      DB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      var geometry = default(GH_Structure<IGH_GeometricGoo>);
      var _Geometry_ = Params.IndexOfOutputParam("Geometry");
      switch (element)
      {
        case DB.HostObject h: GetCompoundStructureGeometry(h, DA.ParameterTargetPath(_Geometry_), out geometry); break;
        case DB.Element e: GetCompoundStructureGeometry(e, DA.ParameterTargetPath(_Geometry_), out geometry); break;
      }

      DA.SetDataTree(_Geometry_, geometry);
    }
  }
}
