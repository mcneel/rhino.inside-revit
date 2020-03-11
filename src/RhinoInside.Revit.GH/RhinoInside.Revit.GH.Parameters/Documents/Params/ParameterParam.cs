using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents.Params
{
  public class ParameterParam : ParameterValue
  {
    public override Guid ComponentGuid => new Guid("43F0E4E9-3DC4-4965-AB80-07E28E203A91");

    public ParameterParam() : base(string.Empty, string.Empty, string.Empty, "Params", "Revit", GH_ParamAccess.item) { }
    public ParameterParam(DB.Parameter p) : this()
    {
      ParameterName = p.Definition.Name;
      ParameterType = p.Definition.ParameterType;
      ParameterGroup = p.Definition.ParameterGroup;
      ParameterBinding = p.Element is DB.ElementType ? RevitAPI.ParameterBinding.Type : RevitAPI.ParameterBinding.Instance;

      if (p.IsShared)
        ParameterSharedGUID = p.GUID;
      else if (p.Id.TryGetBuiltInParameter(out var parameterBuiltInId))
        ParameterBuiltInId = parameterBuiltInId;

      try { Name = $"{DB.LabelUtils.GetLabelFor(ParameterGroup)} : {ParameterName}"; }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { Name = ParameterName; }

      NickName = Name;
      MutableNickName = false;

      try { Description = p.StorageType == DB.StorageType.ElementId ? "ElementId" : DB.LabelUtils.GetLabelFor(p.Definition.ParameterType); }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException)
      { Description = p.Definition.UnitType == DB.UnitType.UT_Number ? "Enumerate" : DB.LabelUtils.GetLabelFor(p.Definition.UnitType); }

      if (ParameterSharedGUID.HasValue)
        Description = $"Shared parameter {ParameterSharedGUID.Value:B}\n{Description}";
      else if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        Description = $"BuiltIn parameter {ParameterBuiltInId.ToStringGeneric()}\n{Description}";
      else
        Description = $"{ParameterBinding} project parameter\n{Description}";
    }

    public string ParameterName { get; private set; } = string.Empty;
    public DB.ParameterType ParameterType { get; private set; } = DB.ParameterType.Invalid;
    public DB.BuiltInParameterGroup ParameterGroup { get; private set; } = DB.BuiltInParameterGroup.INVALID;
    public RevitAPI.ParameterBinding ParameterBinding { get; private set; } = RevitAPI.ParameterBinding.Unknown;
    public DB.BuiltInParameter ParameterBuiltInId { get; private set; } = DB.BuiltInParameter.INVALID;
    public Guid? ParameterSharedGUID { get; private set; } = default;

    public override sealed bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      ///////////////////////////////////////////////////////////////
      // Keep this code while in WIP to read WIP components
      if
      (
        Enum.TryParse(Name, out DB.BuiltInParameter builtInId) &&
        Enum.IsDefined(typeof(DB.BuiltInParameter), builtInId)
      )
        ParameterBuiltInId = builtInId;
      ///////////////////////////////////////////////////////////////

      var parameterName = default(string);
      reader.TryGetString("ParameterName", ref parameterName);
      ParameterName = parameterName;

      var parameterType = (int) DB.ParameterType.Invalid;
      reader.TryGetInt32("ParameterType", ref parameterType);
      ParameterType = (DB.ParameterType) parameterType;

      var parameterGroup = (int) DB.BuiltInParameterGroup.INVALID;
      reader.TryGetInt32("ParameterGroup", ref parameterGroup);
      ParameterGroup = (DB.BuiltInParameterGroup) parameterGroup;

      var parameterBinding = (int) RevitAPI.ParameterBinding.Unknown;
      reader.TryGetInt32("ParameterBinding", ref parameterBinding);
      ParameterBinding = (RevitAPI.ParameterBinding) parameterBinding;

      var parameterBuiltInId = (int) DB.BuiltInParameter.INVALID;
      reader.TryGetInt32("ParameterBuiltInId", ref parameterBuiltInId);
      ParameterBuiltInId = (DB.BuiltInParameter) parameterBuiltInId;

      var parameterSharedGUID = default(Guid);
      if (reader.TryGetGuid("ParameterSharedGUID", ref parameterSharedGUID))
        ParameterSharedGUID = parameterSharedGUID;
      else
        ParameterSharedGUID = default;

      return true;
    }

    public override sealed bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (!string.IsNullOrEmpty(ParameterName))
        writer.SetString("ParameterName", ParameterName);

      if (ParameterGroup != DB.BuiltInParameterGroup.INVALID)
        writer.SetInt32("ParameterGroup", (int) ParameterGroup);

      if (ParameterType != DB.ParameterType.Invalid)
        writer.SetInt32("ParameterType", (int) ParameterType);

      if (ParameterBinding != RevitAPI.ParameterBinding.Unknown)
        writer.SetInt32("ParameterBinding", (int) ParameterBinding);

      if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        writer.SetInt32("ParameterBuiltInId", (int) ParameterBuiltInId);

      if (ParameterSharedGUID.HasValue)
        writer.SetGuid("ParameterSharedGUID", ParameterSharedGUID.Value);

      return true;
    }

    public override int GetHashCode()
    {
      if (ParameterSharedGUID.HasValue)
        return ParameterSharedGUID.Value.GetHashCode();

      if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        return (int) ParameterBuiltInId;

      return new { ParameterName, ParameterType, ParameterBinding }.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj is ParameterParam value)
      {
        if (ParameterSharedGUID.HasValue)
          return value.ParameterSharedGUID.HasValue && ParameterSharedGUID == value.ParameterSharedGUID.Value;

        if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
          return ParameterBuiltInId == value.ParameterBuiltInId;

        return ParameterName == value.ParameterName &&
               ParameterType == value.ParameterType &&
               ParameterBinding == value.ParameterBinding;
      }

      return false;
    }

    public DB.Parameter GetParameter(DB.Element element)
    {
      if (ParameterSharedGUID.HasValue)
        return element.get_Parameter(ParameterSharedGUID.Value);

      if (ParameterBuiltInId != DB.BuiltInParameter.INVALID)
        return element.get_Parameter(ParameterBuiltInId);

      return element.GetParameter(ParameterName, ParameterType, ParameterBinding, RevitAPI.ParameterSet.Project);
    }
  }
}
