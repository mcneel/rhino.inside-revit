using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using OS = System.Environment;

namespace RhinoInside.Revit.Setup
{
  public static partial class Actions
  {
    [CustomAction]
    public static ActionResult WIPExpirationDateWarning(Session session)
    {
      var buildVersion = new Version(session["ProductVersion"]).Build;
      var expirationDate = new DateTime(2000, 1, 1).AddDays(buildVersion + int.Parse(session["ProductExpirationPeriod"]));
      var daysLeft = (expirationDate - DateTime.Now).Days;

      var record = new Record(1);
      if (daysLeft < 0)
      {
        record.FormatString = "This Work-In-Progress installer expired on" + OS.NewLine +
                              $"{expirationDate:D}." + OS.NewLine + OS.NewLine +
                              "Please check for updates at 'https://www.rhino3d.com/inside/revit/'";

        session.Message(InstallMessage.Error, record);
        return ActionResult.Failure;
      }

      if (daysLeft == 0)
      {
        record.FormatString = "This Work-In-Progress installer expired today." + OS.NewLine + OS.NewLine +
                              "Please check for updates at 'https://www.rhino3d.com/inside/revit/'";

        session.Message(InstallMessage.Warning, record);
      }

      if (daysLeft > 0)
      {
        record.FormatString = $"This installer contains a Work-In-Progress version of {session["ProductName"]}." + OS.NewLine + OS.NewLine +
                              $"It will expire on {expirationDate:D}," + OS.NewLine +
                              $"{daysLeft} days from now.";

        session.Message(InstallMessage.User, record);
      }

      return ActionResult.Success;
    }
  }
}
