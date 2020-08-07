using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
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
        access: GH_ParamAccess.list
        );
    }

    private List<Rhino.Geometry.Brep> GetCompoundStructureLayerGeom(DB.Element element)
    {
      var layerGeoms = new List<Rhino.Geometry.Brep>();
      var elementIds = new List<DB.ElementId>() { element.Id };

      bool createParts = DB.PartUtils.AreElementsValidForCreateParts(element.Document, elementIds);
      try
      {
        // start a dry transaction that will be rolled back automatically
        // when execution goes out of next using statment
        using (var transaction = createParts ? new DB.Transaction(element.Document, nameof(GetCompoundStructureLayerGeom)) : default)
        {
          transaction?.Start();

          // explode the element into parts
          if (createParts)
          {
            DB.PartUtils.CreateParts(element.Document, elementIds);
            element.Document.Regenerate();
          }

          // get the exploded parts
          foreach (DB.ElementId partId in DB.PartUtils.GetAssociatedParts(element.Document, element.Id, includePartsWithAssociatedParts: true, includeAllChildren: true))
          {
            if (element.Document.GetElement(partId) is DB.Element part)
            {
              using (var options = new DB.Options())
              {
                // extract geometry for each part
                if (part.get_Geometry(options) is DB.GeometryElement partGeom)
                {
                  foreach (DB.GeometryObject geom in partGeom)
                    layerGeoms.AddRange(geom.ToGeometryBaseMany().OfType<Rhino.Geometry.Brep>());
                }
              }
            }
          }
        }
      }
      catch {}

      return layerGeoms;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      DA.SetDataList("Geometry", GetCompoundStructureLayerGeom(element));
    }
  }
}
