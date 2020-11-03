using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Family : ElementIdWithoutPreviewParam<Types.Family, DB.Family>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("3966ADD8-07C0-43E7-874B-6EFF95598EB0");

    public Family() : base("Family", "Family", "Represents a Revit document family.", "Params", "Revit Primitives") { }
  }

  public class DocumentFamiliesPicker : DocumentPicker
  {
    public override Guid ComponentGuid => new Guid("45CEE087-4194-4E55-AA20-9CC5D2193CE0");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Family));

    public DocumentFamiliesPicker()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Component Families Picker";
      MutableNickName = false;
      Description = "Provides a Family picker";

      ListMode = GH_ValueListMode.DropDown;
    }

    void RefreshList()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (Revit.ActiveDBDocument is DB.Document doc)
      {
        using (var collector = new DB.FilteredElementCollector(doc))
        {
          foreach (var family in collector.OfClass(typeof(DB.Family)).Cast<DB.Family>().OrderBy((x) => $"{x.FamilyCategory.Name} : {x.Name}"))
          {
            var referenceId = FullUniqueId.Format(doc.GetFingerprintGUID(), family.UniqueId);
            var item = new GH_ValueListItem($"{family.FamilyCategory.Name} : {family.Name}", $"\"{ referenceId }\"");
            item.Selected = selectedItems.Contains(item.Expression);
            ListItems.Add(item);
          }
        }
      }
    }

    protected override void CollectVolatileData_Custom()
    {
      NickName = "Component Family";
      RefreshList();
      base.CollectVolatileData_Custom();
    }
  }
}
