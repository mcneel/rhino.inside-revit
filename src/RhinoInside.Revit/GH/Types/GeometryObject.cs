using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI.Selection;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.UI.Selection;

namespace RhinoInside.Revit.GH.Types
{
  public class GeometricElement :
    Element,
    IGH_GeometricGoo,
    IGH_PreviewData,
    IGH_PreviewMeshData
  {
    public override string TypeName => "Revit Geometric element";
    public override string TypeDescription => "Represents a Revit geometric element";

    public override string DisplayName
    {
      get
      {
        var element = (DB.Element) this;
        if (element is object)
        {
          if (element.get_Parameter(DB.BuiltInParameter.ALL_MODEL_MARK) is DB.Parameter parameter && parameter.HasValue)
          {
            var mark = parameter.AsString();
            if(!string.IsNullOrEmpty(mark))
              return $"{base.DisplayName} [{mark}]";
          }
        }

        return base.DisplayName;
      }
    }

    public GeometricElement() { }
    public GeometricElement(DB.Element element) : base(element) { }
    protected override bool SetValue(DB.Element element) => IsValidElement(element) ? base.SetValue(element) : false;
    public static bool IsValidElement(DB.Element element)
    {
      return
      (
        element is DB.DirectShape ||
        element is DB.CurveElement ||
        element is DB.CombinableElement ||
        element is DB.Architecture.TopographySurface ||
        (element.Category is object && element.CanHaveTypeAssigned())
      );
    }
    #region Preview
    public static void BuildPreview
    (
      DB.Element element, MeshingParameters meshingParameters, DB.ViewDetailLevel DetailLevel,
      out Rhino.Display.DisplayMaterial[] materials, out Mesh[] meshes, out Curve[] wires
    )
    {
      DB.Options options = null;
      using (var geometry = element?.GetGeometry(DetailLevel == DB.ViewDetailLevel.Undefined ? DB.ViewDetailLevel.Medium : DetailLevel, out options)) using (options)
      {
        if (geometry is null)
        {
          materials = null;
          meshes = null;
          wires = null;
        }
        else
        {
          var categoryMaterial = element.Category?.Material.ToRhino(null);
          var elementMaterial = geometry.MaterialElement.ToRhino(categoryMaterial);

          meshes = geometry.GetPreviewMeshes(meshingParameters).Where(x => x is object).ToArray();
          wires = geometry.GetPreviewWires().Where(x => x is object).ToArray();
          materials = geometry.GetPreviewMaterials(element.Document, elementMaterial).Where(x => x is object).ToArray();

          foreach (var mesh in meshes)
            mesh.Normals.ComputeNormals();
        }
      }
    }

    class Preview : IDisposable
    {
      readonly GeometricElement geometricElement;
      readonly BoundingBox clippingBox;
      public readonly MeshingParameters MeshingParameters;
      public Rhino.Display.DisplayMaterial[] materials;
      public Mesh[] meshes;
      public Curve[] wires;

      static List<Preview> previewsQueue;

      void Build()
      {
        if ((meshes is null && wires is null && materials is null))
        {
          var element = geometricElement.Document.GetElement(geometricElement.Id);
          if (element is null)
            return;

          BuildPreview(element, MeshingParameters, DB.ViewDetailLevel.Undefined, out materials, out meshes, out wires);
        }
      }

      static void BuildPreviews(DB.Document _, bool cancelled)
      {
        var previews = previewsQueue;
        previewsQueue = null;

        if (cancelled)
          return;

        // Sort in reverse order depending on how 'big' is the element on screen.
        // The bigger the more at the end on the list.
        previews.Sort((x, y) => (x.clippingBox.Diagonal.Length < y.clippingBox.Diagonal.Length) ? -1 : +1);
        BuildPreviews(cancelled, previews);
      }

      static void BuildPreviews(bool cancelled, List<Preview> previews)
      {
        if (cancelled)
          return;

        var stopWatch = new Stopwatch();

        int count = 0;
        while ((count = previews.Count) > 0)
        {
          // Draw the biggest elements first.
          // The biggest element is at the end of previews List, this way no realloc occurs when removing it

          int last = count - 1;
          var preview = previews[last];
          previews.RemoveAt(last);

          stopWatch.Start();
          preview.Build();
          stopWatch.Stop();

          // If building those previews take use more than 200 ms we return to Revit, to keep it 'interactive'.
          if (stopWatch.ElapsedMilliseconds > 200)
            break;
        }

        // RhinoDoc.ActiveDoc.Views.Redraw is synchronous :(
        // better use RhinoView.Redraw that just invalidate the view, the OS will update it when possible
        foreach (var view in Rhino.RhinoDoc.ActiveDoc.Views)
          view.Redraw();

        // If there are pending previews to generate enqueue BuildPreviews again
        if (previews.Count > 0)
          Revit.EnqueueReadAction((_, cancel) => BuildPreviews(cancel, previews));
      }

      Preview(GeometricElement element)
      {
        geometricElement = element;
        clippingBox = element.ClippingBox;
        MeshingParameters = element.meshingParameters;
      }

      public static Preview OrderNew(GeometricElement element)
      {
        if (!element.IsValid)
          return null;

        if (previewsQueue is null)
        {
          previewsQueue = new List<Preview>();
          Revit.EnqueueReadAction((doc, cancel) => BuildPreviews(doc, cancel));
        }

        var preview = new Preview(element);
        previewsQueue.Add(preview);
        return preview;
      }

      void IDisposable.Dispose()
      {
        foreach (var mesh in meshes ?? Enumerable.Empty<Mesh>())
          mesh.Dispose();
        meshes = null;

        foreach (var wire in wires ?? Enumerable.Empty<Curve>())
          wire.Dispose();
        wires = null;
      }
    }

    MeshingParameters meshingParameters;
    Preview geometryPreview;
    Preview GeometryPreview
    {
      get { return geometryPreview ?? (geometryPreview = Preview.OrderNew(this)); }
      set { if (geometryPreview != value) { ((IDisposable) geometryPreview)?.Dispose(); geometryPreview = value; } }
    }

    public Rhino.Display.DisplayMaterial[] TryGetPreviewMaterials()
    {
      return GeometryPreview.materials;
    }

    public Mesh[] TryGetPreviewMeshes(MeshingParameters parameters = default)
    {
      if (parameters is object && !ReferenceEquals(meshingParameters, parameters))
      {
        meshingParameters = parameters;
        if (geometryPreview is object)
        {
          if (geometryPreview.MeshingParameters?.RelativeTolerance != meshingParameters.RelativeTolerance)
            GeometryPreview = null;
        }
      }

      return GeometryPreview.meshes;
    }

    public Curve[] TryGetPreviewWires()
    {
      return GeometryPreview.wires;
    }
    #endregion

    #region IGH_GeometricGoo
    public BoundingBox Boundingbox => ClippingBox;
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedElement;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsElementLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadElement();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public BoundingBox GetBoundingBox(Transform xform) => ClippingBox;
    bool IGH_GeometricGoo.LoadGeometry() => IsElementLoaded || LoadElement();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsElementLoaded || LoadElement();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_PreviewData
    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (!IsValid)
        return;

      var meshes = TryGetPreviewMeshes(args.MeshingParameters);
      if (meshes is null)
        return;

      var material = args.Material;
      var element = Document?.GetElement(Id);
      if (element is null)
      {
        const int factor = 3;

        // Erased element
        material = new Rhino.Display.DisplayMaterial(material)
        {
          Diffuse = System.Drawing.Color.FromArgb(20, 20, 20),
          Emission = System.Drawing.Color.FromArgb(material.Emission.R / factor, material.Emission.G / factor, material.Emission.B / factor),
          Shine = 0.0,
        };
      }
      else if (!element.Pinned)
      {
        if (args.Pipeline.DisplayPipelineAttributes.ShadingEnabled)
        {
          // Unpinned element
          if (args.Pipeline.DisplayPipelineAttributes.UseAssignedObjectMaterial)
          {
            var materials = TryGetPreviewMaterials();

            for (int m = 0; m < meshes.Length; ++m)
              args.Pipeline.DrawMeshShaded(meshes[m], materials[m]);

            return;
          }
          else
          {
            material = new Rhino.Display.DisplayMaterial(material)
            {
              Diffuse = element.Category?.LineColor.ToRhino() ?? System.Drawing.Color.White,
              Transparency = 0.0
            };

            if (material.Diffuse == System.Drawing.Color.Black)
              material.Diffuse = System.Drawing.Color.White;
          }
        }
      }

      foreach (var mesh in meshes)
        args.Pipeline.DrawMeshShaded(mesh, material);
    }

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if (!args.Pipeline.DisplayPipelineAttributes.ShowSurfaceEdges)
        return;

      int thickness = 1; //args.Thickness;
      const int factor = 3;

      var color = args.Color;
      var element = Document?.GetElement(Id);
      if (element is null)
      {
        // Erased element
        color = System.Drawing.Color.FromArgb(args.Color.R / factor, args.Color.G / factor, args.Color.B / factor);
      }
      else if (!element.Pinned)
      {
        // Unpinned element
        if (args.Thickness <= 1 && args.Pipeline.DisplayPipelineAttributes.UseAssignedObjectMaterial)
          color = System.Drawing.Color.Black;
      }

      var wires = TryGetPreviewWires();
      if (wires is object && wires.Length > 0)
      {
        foreach (var wire in wires)
          args.Pipeline.DrawCurve(wire, color, thickness);
      }
      else
      {
        var meshes = TryGetPreviewMeshes();
        if (meshes is object)
        {
          // Grasshopper does not show mesh wires.
          //foreach (var mesh in meshes)
          //  args.Pipeline.DrawMeshWires(mesh, color, thickness);
        }
        else
        {
          foreach (var edge in ClippingBox.GetEdges() ?? Enumerable.Empty<Line>())
            args.Pipeline.DrawPatternedLine(edge.From, edge.To, System.Drawing.Color.Black /*color*/, 0x00001111, thickness);
        }
      }
    }
    #endregion

    #region IGH_PreviewMeshData
    void IGH_PreviewMeshData.DestroyPreviewMeshes()
    {
      GeometryPreview = null;
      clippingBox = BoundingBox.Empty;
    }

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes()
    {
      return TryGetPreviewMeshes();
    }
    #endregion
  }

  public abstract class GeometryObject<X> :
    GH_Goo<X>,
    IGH_ElementId,
    IGH_GeometricGoo,
    IGH_PreviewMeshData
    where X : DB.GeometryObject
  {
    public override string TypeName => "Revit GeometryObject";
    public override string TypeDescription => "Represents a Revit GeometryObject";
    public override bool IsValid => !(Value is null);
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();
    protected virtual Type ScriptVariableType => typeof(X);

    #region IGH_ElementId
    public DB.Reference Reference { get; protected set; }
    public DB.Document Document { get; protected set; }
    public DB.ElementId Id => Reference?.ElementId;
    public Guid DocumentGUID { get; private set; } = Guid.Empty;
    public string UniqueID { get; protected set; } = string.Empty;
    public bool IsReferencedElement => !string.IsNullOrEmpty(UniqueID);
    public bool IsElementLoaded => !(Value is default(X));
    public virtual bool LoadElement()
    {
      if (Document is null)
      {
        Value = null;
        if (!Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
        {
          Document = null;
          return false;
        }

        Document = doc;
      }
      else if (IsElementLoaded)
        return true;

      if (Document is object)
      {
        try
        {
          Reference = Reference ?? DB.Reference.ParseFromStableRepresentation(Document, UniqueID);
          var element = Document.GetElement(Reference);
          m_value = element?.GetGeometryObjectFromReference(Reference) as X;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }

        return IsElementLoaded;
      }

      return false;
    }
    public void UnloadElement() { Value = default; Document = default; }
    #endregion

    #region IGH_GeometricGoo
    BoundingBox IGH_GeometricGoo.Boundingbox => GetBoundingBox(Transform.Identity);
    Guid IGH_GeometricGoo.ReferenceID
    {
      get => Guid.Empty;
      set { if (value != Guid.Empty) throw new InvalidOperationException(); }
    }
    bool IGH_GeometricGoo.IsReferencedGeometry => IsReferencedElement;
    bool IGH_GeometricGoo.IsGeometryLoaded => IsElementLoaded;

    void IGH_GeometricGoo.ClearCaches() => UnloadElement();
    IGH_GeometricGoo IGH_GeometricGoo.DuplicateGeometry() => (IGH_GeometricGoo) MemberwiseClone();
    public abstract BoundingBox GetBoundingBox(Transform xform);
    bool IGH_GeometricGoo.LoadGeometry(                  ) => IsElementLoaded || LoadElement();
    bool IGH_GeometricGoo.LoadGeometry(Rhino.RhinoDoc doc) => IsElementLoaded || LoadElement();
    IGH_GeometricGoo IGH_GeometricGoo.Transform(Transform xform) => null;
    IGH_GeometricGoo IGH_GeometricGoo.Morph(SpaceMorph xmorph) => null;
    #endregion

    #region IGH_Goo
    public override sealed string ToString()
    {
      if (!IsValid)
        return "Null " + TypeName;

      try
      {
        string typeName = TypeName;
        if (Document?.GetElement(Reference) is DB.DisplacementElement element)
        {
          typeName = "Referenced ";
          switch (Reference.ElementReferenceType)
          {
            case DB.ElementReferenceType.REFERENCE_TYPE_NONE: typeName += "geometry"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_LINEAR: typeName += "edge"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_SURFACE: typeName += "face"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_FOREIGN: typeName += "external geometry"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_INSTANCE: typeName += "instance"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_CUT_EDGE: typeName += "trim"; break;
            case DB.ElementReferenceType.REFERENCE_TYPE_MESH: typeName += "mesh"; break;
#if REVIT_2018
            case DB.ElementReferenceType.REFERENCE_TYPE_SUBELEMENT: typeName += "subelement"; break;
#endif
          }

          typeName += " at Revit " + element.GetType().Name + " \"" + element.Name + "\"";
        }

#if DEBUG
        typeName += " (" + Reference.ConvertToStableRepresentation(Document) + ")";
#endif
        return typeName;
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException)
      {
        return "Invalid" + TypeName;
      }
    }

    public override sealed bool Read(GH_IReader reader)
    {
      Value = null;
      Document = null;

      var documentGUID = Guid.Empty;
      reader.TryGetGuid("DocumentGUID", ref documentGUID);
      DocumentGUID = documentGUID;

      string uniqueID = string.Empty;
      reader.TryGetString("UniqueID", ref uniqueID);
      UniqueID = uniqueID;

      return true;
    }

    public override sealed bool Write(GH_IWriter writer)
    {
      if (DocumentGUID != Guid.Empty)
        writer.SetGuid("DocumentGUID", DocumentGUID);

      if(!string.IsNullOrEmpty(UniqueID))
        writer.SetString("UniqueID", UniqueID);

      return true;
    }
    #endregion

    #region IGH_PreviewMeshData
    protected Point   point = null;
    protected Curve[] wires = null;
    protected Mesh[]  meshes = null;

    void IGH_PreviewMeshData.DestroyPreviewMeshes()
    {
      point = null;
      wires = null;
      meshes = null;
    }

    Mesh[] IGH_PreviewMeshData.GetPreviewMeshes() => meshes;
    #endregion

    protected GeometryObject() { }
    protected GeometryObject(X data) : base(data) { }
    protected GeometryObject(DB.Document doc, DB.Reference reference)
    {
      DocumentGUID = doc.GetFingerprintGUID();
      UniqueID = reference.ConvertToStableRepresentation(doc);
    }
  }

  public class Vertex : GeometryObject<DB.Point>, IGH_PreviewData
  {
    public override string TypeName => "Revit Vertex";
    public override string TypeDescription => "Represents a Revit Vertex";

    readonly int VertexIndex = -1;
    public override bool LoadElement()
    {
      Document = default;
      Value = default;

      if (Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
      {
        Document = doc;

        try
        {
          Reference = DB.Reference.ParseFromStableRepresentation(doc, UniqueID);
          var element = doc.GetElement(Reference);
          var geometry = element?.GetGeometryObjectFromReference(Reference);
          if (geometry is DB.Edge edge)
          {
            var curve = edge.AsCurve();
            var points = new DB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
            Value = DB.Point.Create(points[VertexIndex]);
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }

      return IsValid;
    }

    public Vertex() { }
    public Vertex(DB.Point data) : base(data) { }
    public Vertex(DB.Document doc, DB.Reference reference, int index) : base(doc, reference) { VertexIndex = index; }

    Point Point
    {
      get
      {
        if (point is null && IsValid)
        {
          point = new Point(Value.Coord.ToRhino().ChangeUnits(Revit.ModelUnits));

          if(/*Value.IsElementGeometry && */Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToRhino().ChangeUnits(Revit.ModelUnits);
            point.Transform(xform);
          }
        }

        return point;
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is GH_Point point)
      {
        Value = DB.Point.Create(point.Value.ChangeUnits(1.0 / Revit.ModelUnits).ToHost());
        UniqueID = string.Empty;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(DB.Point)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is object)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
        {
          target = (Q) (object) new GH_Point(Point.Location);
          return true;
        }
        else if (Reference is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElementId(Document, Id);
          return true;
        }
      }

      return base.CastTo(ref target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      return xform == Transform.Identity ?
        Point.GetBoundingBox(true) :
        Point.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    BoundingBox IGH_PreviewData.ClippingBox => GetBoundingBox(Transform.Identity);

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if (Point is Point point)
        args.Pipeline.DrawPoint(point.Location, CentralSettings.PreviewPointStyle, CentralSettings.PreviewPointRadius, args.Color);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion
  }

  public class Edge : GeometryObject<DB.Edge>, IGH_PreviewData
  {
    public override string TypeName => "Revit Edge";
    public override string TypeDescription => "Represents a Revit Edge";

    public Edge() { }
    public Edge(DB.Edge edge) : base(edge) { }
    public Edge(DB.Document doc, DB.Reference reference) : base(doc, reference) { }

    Curve Curve
    {
      get
      {
        if (wires is null && IsValid)
        {
          wires = Enumerable.Repeat(Value, 1).GetPreviewWires().ToArray();

          if (Value.IsElementGeometry && Document?.GetElement(Reference) is DB.Instance instance)
          {
            var xform = instance.GetTransform().ToRhino().ChangeUnits(Revit.ModelUnits);
            foreach (var wire in wires)
              wire.Transform(xform);
          }
        }

        return wires.FirstOrDefault();
      }
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Reference)))
      {
        target = (Q) (object) (IsValid ? Reference : null);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(DB.Edge)))
      {
        target = (Q) (object) (IsValid ? Value : null);
        return true;
      }
      else if (Value is object)
      {
        if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
        {
          target = (Q) (object) new GH_Curve(Curve);
          return true;
        }
        else if (Reference is object && typeof(Q).IsAssignableFrom(typeof(Element)))
        {
          target = (Q) (object) Element.FromElementId(Document, Id);
          return true;
        }
      }

      return base.CastTo(ref target);
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is null)
        return BoundingBox.Empty;

      return xform == Transform.Identity ?
        Curve.GetBoundingBox(true) :
        Curve.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    BoundingBox IGH_PreviewData.ClippingBox => GetBoundingBox(Transform.Identity);

    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid)
        return;

      if(Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion
  }

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

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class ElementIdGeometryParam<X, R> : ElementIdParam<X, R>, IGH_PreviewObject
    where X : class, Types.IGH_ElementId, IGH_PreviewData
  {
    protected ElementIdGeometryParam(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory) { }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
  }

  public abstract class GeometricElementT<T, R> :
    ElementIdGeometryParam<T, R>,
    ISelectionFilter
    where T : Types.GeometricElement, new()
  {
    protected GeometricElementT(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    #region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalElementMenuItems(menu);
      Menu_AppendItem(menu, $"Highlight {GH_Convert.ToPlural(TypeName)}", Menu_HighlightElements, !VolatileData.IsEmpty, false);
    }

    void Menu_HighlightElements(object sender, EventArgs e)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var elementIds = ToElementIds(VolatileData).
                       Where(x => x.Document.Equals(uiDocument.Document)).
                       Select(x => x.Id);

      if (elementIds.Any())
      {
        var ids = elementIds.ToArray();

        uiDocument.Selection.SetElementIds(ids);
        uiDocument.ShowElements(ids);
      }
    }
    #endregion

    public virtual bool AllowElement(DB.Element elem) => elem is R;
    public bool AllowReference(DB.Reference reference, DB.XYZ position)
    {
      if (reference.ElementReferenceType == DB.ElementReferenceType.REFERENCE_TYPE_NONE)
        return AllowElement(Revit.ActiveUIDocument.Document.GetElement(reference));

      return false;
    }

    protected override GH_GetterResult Prompt_Singular(ref T value)
    {
#if REVIT_2018
      const ObjectType objectType = ObjectType.Subelement;
#else
      const ObjectType objectType = ObjectType.Element;
#endif

      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, objectType, this))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = (T) Types.Element.FromElementId(uiDocument.Document, reference.ElementId);
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected GH_GetterResult Prompt_SingularLinked(ref GH_Structure<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;

      switch (uiDocument.PickObject(out var reference, ObjectType.LinkedElement, this))
      {
      case Autodesk.Revit.UI.Result.Succeeded:
        value = new GH_Structure<T>();

        if (doc.GetElement(reference.ElementId) is DB.RevitLinkInstance instance)
        {
          var linkedDoc = instance.GetLinkDocument();
          var element = (T) Types.Element.FromElementId(linkedDoc, reference.LinkedElementId);
          value.Append(element, new GH_Path(0));

          return GH_GetterResult.success;
        }
        break;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected override GH_GetterResult Prompt_Plural(ref List<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;
      var selection = uiDocument.Selection.GetElementIds();
      if (selection?.Count > 0)
      {
        value = selection.Select(id => doc.GetElement(id)).Where(element => AllowElement(element)).
                Select(element => (T) Types.Element.FromElementId(element.Document, element.Id)).ToList();

        return GH_GetterResult.success;
      }
      else
      {
        switch (uiDocument.PickObjects(out var references, ObjectType.Element, this))
        {
          case Autodesk.Revit.UI.Result.Succeeded:
            value = references.Select(r => (T) Types.Element.FromElementId(doc, r.ElementId)).ToList();
            return GH_GetterResult.success;
          case Autodesk.Revit.UI.Result.Cancelled:
            return GH_GetterResult.cancel;
        }
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected GH_GetterResult Prompt_PluralLinked(ref GH_Structure<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;

      switch(uiDocument.PickObjects(out var references, ObjectType.LinkedElement, this))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          var groups = references.Select
          (
            r =>
            {
              var instance = doc.GetElement(r.ElementId) as DB.RevitLinkInstance;
              var linkedDoc = instance.GetLinkDocument();

              return (T) Types.Element.FromElementId(linkedDoc, r.LinkedElementId);
            }
          ).GroupBy(x => x.Document);

          int index = 0;
          foreach (var group in groups)
            value.AppendRange(group, new GH_Path(index));

          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected GH_GetterResult Prompt_More(ref GH_Structure<T> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      var doc = uiDocument.Document;
      var docGUID = doc.GetFingerprintGUID();

      var documents = value.AllData(true).OfType<T>().GroupBy(x => x.DocumentGUID);
      var activeElements = documents.Where(x => x.Key == docGUID).
                           SelectMany(x => x).
                           Select
                           (
                             x =>
                             {
                               try { return DB.Reference.ParseFromStableRepresentation(doc, x.UniqueID); }
                               catch (Autodesk.Revit.Exceptions.ArgumentException) { return null; }
                             }
                           ).
                           Where(x => x is object).
                           ToArray();

      switch(uiDocument.PickObjects(out var references, ObjectType.Element, this, null, activeElements))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new GH_Structure<T>();

          int index = 0;
          foreach (var document in documents)
          {
            if (document.Key == docGUID)
              continue;

            var path = new GH_Path(index++);
            value.AppendRange(document, path);
          }

          value.AppendRange(references.Select(r => (T) Types.Element.FromElementId(doc, r.ElementId)), new GH_Path(index));
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      base.Menu_AppendPromptOne(menu);

      Menu_AppendItem(menu, $"Set one linked {TypeName}", Menu_PromptOneLinked, SourceCount == 0, false);
    }

    protected override void Menu_AppendPromptMore(ToolStripDropDown menu)
    {
      var name_plural = GH_Convert.ToPlural(TypeName);

      base.Menu_AppendPromptMore(menu);

      Menu_AppendItem(menu, $"Set Multiple linked {name_plural}", Menu_PromptPluralLinked, SourceCount == 0);
      Menu_AppendSeparator(menu);

      Menu_AppendItem(menu, $"Change {name_plural} collection", Menu_PromptMore, SourceCount == 0, false);
    }

    private void Menu_PromptOneLinked(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = PersistentData;
        if (Prompt_SingularLinked(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          if (data is object)
            PersistentData.MergeStructure(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }

    private void Menu_PromptPluralLinked(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = PersistentData;
        if (Prompt_PluralLinked(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          if (data is object)
            PersistentData.MergeStructure(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }

    private void Menu_PromptMore(object sender, EventArgs e)
    {
      try
      {
        PrepareForPrompt();
        var data = PersistentData;
        if (Prompt_More(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          PersistentData.Clear();
          if (data is object)
            PersistentData.MergeStructure(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }
  }

  public class GeometricElement : GeometricElementT<Types.GeometricElement, DB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("EF607C2A-2F44-43F4-9C39-369CE114B51F");

    public GeometricElement() : base("Geometric Element", "Geometric Element", "Represents a Revit document geometric element.", "Params", "Revit") { }

    protected override Types.GeometricElement PreferredCast(object data)
    {
      return data is DB.Element element && AllowElement(element) ?
             new Types.GeometricElement(element) :
             null;
    }

    public override bool AllowElement(DB.Element elem) => Types.GeometricElement.IsValidElement(elem);
  }

  public class Vertex : ElementIdGeometryParam<Types.Vertex, DB.Point>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("BC1B160A-DC04-4139-AB7D-1AECBDE7FF88");
    public Vertex() : base("Vertex", "Vertex", "Represents a Revit vertex.", "Params", "Revit") { }

#region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Vertex> value)
    {
      using (new ModalForm.EditScope())
      {
        var values = new List<Types.Vertex>();
        Types.Vertex vertex = null;
        while(Prompt_Singular(ref vertex) == GH_GetterResult.success)
          values.Add(vertex);

        if (values.Count > 0)
        {
          value = values;
          return GH_GetterResult.success;
        }
      }

      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Vertex value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge, "Click on an edge near an end to select a vertex, TAB for alternates, ESC quit."))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          var element = uiDocument.Document.GetElement(reference);
          if (element?.GetGeometryObjectFromReference(reference) is DB.Edge edge)
          {
            var curve = edge.AsCurve();
            var result = curve.Project(reference.GlobalPoint);
            var points = new DB.XYZ[] { curve.GetEndPoint(0), curve.GetEndPoint(1) };
            int index = result.XYZPoint.DistanceTo(points[0]) < result.XYZPoint.DistanceTo(points[1]) ? 0 : 1;

            value = new Types.Vertex(uiDocument.Document, reference, index);
            return GH_GetterResult.success;
          }
          break;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
#endregion
  }

  public class Edge : ElementIdGeometryParam<Types.Edge, DB.Edge>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("B79FD0FD-63AE-4776-A0A7-6392A3A58B0D");
    public Edge() : base("Edge", "Edge", "Represents a Revit edge.", "Params", "Revit") { }

#region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Edge> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch(uiDocument.PickObjects(out var references, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Edge(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Edge value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Edge))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Edge(uiDocument.Document, reference);
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
#endregion
  }

  public class Face : ElementIdGeometryParam<Types.Face, DB.Face>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("759700ED-BC79-4986-A6AB-84921A7C9293");
    public Face() : base("Face", "Face", "Represents a Revit face.", "Params", "Revit") { }

#region UI methods
    public override void AppendAdditionalElementMenuItems(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Face> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObjects(out var references, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = references.Select((x) => new Types.Face(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.Face value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      switch (uiDocument.PickObject(out var reference, ObjectType.Face))
      {
        case Autodesk.Revit.UI.Result.Succeeded:
          value = new Types.Face(uiDocument.Document, reference);
          return GH_GetterResult.success;
        case Autodesk.Revit.UI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
#endregion
  }
}
