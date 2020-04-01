using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementTypeDuplicate : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ED7E612-E5C6-4F0E-AA69-814CF2478F7E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "D";

    public ElementTypeDuplicate() : base
    (
      "Duplicate ElementType", "Duplicate",
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
