using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementTypeDefault : Component
  {
    public override Guid ComponentGuid => new Guid("D67B341F-46E4-4532-980E-42CE035470CF");
    protected override string IconTag => "D";

    public ElementTypeDefault()
    : base("Default ElementType", "Default", "Query default type", "Revit", "Type")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category in which the ElementType resides", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "ElementType to query for its identity", GH_ParamAccess.item);
    }

    public static DB.ElementId GetDefaultElementTypeId(DB.Document doc, DB.ElementId categoryId)
    {
      var elementTypeId = doc.GetDefaultFamilyTypeId(categoryId);
      if (elementTypeId.IsValid())
        return elementTypeId;

      if (categoryId.TryGetBuiltInCategory(out var cat))
      {
        foreach (var elementTypeGroup in Enum.GetValues(typeof(DB.ElementTypeGroup)).Cast<DB.ElementTypeGroup>())
        {
          var type = doc.GetElement(doc.GetDefaultElementTypeId(elementTypeGroup)) as DB.ElementType;
          if (type?.Category?.Id.IntegerValue == (int) cat)
            return type.Id;
        }
      }

      return DB.ElementId.InvalidElementId;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var category = default(DB.Category);
      if (!DA.GetData("Category", ref category))
        return;

      var doc = category.Document();
      var elementTypeId = GetDefaultElementTypeId(doc, category.Id);
      DA.SetData("Type", doc.GetElement(elementTypeId) as DB.ElementType);
    }
  }
}
