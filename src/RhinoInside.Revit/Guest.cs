using System;

namespace RhinoInside.Revit
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
  internal class GuestPlugInIdAttribute : Attribute
  {
    public readonly Guid PlugInId;
    public GuestPlugInIdAttribute(string plugInId) => PlugInId = Guid.Parse(plugInId);
  }

  internal class CheckInArgs : EventArgs
  {
    public string Message { get; set; } = string.Empty;
    public bool ShowMessage { get; set; } = true;
  }

  internal class CheckOutArgs : EventArgs
  {
    public string Message { get; set; } = string.Empty;
    public bool ShowMessage { get; set; } = true;
  }

  internal enum GuestResult
  {
    Failed = int.MinValue,
    Cancelled = int.MinValue + 1,
    Nothing = 0,
    Succeeded = 1
  }

  internal interface IGuest
  {
    string Name { get; }

    GuestResult EntryPoint(object sender, EventArgs args);
  }
}
