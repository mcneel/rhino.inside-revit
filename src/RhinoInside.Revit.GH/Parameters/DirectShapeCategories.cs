using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class DirectShapeCategories : Grasshopper.Special.ValueSet<Types.CategoryId>
  {
    public override Guid ComponentGuid => new Guid("7BAFE137-332B-481A-BE22-09E8BD4C86FC");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public DirectShapeCategories() : base
    (
      name: "DirectShape Categories",
      nickname: "Categories",
      description: "Provides a picker for direct shape categories",
      category: "Revit",
      subcategory: "DirectShape"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();

        if (Document.TryGetCurrentDocument(this, out var doc))
        {
          var categories = Types.CategoryId.EnumValues.
            Where(x => DB.DirectShape.IsValidCategoryId(new DB.ElementId(x.Value), doc.Value));
          m_data.AppendRange(categories);
        }
      }

      base.LoadVolatileData();
    }
  }
}
