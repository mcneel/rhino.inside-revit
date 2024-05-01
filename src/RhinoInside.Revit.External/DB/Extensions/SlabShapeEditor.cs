using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SlabShapeEditorExtension
  {
#if !REVIT_2025
    public static SlabShapeVertex AddPoint(this SlabShapeEditor editor, XYZ location) => editor.DrawPoint(location);
    public static SlabShapeCreaseArray AddSplitLine(this SlabShapeEditor editor, SlabShapeVertex startVertex, SlabShapeVertex endVertex) => editor.DrawSplitLine(startVertex, endVertex);
#endif
  }
}
