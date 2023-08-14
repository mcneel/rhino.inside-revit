using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.UI.Selection;

  public abstract class GeometryObject<X> : Reference<X>, IGH_PreviewObject
  where X : class, Types.IGH_GeometryObject
  {
    protected GeometryObject(string name, string nickname, string description, string category, string subcategory) :
    base(name, nickname, description, category, subcategory)
    { }

    #region IGH_PreviewObject
    bool IGH_PreviewObject.Hidden { get; set; }
    bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
    BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion

    protected virtual GH_GetterResult Prompt_One<T>(ref T value) where T : X => GH_GetterResult.cancel;
    protected virtual GH_GetterResult Prompt_Plural<T>(ref List<T> values) where T : X => GH_GetterResult.cancel;

    protected void Menu_PromptOne(Func<(X, GH_GetterResult)> getter)
    {
      try
      {
        PrepareForPrompt();
        var (data, result) = getter();
        if (result == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");
          PersistentData.Clear();
          if (data is object)
            PersistentData.Append(data);

          OnObjectChanged(GH_ObjectEventType.PersistentData);
        }
      }
      finally
      {
        RecoverFromPrompt();
        ExpireSolution(true);
      }
    }

    protected void Menu_PromptPlural<T>(object sender, EventArgs e) where T : X
    {
      try
      {
        PrepareForPrompt();
        var data = default(List<T>);
        if (Prompt_Plural(ref data) == GH_GetterResult.success)
        {
          RecordPersistentDataEvent("Change data");

          MutableNickName = true;
          if (Kind == GH_ParamKind.floating)
          {
            IconDisplayMode = GH_IconDisplayMode.application;
            Attributes?.ExpireLayout();
          }

          PersistentData.Clear();
          if (data is object)
            PersistentData.AppendRange(data.Cast<X>());

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

  public class GeometryObject : GeometryObject<Types.IGH_GeometryObject>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("7A41402E-7B6C-4523-9B57-E8485713F461");
    public GeometryObject() : base("Geometry", "Geometry", "Contains a collection of Revit geometry", "Params", "Revit") { }
    protected override string IconTag => string.Empty;

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override void Menu_AppendInternaliseData(ToolStripDropDown menu) { }

    protected override GH_GetterResult Prompt_Singular(ref Types.IGH_GeometryObject value) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Plural(ref List<Types.IGH_GeometryObject> values) => GH_GetterResult.cancel;
    #endregion
  }

  public class GeometryPoint : GeometryObject<Types.GeometryPoint>,
    ARUI.Selection.ISelectionFilter
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("BC1B160A-DC04-4139-AB7D-1AECBDE7FF88");
    public GeometryPoint() : base("Point", "Point", "Contains a collection of Revit points", "Params", "Revit") { }

    #region ISelectionFilter
    public virtual bool AllowElement(ARDB.Element elem) => true;
    public bool AllowReference(ARDB.Reference reference, ARDB.XYZ position) =>
      reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR;
    #endregion

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Box", "Point", "Line Style", "Category" }
    );

    //protected override GH_GetterResult Prompt_Plural(ref List<Types.IGH_PointGeometry> value)
    //{
    //  var uiDocument = Revit.ActiveUIDocument;
    //  if (uiDocument is null) return GH_GetterResult.cancel;

    //  switch (uiDocument.PickPoints(out var points))
    //  {
    //    case ARUI.Result.Succeeded:
    //      value = points.Select(x => (Types.IGH_PointGeometry) new Types.PointGeometry(uiDocument.Document, x)).ToList();
    //      return GH_GetterResult.success;

    //    case ARUI.Result.Cancelled:
    //      return GH_GetterResult.cancel;
    //  }

    //  // If PickPoints failed reset the Param content to Null.
    //  value = default;
    //  return GH_GetterResult.success;
    //}

    //protected override GH_GetterResult Prompt_Singular(ref Types.IGH_PointGeometry value)
    //{
    //  var uiDocument = Revit.ActiveUIDocument;
    //  if (uiDocument is null) return GH_GetterResult.cancel;

    //  switch (uiDocument.PickPoint(out var point))
    //  {
    //    case ARUI.Result.Succeeded:
    //      value = new Types.PointGeometry(uiDocument.Document, point);
    //      return GH_GetterResult.success;

    //    case ARUI.Result.Cancelled:
    //      return GH_GetterResult.cancel;
    //  }

    //  // If PickPoint failed reset the Param content to Null.
    //  value = default;
    //  return GH_GetterResult.success;
    //}

    protected override GH_GetterResult Prompt_Plural(ref List<Types.GeometryPoint> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObjects(out var references, ARUI.Selection.ObjectType.PointOnElement, this, "Click near a curve end, TAB for alternates, ESC quit."))
      {
        case ARUI.Result.Succeeded:
          value = references.Select(x => Types.GeometryPoint.FromReference(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;

        case ARUI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected override GH_GetterResult Prompt_Singular(ref Types.GeometryPoint value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObject(out var reference, ARUI.Selection.ObjectType.PointOnElement, this, "Click near a curve end, TAB for alternates, ESC quit."))
      {
        case ARUI.Result.Succeeded:
          value = Types.GeometryPoint.FromReference(uiDocument.Document, reference);
          return GH_GetterResult.success;

        case ARUI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    #endregion
  }

  public class GeometryCurve : GeometryObject<Types.GeometryCurve>,
    ARUI.Selection.ISelectionFilter
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("B79FD0FD-63AE-4776-A0A7-6392A3A58B0D");
    public GeometryCurve() : base("Curve", "Curve", "Contains a collection of Revit curves", "Params", "Revit") { }

    #region ISelectionFilter
    public virtual bool AllowElement(ARDB.Element elem) => true;
    public bool AllowReference(ARDB.Reference reference, ARDB.XYZ position) =>
      reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_LINEAR;
    #endregion

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Box", "Curve", "Line Style", "Category" }
    );

    protected override GH_GetterResult Prompt_Plural(ref List<Types.GeometryCurve> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObjects(out var references, ARUI.Selection.ObjectType.PointOnElement, this))
      {
        case ARUI.Result.Succeeded:
          value = references.Select(x => Types.GeometryCurve.FromReference(uiDocument.Document, x)).ToList();
          return GH_GetterResult.success;

        case ARUI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }

    protected override GH_GetterResult Prompt_Singular(ref Types.GeometryCurve value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObject(out var reference, ARUI.Selection.ObjectType.PointOnElement, this))
      {
        case ARUI.Result.Succeeded:
          value = Types.GeometryCurve.FromReference(uiDocument.Document, reference);
          return GH_GetterResult.success;

        case ARUI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    #endregion
  }

  public class GeometryFace : GeometryObject<Types.GeometryFace>,
    ARUI.Selection.ISelectionFilter
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override Guid ComponentGuid => new Guid("759700ED-BC79-4986-A6AB-84921A7C9293");
    public GeometryFace() : base("Face", "Face", "Contains a collection of Revit faces", "Params", "Revit") { }

    #region ISelectionFilter
    public virtual bool AllowElement(ARDB.Element elem) => true;
    public bool AllowReference(ARDB.Reference reference, ARDB.XYZ position) =>
      reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_SURFACE;
    #endregion

    #region UI methods
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Box", "Surface", "Brep", "Mesh", "Line Style", "Category", "Material" }
    );

    static ARDB.Reference FixReference(ARDB.Document document, ARDB.Reference reference)
    {
      if (reference.ElementReferenceType == ARDB.ElementReferenceType.REFERENCE_TYPE_NONE)
      {
        // For some reason non visible faces do not have the ":SURFACE" sufix.
        var representation = reference.ConvertToStableRepresentation(document);
        if (!representation.EndsWith(":SURFACE")) representation += ":SURFACE";
        reference = ARDB.Reference.ParseFromStableRepresentation(document, representation);
      }

      return reference;
    }

    protected override GH_GetterResult Prompt_Plural(ref List<Types.GeometryFace> value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObjects(out var references, ARUI.Selection.ObjectType.PointOnElement, this))
      {
        case ARUI.Result.Succeeded:
          value = references.Select(x => new Types.GeometryFace(uiDocument.Document, FixReference(uiDocument.Document, x))).ToList();
          return GH_GetterResult.success;

        case ARUI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Types.GeometryFace value)
    {
      var uiDocument = Revit.ActiveUIDocument;
      if (uiDocument is null) return GH_GetterResult.cancel;

      switch (uiDocument.PickObject(out var reference, ARUI.Selection.ObjectType.PointOnElement, this))
      {
        case ARUI.Result.Succeeded:
          value = new Types.GeometryFace(uiDocument.Document, FixReference(uiDocument.Document, reference));
          return GH_GetterResult.success;

        case ARUI.Result.Cancelled:
          return GH_GetterResult.cancel;
      }

      // If PickObject failed reset the Param content to Null.
      value = default;
      return GH_GetterResult.success;
    }
    #endregion
  }
}
