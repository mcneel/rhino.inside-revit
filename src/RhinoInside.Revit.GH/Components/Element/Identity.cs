using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements.Obsolete
{
  [Obsolete("Obsolete since 2020-07-27")]
  public class ElementIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("D3917D58-7183-4B3F-9D22-03F0FE93B956");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;

    protected override string IconTag => "ID";

    public ElementIdentity() : base
    (
      name: "Element Identity",
      nickname: "Identity",
      description: "Queries element identity information",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query for its identity", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category in which the Element resides", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "ElementType of the Element", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "N", "A human readable name for the Element", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Autodesk.Revit.DB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      DA.SetData("Category", element?.Category);
      DA.SetData("Type", element?.Document.GetElement(element.GetTypeId()));
      DA.SetData("Name", element?.Name);
    }
  }
}
