using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents.ElementTypes
{
  public class ElementTypeIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("7DEA1BA3-D9BC-4E94-9E1C-0E527187C9DC");
    protected override string IconTag => "ID";

    public ElementTypeIdentity()
    : base("ElementType.Identity", "ElementType.Identity", "Query type identity information", "Revit", "Type")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.ElementTypes.ElementType(), "Type", "T", "ElementType to query for its identity", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Categories.Category(), "Category", "C", "Category in which the ElementType resides", GH_ParamAccess.item);
      manager.AddTextParameter("FamilyName", "F", "The family name of the ElementType", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "N", "A human readable name for the ElementType", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementType = default(DB.ElementType);
      if (!DA.GetData("Type", ref elementType))
        return;

      DA.SetData("Category", elementType?.Category);
      DA.SetData("FamilyName", elementType?.FamilyName);
      DA.SetData("Name", elementType?.Name);
    }
  }
}
