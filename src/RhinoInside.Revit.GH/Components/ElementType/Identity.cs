using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementTypeIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("7DEA1BA3-D9BC-4E94-9E1C-0E527187C9DC");
    protected override string IconTag => "ID";

    public ElementTypeIdentity() : base
    (
      name: "Type Identity",
      nickname: "TypIdent",
      description: "Query type identity information",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "Type to query for its identity", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category in which the Type resides", GH_ParamAccess.item);
      manager.AddTextParameter("Family Name", "F", "The family name of the Type", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "N", "A human readable name for the Type", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementType = default(DB.ElementType);
      if (!DA.GetData("Type", ref elementType))
        return;

      DA.SetData("Category", elementType?.Category);
      DA.SetData("Family Name", elementType?.FamilyName);
      DA.SetData("Name", elementType?.Name);
    }
  }
}
