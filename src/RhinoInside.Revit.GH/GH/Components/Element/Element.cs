using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class ElementGetter : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected static readonly Type ObjectType = typeof(Types.Element);

    protected ElementGetter(string propertyName)
      : base(ObjectType.Name + "." + propertyName, propertyName, "Get the " + propertyName + " of the specified " + ObjectType.Name, "Revit", ObjectType.Name)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), ObjectType.Name, ObjectType.Name.Substring(0, 1), ObjectType.Name + " to query", GH_ParamAccess.item);
    }
  }

  public class ElementMaterials : Component
  {
    public override Guid ComponentGuid => new Guid("93C18DFD-FAAB-4CF1-A681-C11754C2495D");

    public ElementMaterials()
    : base("Element.Materials", "Element.Materials", "Query element used materials", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query for its materials", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Materials", "M", "Materials this Element is made of", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Material(), "Paint", "P", "Materials used to paint this Element", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      DA.SetDataList("Materials", element?.GetMaterialIds(false).Select(x => element.Document.GetElement(x)));
      DA.SetDataList("Paint",     element?.GetMaterialIds( true).Select(x => element.Document.GetElement(x)));
    }
  }

  public class ElementDelete : TransactionsComponent
  {
    public override Guid ComponentGuid => new Guid("213C1F14-A827-40E2-957E-BA079ECCE700");
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    protected override string IconTag => "X";

    public ElementDelete()
    : base("Element.Delete", "Delete", "Deletes elements from Revit document", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Elements to delete", GH_ParamAccess.tree);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager) { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!DA.GetDataTree<Types.Element>("Elements", out var elementsTree))
        return;

      var elementsToDelete = Parameters.Element.
                             ToElementIds(elementsTree).
                             GroupBy(x => x.Document).
                             ToArray();

      foreach (var group in elementsToDelete)
      {
        BeginTransaction(group.Key);

        try
        {
          var deletedElements = group.Key.Delete(group.Select(x => x.Id).ToArray());

          if (deletedElements.Count == 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No elements were deleted");
          else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"{elementsToDelete.Length} elements and {deletedElements.Count - elementsToDelete.Length} dependant elements were deleted.");
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more of the elements cannot be deleted.");
        }
      }
    }
  }

  public class ElementGeometry : ElementGetter
  {
    public override Guid ComponentGuid => new Guid("B7E6A82F-684F-4045-A634-A4AA9F7427A8");
    static readonly string PropertyName = "Geometry";

    public ElementGeometry() : base(PropertyName) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewDetailLevel>(), "DetailLevel", "LOD", ObjectType.Name + " LOD [1, 3]", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter(PropertyName, PropertyName.Substring(0, 1), ObjectType.Name + " parameter names", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = null;
      if (!DA.GetData(ObjectType.Name, ref element))
        return;

      var detailLevel = DB.ViewDetailLevel.Undefined;
      DA.GetData(1, ref detailLevel);
      if (detailLevel == DB.ViewDetailLevel.Undefined)
        detailLevel = DB.ViewDetailLevel.Coarse;

      DB.Options options = null;
      using (var geometry = element?.GetGeometry(detailLevel, out options)) using (options)
      {
        var list = geometry?.ToRhino().Where(x => x is object).ToList();

        DA.SetDataList(PropertyName, list);
      }
    }
  }

  public class ElementPreview : ElementGetter
  {
    public override Guid ComponentGuid => new Guid("A95C7B73-6F70-46CA-85FC-A4402A3B6971");
    static readonly string PropertyName = "Preview";

    public ElementPreview() : base(PropertyName) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewDetailLevel>(), "DetailLevel", "LOD", ObjectType.Name + " LOD [1, 3]", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddNumberParameter("Quality", "Q", ObjectType.Name + " meshes quality [0.0, 1.0]", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddMeshParameter("Meshes", "M", ObjectType.Name + " meshes", GH_ParamAccess.list);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_OGLShader(), "Materials", "M", ObjectType.Name + " materials", GH_ParamAccess.list);
      manager.AddCurveParameter("Wires", "W", ObjectType.Name + " wires", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(DB.Element);
      if (!DA.GetData(ObjectType.Name, ref element))
        return;

      var detailLevel = DB.ViewDetailLevel.Undefined;
      DA.GetData(1, ref detailLevel);
      if (detailLevel == DB.ViewDetailLevel.Undefined)
        detailLevel = DB.ViewDetailLevel.Coarse;

      var relativeTolerance = double.NaN;
      if (DA.GetData(2, ref relativeTolerance))
      {
        if(0.0 > relativeTolerance || relativeTolerance > 1.0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Parameter '{Params.Input[2].Name}' range is [0.0, 1.0].");
          return;
        }
      }

      var meshingParameters = !double.IsNaN(relativeTolerance) ? new Rhino.Geometry.MeshingParameters(relativeTolerance, Revit.VertexTolerance) : null;
      Types.GeometricElement.BuildPreview(element, meshingParameters, detailLevel, out var materials, out var meshes, out var wires);

      DA.SetDataList(0, meshes?.Select((x) =>    new GH_Mesh(x)));
      DA.SetDataList(1, materials?.Select((x) => new GH_Material(x)));
      DA.SetDataList(2, wires?.Select((x) =>     new GH_Curve(x)));
    }
  }
}
