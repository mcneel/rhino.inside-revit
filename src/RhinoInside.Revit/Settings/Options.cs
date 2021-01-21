using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RhinoInside.Revit.Settings
{
  [Serializable]
  static class AddinOptions
  {
    [XmlAttribute]
    public static bool CheckForUpdatesOnStartup { get; set; } = true;

    [XmlAttribute]
    public static string UpdateChannel { get; set; } = AddinUpdater.DefaultChannel.Id.ToString();

    static void SaveOptions()
    {

    }
  }
}
