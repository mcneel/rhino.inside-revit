using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH
{
  public class AssemblyInfo : GH_AssemblyInfo
  {
    public AssemblyInfo() { }

    public override string Name => "Rhino.Inside for Autodesk Revit";
    public override string Version => $"{Assembly.GetName().Version.ToString()} (WIP)";
    public override string Description => "Provides live connection with Autodesk© Revit©";

    public override string AuthorName => "Robert McNeel & Associates";
    public override string AuthorContact => WebPageURI;

    public override GH_LibraryLicense License => Addin.IsExpired() ? GH_LibraryLicense.expired : GH_LibraryLicense.opensource;

    public static string ContactURI => @"https://discourse.mcneel.com/c/rhino-inside/Revit";
    public static string WebPageURI = @"https://www.rhino3d.com/inside/revit/";
  }
}
