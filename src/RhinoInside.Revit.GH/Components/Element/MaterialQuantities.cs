using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class ElementMaterialQuantities : Component
  {
    public override Guid ComponentGuid => new Guid("8A162EE6-812E-459B-9123-8F7735AAAC0C");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "Q";

    public ElementMaterialQuantities()
    : base
    (
      name: "Element Material Quantities",
      nickname: "MatQuantities",
      description: "Query element material information",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(),  "Element", "E", "Element to query for its material info", GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.Material(), "Materials", "M", "Materials used to build this element", GH_ParamAccess.list)].Optional = true;
      manager[manager.AddParameter(new Parameters.Material(), "Paint",     "P", "Material used to paint this element",  GH_ParamAccess.list)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddNumberParameter("Volume",   "V", "Material Volume", GH_ParamAccess.list);
      manager.AddNumberParameter("Area",     "A", "Material Area",   GH_ParamAccess.list);
      manager.AddNumberParameter("Painting", "P", "Painting Area",   GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.Element element = null;
      if (!DA.GetData("Element", ref element) || !element.IsValid)
        return;

      var area = Math.Pow(Revit.ModelUnits, 2.0);
      var volume = Math.Pow(Revit.ModelUnits, 3.0);

      var materialIds = new List<Types.Material>();
      if (DA.GetDataList("Materials", materialIds))
      {
        DA.SetDataList("Volume", materialIds.Select(x => x.Id is null ? default(double?) : element.Value.GetMaterialVolume(x.Id) * area));
        DA.SetDataList("Area",   materialIds.Select(x => x.Id is null ? default(double?) : element.Value.GetMaterialArea(x.Id, false) * volume));
      }

      var paintIds = new List<Types.Material>();
      if (DA.GetDataList("Paint", paintIds))
      {
        try { DA.SetDataList("Painting", paintIds.Select(x => x.Id is null ? default(double?) : element.Value.GetMaterialArea(x.Id, true) * area)); }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message); }
      }
    }
  }
}
