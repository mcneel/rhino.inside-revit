using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.InteropExtension;
using Autodesk.Revit.UI;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class GH_PersistentParam<T> : Grasshopper.Kernel.GH_PersistentParam<T>
    where T : class, IGH_Goo
  {
    protected override sealed Bitmap Icon => ((Bitmap) Properties.Resources.ResourceManager.GetObject(typeof(T).Name)) ??
                                             ImageBuilder.BuildIcon(IconTag);

    protected virtual string IconTag => typeof(T).Name.Substring(0, 1);

    protected GH_PersistentParam(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }
  }
}
