using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
  public static class TaskDialogIcons
  {
    public const TaskDialogIcon IconNone        = TaskDialogIcon.TaskDialogIconNone;
#if REVIT_2018
    public const TaskDialogIcon IconShield      = TaskDialogIcon.TaskDialogIconShield;
    public const TaskDialogIcon IconInformation = TaskDialogIcon.TaskDialogIconInformation;
    public const TaskDialogIcon IconError       = TaskDialogIcon.TaskDialogIconError;
#else
    public const TaskDialogIcon IconShield      = TaskDialogIcon.TaskDialogIconWarning;
    public const TaskDialogIcon IconInformation = TaskDialogIcon.TaskDialogIconWarning;
    public const TaskDialogIcon IconError       = TaskDialogIcon.TaskDialogIconWarning;
#endif
    public const TaskDialogIcon IconWarning     = TaskDialogIcon.TaskDialogIconWarning;
  }
}
