using System;
using System.Reflection;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Rhino.Geometry;

using RhinoInside.Revit.UI;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;

namespace RhinoInside.Revit.Samples
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public class Sample1 : RhinoCommand
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<Sample1, NeedsActiveDocument<Availability>>("Sample 1");

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.ToolTip = "Creates a mesh sphere";
        pushButton.Image = ImageBuilder.BuildImage("1");
        pushButton.LargeImage = ImageBuilder.BuildLargeImage("1");
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://github.com/mcneel/rhino.inside-revit/tree/master#sample-1"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // RhinoCommon code
      var sphere = new Sphere(Point3d.Origin, 12 * Revit.ModelUnits);
      var brep = sphere.ToBrep();
      var meshes = Rhino.Geometry.Mesh.CreateFromBrep(brep, MeshingParameters.Default);

      // Revit code
      var uiApp = data.Application;
      var doc = uiApp.ActiveUIDocument.Document;

      using (var trans = new Transaction(doc, MethodBase.GetCurrentMethod().DeclaringType.FullName))
      {
        if (trans.Start() == TransactionStatus.Started)
        {
          var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);

          var ds = DirectShape.CreateElement(doc, categoryId);
          ds.Name = "Sphere";
            
          foreach (var shape in meshes.ConvertAll(ShapeEncoder.ToShape))
          {
            if (shape?.Length > 0)
              ds.AppendShape(shape);
          }

          trans.Commit();
        }
      }

      return Result.Succeeded;
    }
  }
}
