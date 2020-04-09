using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentWallsBySystemFamily : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("118F5744-292F-4BEC-9213-8073219D8563");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "W";

    public DocumentWallsBySystemFamily() : base(
      name: "Document Walls By System Family",
      nickname: "WxSF",
      description: "Get wall types and instances by wall system family (DB.WallKind)",
      category: "Revit",
      subCategory: "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      // required system family index value
      manager.AddParameter
      (
        new Param_Enum<Types.WallSystemFamily>(),
        name: "Wall System Family",
        nickname: "WSF",
        description: "Wall system family (corresponds to interger values of DB.WallKind)",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Wall Types",
        nickname: "WT",
        description: "Wall types, of the given wall system family",
        access: GH_ParamAccess.list
        );

      manager.AddParameter(
        param: new Parameters.HostObject(),
        name: "Wall Instances",
        nickname: "W",
        description: "Wall instances, of the given wall system family",
        access: GH_ParamAccess.list
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.DisableGapLogic();

      // grab wall system family from input
      int wallKindIndex = -1;
      if (!DA.GetData("Wall System Family", ref wallKindIndex))
        return;

      // convert provided integer to DB.WallKind
      DB.WallKind wallKind = DB.WallKind.Unknown;
      try {
        wallKind = (DB.WallKind) wallKindIndex;
      } catch {
        // let user know if input is invalid
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid wall system family index: {wallKindIndex}");
        return;
      }

      // collect wall types based on the given wallkind
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        DA.SetDataList
        (
          "Wall Types",
          collector.OfClass(typeof(DB.WallType))
                   .Where(x => ((DB.WallType) x).Kind == wallKind)
                   .Select(x => Types.ElementType.FromElement(x))
        );
      }

      // collect wall instances based on the given wallkind
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        IEnumerable<Types.Element> wallInstances;

        if (wallKind == DB.WallKind.Basic)
        {
          wallInstances = collector.OfClass(typeof(DB.Wall))
                                   .Cast<DB.Wall>()
                                   .Where(x => x.WallType.Kind == wallKind && !x.IsStackedWallMember)
                                   .Select(x => Types.Element.FromElement(x));
        }
        else
        {
          wallInstances = collector.OfClass(typeof(DB.Wall))
                                   .Where(x => ((DB.Wall) x).WallType.Kind == wallKind)
                                   .Select(x => Types.Element.FromElement(x));
        }

        DA.SetDataList("Wall Instances", wallInstances);
      }
    }
  }
}
