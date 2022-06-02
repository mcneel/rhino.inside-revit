using System;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Geometry
{
  using Convert.Geometry;
  using External.DB;

  public class ElementPreview : Component
  {
    public override Guid ComponentGuid => new Guid("A95C7B73-6F70-46CA-85FC-A4402A3B6971");
    protected override string IconTag => "P";

    public ElementPreview()
    : base("Element Preview", "Preview", "Get the preview of the specified Element", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query", GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewDetailLevel>(), "DetailLevel", "LOD", "Preview Level of detail [1, 3]", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddNumberParameter("Quality", "Q", "Meshes quality [0.0, 1.0]", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddMeshParameter("Meshes", "M", "Element meshes", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Material(), "Materials", "M", "Element materials", GH_ParamAccess.list);
      manager.AddCurveParameter("Wires", "W", "Element wires", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(ARDB.Element);
      if (!DA.GetData(0, ref element))
        return;

      var scope = default(IDisposable);
      var detailLevel = ARDB.ViewDetailLevel.Undefined;
      DA.GetData(1, ref detailLevel);
      if (detailLevel == ARDB.ViewDetailLevel.Undefined)
      {
        detailLevel = ARDB.ViewDetailLevel.Coarse;
      }
      else if (element is ARDB.FamilySymbol symbol && !symbol.IsActive)
      {
        scope = symbol.Document.RollBackScope();
        symbol.Activate();
        symbol.Document.Regenerate();
      }

      using (scope)
      {
        var quality = 0.5;
        if (DA.GetData(2, ref quality))
        {
          if (0.0 > quality || quality > 1.0)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Parameter '{Params.Input[2].Name}' range is [0.0, 1.0].");
            return;
          }
        }

        var meshingParameters = !double.IsNaN(quality) ? new MeshingParameters(quality, GeometryTolerance.Internal.VertexTolerance) : null;
        Types.GeometricElement.BuildPreview(element, meshingParameters, detailLevel, out var materials, out var meshes, out var wires);

        for (int m = 0; m < meshes?.Length; ++m)
          meshes[m] = MeshDecoder.FromRawMesh(meshes[m], UnitConverter.NoScale);

        var outMesh = new Mesh();
        var dictionary = Convert.Display.PreviewConverter.ZipByMaterial(materials, meshes, outMesh);
        if (dictionary is null)
        {
          // In case ZipByMaterial fails we just return the unclasified preview meshes
          DA.SetDataList(0, meshes?.Select(x => new GH_Mesh(x)));
          DA.SetDataList(1, materials?.Select(x => new Types.Material(x)));
        }
        else
        {
          // On success we return the classified set of meshes.
          if (outMesh.Faces.Count > 0)
          {
            DA.SetDataList(0, dictionary.Values.Select(x => new GH_Mesh(x)).Concat(Enumerable.Repeat(new GH_Mesh(outMesh), 1)));
            DA.SetDataList(1, dictionary.Keys.Select(x => new Types.Material(x)).Concat(Enumerable.Repeat(new Types.Material(), 1)));
          }
          else
          {
            DA.SetDataList(0, dictionary.Values.Select(x => new GH_Mesh(x)));
            DA.SetDataList(1, dictionary.Keys.Select(x => new Types.Material(x)));
          }
        }

        DA.SetDataList(2, wires?.Select(x => new GH_Curve(x)));
      }
    }
  }
}
