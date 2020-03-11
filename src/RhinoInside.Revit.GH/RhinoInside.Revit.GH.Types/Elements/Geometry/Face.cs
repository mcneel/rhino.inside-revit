using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Elements.Geometry
{
  public class Face : GeometryObject<DB.Face>, IGH_PreviewData
  {
    public override string TypeName => "Revit Face";
    public override string TypeDescription => "Represents a Revit Face";

    public Face() { }
    public Face(DB.Face face) : base(face) { }
    public Face(DB.Document doc, DB.Reference reference) : base(doc, reference) { }

    Curve[] Curves
    {
      get
      {
        if (wires is null && IsValid)
        {
          wires = Value.GetEdgesAsCurveLoops().SelectMany(x => x.GetPreviewWires()).ToArray();

          if (Value.IsElementGeometry && Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToRhino().ChangeUnits(Revit.ModelUnits);
            foreach (var wire in wires)
              wire.Transform(xform);
          }
        }

        return wires;
      }
    }

    Mesh[] Meshes(MeshingParameters meshingParameters)
    {
      if (meshes is null && IsValid)
      {
        meshes = Enumerable.Repeat(Value, 1).GetPreviewMeshes(meshingParameters).ToArray();

        if (Value.IsElementGeometry && Document?.GetElement(Reference) is DB.Instance instance)
        {
          var xform = instance.GetTransform().ToRhino().ChangeUnits(Revit.ModelUnits);
          foreach (var mesh in meshes)
            mesh.Transform(xform);
        }

        foreach (var mesh in meshes)
          mesh.Normals.ComputeNormals();
      }

      return meshes;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(DB.Face)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is object)
      {
        var element = Reference is object ? Document?.GetElement(Reference) : null;

        if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
        {
          if (Value.ToRhino(true) is Brep brep)
          {
            if (element is DB.Instance instance)
              brep.Transform(Transform.Scale(Point3d.Origin, Revit.ModelUnits) * instance.GetTransform().ToRhino());
            else
              brep.Scale(Revit.ModelUnits);

            target = (Q) (object) new GH_Surface(brep);
          }
          else target = default;
          return true;
        }
        else if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
        {
          if (Value.ToRhino(false) is Brep brep)
          {
            if (element is DB.Instance instance)
              brep.Transform(Transform.Scale(Point3d.Origin, Revit.ModelUnits) * instance.GetTransform().ToRhino());
            else
              brep.Scale(Revit.ModelUnits);

            target = (Q) (object) new GH_Brep(brep);
          }
          else target = default;
          return true;
        }
        if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
        {
          if (Value.Triangulate()?.ToRhino() is Mesh mesh)
          {
            if (element is DB.Instance instance)
              mesh.Transform(Transform.Scale(Point3d.Origin, Revit.ModelUnits) * instance.GetTransform().ToRhino());
            else
              mesh.Scale(Revit.ModelUnits);

            mesh.Normals.ComputeNormals();

            target = (Q) (object) new GH_Mesh(mesh);
          }
          else target = default;
          return true;
        }
        else if (element is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElement(element);
          return true;
        }
      }

      return base.CastTo(ref target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      var bbox = BoundingBox.Empty;
      if (xform == Transform.Identity)
      {
        foreach (var curve in Curves)
          bbox.Union(curve.GetBoundingBox(true));
      }
      else
      {
        foreach (var curve in Curves)
          bbox.Union(curve.GetBoundingBox(xform));
      }

      return bbox;
    }

    #region IGH_PreviewData
    BoundingBox IGH_PreviewData.ClippingBox => GetBoundingBox(Transform.Identity);

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      foreach (var curve in Curves)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (!IsValid)
        return;

      foreach (var mesh in Meshes(args.MeshingParameters))
        args.Pipeline.DrawMeshShaded(mesh, args.Material);
    }
    #endregion
  }
}
