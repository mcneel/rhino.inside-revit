using System;
using System.Linq;
using System.Collections.Generic;

using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CompoundStructureGeometry : Component
  {
    public override Guid ComponentGuid => new Guid("3DBAAAE8-90D2-465E-A88B-FCC2B64E3BB3");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "CSG";

    public CompoundStructureGeometry() : base(
      name: "Element Compound Structure Geometry",
      nickname: "CSG",
      description: "Extracts the geometry of given compound structure layers",
      category: "Revit",
      subCategory: "Element"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Element",
        nickname: "E",
        description: "Element with Compound Structure e.g. Basic Wall, Floor, Ceiling, etc",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
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

      if (DB.PartUtils.AreElementsValidForCreateParts(element.Document, elementIds))
      {
        try
        {
          // start a dry transaction that will be rolled back later
          var t = new DB.Transaction(element.Document, "Dry Transaction");
          t.Start();

          // explode the element into parts
          DB.PartUtils.CreateParts(element.Document, elementIds);
          element.Document.Regenerate();
          // get the exploded parts
          foreach(DB.ElementId partId in DB.PartUtils.GetAssociatedParts(element.Document, element.Id, includePartsWithAssociatedParts: true, includeAllChildren: true))
          {
            DB.Element part = element.Document.GetElement(partId);
            if (part != null)
            {
              // extract geometry for each part
              DB.GeometryElement partGeom = part.get_Geometry(new DB.Options());
              if (partGeom != null)
                foreach (DB.GeometryObject geom in partGeom)
                  layerGeoms.AddRange(geom.ToRhino().Cast<Rhino.Geometry.Brep>());
            }
          }

          // rollback the changes now
          t.RollBack();
        }
        catch {}
      }
      return layerGeoms;
    }

    private List<Rhino.Geometry.Brep> GetGenericLayerGeometry(DB.Element element)
    {
      return GetCompoundStructureLayerGeom(element)
             .OrderBy(x => Rhino.Geometry.VolumeMassProperties.Compute(x, volume: true, firstMoments: true, secondMoments: false, productMoments: false).Centroid.Z)
             .ToList();
    }

    private List<Rhino.Geometry.Brep> GetWallLayerGeometry(DB.Wall wall)
    {
      // determine wall anchor point to help in sorting layer breps
      DB.Curve wallLocationCurve = (wall.Location as DB.LocationCurve).Curve;
      DB.XYZ wallEndPoint = wallLocationCurve.GetEndPoint(0);
      DB.XYZ wallOrientation = wall.Orientation;
      DB.XYZ anchor = wallEndPoint + (wallOrientation * wallLocationCurve.Length);
      Rhino.Geometry.Point3d basePoint = anchor.ToRhino();

      return GetCompoundStructureLayerGeom(wall)
             .OrderBy(x => Rhino.Geometry.VolumeMassProperties.Compute(x, volume: true, firstMoments: true, secondMoments: false, productMoments: false).Centroid.DistanceTo(basePoint))
             .ToList();

    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Element element = default;
      if (!DA.GetData("Element", ref element))
        return;

      // decide how to extract layer geometry by element type
      switch (element)
      {
        case DB.Wall wall:
          var layerGeoms = new List<Rhino.Geometry.Brep>();
          // curtain walls don't have layers
          if (wall.WallType.Kind != DB.WallKind.Curtain)
          {
            // stacked walls have multiple base walls, let's process all of them
            if (wall.WallType.Kind == DB.WallKind.Stacked)
              foreach (DB.Wall wallPart in wall.GetStackedWallMemberIds().Select(x => wall.Document.GetElement(x)))
                layerGeoms.AddRange(GetWallLayerGeometry(wallPart));
            else
              layerGeoms = GetWallLayerGeometry(wall);
          }
          DA.SetDataList("Geometry", layerGeoms);
          break;

        case DB.Element elmnt:
          DA.SetDataList("Geometry", GetGenericLayerGeometry(elmnt));
          break;
      }
    }
  }
}
