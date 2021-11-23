using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ElementTypes
{
  using External.DB.Extensions;

  public class ElementTypeDefault : Component
  {
    public override Guid ComponentGuid => new Guid("D67B341F-46E4-4532-980E-42CE035470CF");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "D";

    public ElementTypeDefault() : base
    (
      name: "Default Type",
      nickname: "Default",
      description: "Query default type",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to look for a default type", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "Default type on specified category", GH_ParamAccess.item);
    }

    static ARDB.ElementId GetDefaultElementTypeId(ARDB.Document doc, ARDB.ElementId categoryId)
    {
      var elementTypeId = doc.GetDefaultFamilyTypeId(categoryId);
      if (elementTypeId.IsValid())
        return elementTypeId;

      if (categoryId.TryGetBuiltInCategory(out var _))
      {
        foreach (var elementTypeGroup in Enum.GetValues(typeof(ARDB.ElementTypeGroup)).Cast<ARDB.ElementTypeGroup>())
        {
          var type = doc.GetElement(doc.GetDefaultElementTypeId(elementTypeGroup)) as ARDB.ElementType;
          if (type?.Category?.Id == categoryId || type?.Category?.Parent?.Id == categoryId)
            return type.Id;
        }
      }

      return ARDB.ElementId.InvalidElementId;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var category = default(Types.Category);
      if (!DA.GetData("Category", ref category) || !category.IsValid)
        return;

      var typeId = GetDefaultElementTypeId(category.Document, category.Id);
      DA.SetData("Type", Types.ElementType.FromElementId(category.Document, typeId));
    }
  }
}
