using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Adaptive Component")]
  public sealed class AdaptiveInstance : FamilyInstance
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      if (element is ARDB.FamilyInstance instance)
        return ARDB.AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(instance);

      return false;
    }

    public AdaptiveInstance() { }
    public AdaptiveInstance(ARDB.FamilyInstance value) : base(value) { }

    //public override bool CanFlipFacing => true;
    //public override bool? FacingFlipped
    //{
    //  get => Value is ARDB.FamilyInstance instance ? ARDB.AdaptiveComponentInstanceUtils.IsInstanceFlipped(instance) : default(bool?);
    //  set { if (Value is ARDB.FamilyInstance instance && value.HasValue && value != FacingFlipped) ARDB.AdaptiveComponentInstanceUtils.SetInstanceFlipped(instance, value.Value); }
    //}
  }

  [Kernel.Attributes.Name("Adaptive Component Type")]
  public sealed class AdaptiveType : FamilySymbol
  {
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      if (element is ARDB.FamilySymbol symbol)
        return ARDB.AdaptiveComponentInstanceUtils.IsAdaptiveFamilySymbol(symbol);

      return false;
    }

    public AdaptiveType() { }
    public AdaptiveType(ARDB.FamilySymbol value) : base(value) { }
  }
}
