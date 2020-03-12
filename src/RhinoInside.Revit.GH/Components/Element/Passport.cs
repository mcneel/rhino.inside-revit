using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("D3917D58-7183-4B3F-9D22-03F0FE93B956");
    protected override string IconTag => "ID";

    public ElementIdentity()
    : base("Element.Identity", "Element.Identity", "Query element identity information", "Revit", "Element")
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

  public class ElementPassport : Component
  {
    public override Guid ComponentGuid => new Guid("BD534A54-B7ED-4D56-AA53-1F446D445DF6");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "PASS";

    public ElementPassport()
    : base("Element.Passport", "Element.Passport", string.Empty, "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "DocumentGUID", "GUID", "A unique identifier for the document the Element resides", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Document(), "Document", "Document", "The document that contains this element", GH_ParamAccess.item);
      manager.AddTextParameter("UniqueID", "UUID", "A stable across upgrades and workset operations unique identifier for the Element", GH_ParamAccess.item);
      manager.AddIntegerParameter("Id", "ID", "A unique identifier for an Element within the document that contains it", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var reference = default(Types.ElementId);
      if (!DA.GetData("Element", ref reference))
        return;

      DA.SetData("DocumentGUID", reference?.DocumentGUID);
      DA.SetData("Document", reference?.Document);
      DA.SetData("UniqueID", reference?.UniqueID);
      DA.SetData("Id", reference?.Id.IntegerValue);
    }
  }
}
