using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementTypeSimilar : Component
  {
    public override Guid ComponentGuid => new Guid("BA9C72C5-EC88-450B-B736-BE6D827FA2F3");
    protected override string IconTag => "S";

    public ElementTypeSimilar()
    : base("ElementType.Similar", "ElementType.Similar", "Obtains a set of types that are similar to Type", "Revit", "Type")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "ElementType to query for its similar types", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementType = default(DB.ElementType);
      if (!DA.GetData("Type", ref elementType))
        return;

      DA.SetDataList("Types", elementType?.GetSimilarTypes());
    }
  }

  public class ElementTypeDuplicate : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ED7E612-E5C6-4F0E-AA69-814CF2478F7E");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "D";

    public ElementTypeDuplicate() : base
    (
      "ElementType.Duplicate", "Duplicate",
      "Given a Name, it duplicates an ElementType into the active Revit document",
      "Revit", "Type"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "New ElementType", GH_ParamAccess.item);
    }

    void ReconstructElementTypeDuplicate
    (
      DB.Document doc,
      ref DB.ElementType elementType,

      DB.ElementType type,
      string name
    )
    {
      ReplaceElement(ref elementType, type.Duplicate(name));
    }
  }
}
