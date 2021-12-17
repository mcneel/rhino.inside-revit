using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.ElementTracking
{
  public static class TrackingParamElementExtensions
  {
    /// <summary>
    /// Reads an element from <paramref name="name"/> parameter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parameters"></param>
    /// <param name="name"></param>
    /// <param name="doc"></param>
    /// <param name="value"></param>
    /// <param name="elementName"></param>
    /// <returns>True if the parameter is present</returns>
    public static bool ReadTrackedElement<T>(this GH_ComponentParamServer parameters, string name, ARDB.Document doc, out T value)
      where T : ARDB.Element
    {
      var index = parameters.Output.IndexOf(name, out var parameter);
      if (parameter is IGH_TrackingParam tracking)
      {
        tracking.ReadTrackedElement(doc, out value);
        return true;
      }
      else if (parameter is object)
        throw new InvalidOperationException($"Parameter '{name}' does not implement {nameof(IGH_TrackingParam)} interface");

      value = default;
      return false;
    }

    public static void WriteTrackedElement<T>(this GH_ComponentParamServer parameters, string name, ARDB.Document document, T value)
      where T : ARDB.Element
    {
      var index = parameters.Output.IndexOf(name, out var parameter);
      if (parameter is IGH_TrackingParam tracking)
        tracking.WriteTrackedElement(document, value);
      else if (parameter is object)
        throw new InvalidOperationException($"Parameter '{name}' does not implement {nameof(IGH_TrackingParam)} interface");
      else
        throw new InvalidOperationException($"Parameter '{name}' is missing");
    }

    public static IEnumerable<T> TrackedElements<T>(this GH_ComponentParamServer parameters, string name, ARDB.Document document)
      where T : ARDB.Element
    {
      var index = parameters.Output.IndexOf(name, out var parameter);
      if (parameter is IGH_TrackingParam tracking)
        return tracking.GetTrackedElements<T>(document);
      else if (parameter is object)
        throw new InvalidOperationException($"Parameter '{name}' does not implement {nameof(IGH_TrackingParam)} interface");

      return Enumerable.Empty<T>();
    }
  }

  internal static class TrackingParamGooExtensions
  {
    public static bool ReadTrackedElement<T>(this GH_ComponentParamServer parameters, string name, ARDB.Document doc, out T value)
      where T : Types.IGH_ElementId
    {
      if
      (
        TrackingParamElementExtensions.ReadTrackedElement(parameters, name, doc, out ARDB.Element element) &&
        Types.Element.FromElement(element) is T t
      )
      {
        value = t;
        return true;
      }

      value = default;
      return false;
    }

    public static void WriteTrackedElement<T>(this GH_ComponentParamServer parameters, string name, ARDB.Document document, T value)
      where T : Types.IGH_ElementId
    {
      TrackingParamElementExtensions.WriteTrackedElement(parameters, name, document, value.Value as ARDB.Element);
    }

    public static IEnumerable<T> TrackedElements<T>(this GH_ComponentParamServer parameters, string name, ARDB.Document document)
      where T : Types.IGH_ElementId
    {
      return TrackingParamElementExtensions.TrackedElements<ARDB.Element>(parameters, name, document).
        Select(x => Types.Element.FromElement(x) is T t ? t : default);
    }
  }
}

namespace RhinoInside.Revit.GH.Parameters
{
  using ElementTracking;
  using External.DB;
  using External.DB.Extensions;

  public abstract class Element<T, R> : ElementIdParam<T, R>,
    IGH_TrackingParam
    where T : class, Types.IGH_Element
    where R : ARDB.Element
  {
    protected Element(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    protected override T PreferredCast(object data) => data is R ? Types.Element.FromValue(data) as T : null;

    internal static IEnumerable<Types.IGH_Element> ToElementIds(IGH_Structure data) =>
      data.AllData(true).
      OfType<Types.IGH_Element>().
      Where(x => x.IsValid);

    #region UI
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      Menu_AppendSeparator(menu);
      Menu_AppendActions(menu);
    }

    public virtual void Menu_AppendActions(ToolStripDropDown menu)
    {
      if (Revit.ActiveUIDocument?.Document is ARDB.Document doc)
      {
        if (Kind == GH_ParamKind.output && Attributes.GetTopLevel.DocObject is Components.ReconstructElementComponent)
        {
          var pinned = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
                       Where(x => x?.Pinned == true).Any();

          if (pinned)
            Menu_AppendItem(menu, $"Unpin {GH_Convert.ToPlural(TypeName)}", Menu_UnpinElements, DataType != GH_ParamData.remote, false);

          var unpinned = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
                       Where(x => x?.Pinned == false).Any();

          if (unpinned)
            Menu_AppendItem(menu, $"Pin {GH_Convert.ToPlural(TypeName)}", Menu_PinElements, DataType != GH_ParamData.remote, false);
        }

        {
          bool delete = ToElementIds(VolatileData).Any(x => doc.Equals(x.Document));

          Menu_AppendItem(menu, $"Delete {GH_Convert.ToPlural(TypeName)}", Menu_DeleteElements, delete, false);
        }

        if (TrackingMode != TrackingMode.NotApplicable)
          Menu_AppendItem(menu, $"Release {GH_Convert.ToPlural(TypeName)}…", Menu_ReleaseElements, HasTrackedElements, false);
      }
    }

    bool HasTrackedElements => ToElementIds(VolatileData).Any(x => ElementStream.IsElementTracked(x.Value as ARDB.Element));

    async Task<(bool Committed, IList<string> Messages)> ReleaseElementsAsync()
    {
      var committed = false;
      var messages = new List<string>();

      foreach (var document in ToElementIds(VolatileData).GroupBy(x => x.Document))
      {
        using (var tx = new ARDB.Transaction(document.Key, "Release Elements"))
        {
          if (tx.Start() == ARDB.TransactionStatus.Started)
          {
            var list = new List<ARDB.ElementId>();

            foreach (var element in document.Select(x => x.Value).OfType<ARDB.Element>())
            {
              if (ElementStream.ReleaseElement(element))
              {
                element.Pinned = false;
                list.Add(element.Id);
              }
            }

            // Show feedback on Revit
            if (list.Count > 0)
            {
              if (list.Count == 1)
                messages.Add($"An element was released at '{document.Key.Title.TripleDot(16)}' document and is no longer synchronized.");
              else
                messages.Add($"{list.Count} elements were released at '{document.Key.Title.TripleDot(16)}' document and are no longer synchronized.");

              using (var message = new ARDB.FailureMessage(ExternalFailures.ElementFailures.TrackedElementReleased))
              {
                message.SetFailingElements(list);
                document.Key.PostFailure(message);
              }

              committed |= await tx.CommitAsync() == ARDB.TransactionStatus.Committed;
            }
          }
        }
      }

      return (committed, messages);
    }

    async void Menu_ReleaseElements(object sender, EventArgs e)
    {
      // TODO: Ask for pinned or not, Save a selection, destination workset, destination view??

      bool enableSolutions = Guest.DocumentChangedEvent.EnableSolutions;
      Guest.DocumentChangedEvent.EnableSolutions = false;

      var (commited, messages) = await ReleaseElementsAsync();

      Guest.ShowEditor();

      if (commited)
      {
        // Show feedback on Grasshopper
        ExpireSolutionTopLevel(false);

        foreach (var message in messages)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

        OnDisplayExpired(false);
      }

      Guest.DocumentChangedEvent.EnableSolutions = enableSolutions;
    }

    void Menu_PinElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var elements = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
                       Where(x => x.Pinned == false);

      if (elements.Any())
      {
        try
        {
          using (var transaction = new ARDB.Transaction(doc, "Pin elements"))
          {
            transaction.Start();

            foreach (var element in elements)
              element.Pinned = true;

            transaction.Commit();
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          TaskDialog.Show("Pin elements", $"One or more of the {TypeName} cannot be pinned.");
        }
      }
    }

    void Menu_UnpinElements(object sender, EventArgs args)
    {
      var doc = Revit.ActiveUIDocument.Document;
      var elements = ToElementIds(VolatileData).
                       Where(x => doc.Equals(x.Document)).
                       Select(x => doc.GetElement(x.Id)).
                       Where(x => x.Pinned == true);

      if (elements.Any())
      {
        try
        {
          using (var transaction = new ARDB.Transaction(doc, "Unpin elements"))
          {
            transaction.Start();

            foreach (var element in elements)
              element.Pinned = false;

            transaction.Commit();
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
          TaskDialog.Show("Unpin elements", $"One or more of the {TypeName} cannot be unpinned.");
        }
      }
    }

    bool DeleteElements(out IList<GH_RuntimeMessage> messages)
    {
      var committed = false;
      messages = new List<GH_RuntimeMessage>();
      var groups = new List<ARDB.TransactionGroup>();

      try
      {
        foreach (var document in ToElementIds(VolatileData).GroupBy(x => x.Document))
        {
          // Look for all disctint non built-in elements
          var elementIds = new HashSet<ARDB.ElementId>
          (
            document.Where(x => !x.Id.IsBuiltInId() && x.Value is object).Select(x => x.Id),
            default(ElementIdEqualityComparer)
          );

          if (elementIds.Count > 0)
          {
            var group = new ARDB.TransactionGroup(document.Key, "Delete Elements")
            { IsFailureHandlingForcedModal = true };

            if (group.Start() != ARDB.TransactionStatus.Started) continue;
            groups.Add(group);

            using (var tx = new ARDB.Transaction(document.Key, "Delete Elements"))
            {
              tx.SetFailureHandlingOptions(tx.GetFailureHandlingOptions().SetDelayedMiniWarnings(false));

              if (tx.Start() == ARDB.TransactionStatus.Started)
              {
                // Show feedback on Revit
                using (var message = new ARDB.FailureMessage(ExternalFailures.ElementFailures.ConfirmDeleteElement))
                {
                  message.SetFailingElements(elementIds);
                  document.Key.PostFailure(message);
                }

                if (tx.Commit() == ARDB.TransactionStatus.Committed)
                {
                  tx.Start();
                  document.Key.Delete(elementIds);
                  committed |= tx.Commit() == ARDB.TransactionStatus.Committed;

                  if (committed)
                  {
                    messages.Add
                    (
                      new GH_RuntimeMessage
                      ($"Some elements were deleted from '{document.Key.Title.TripleDot(16)}'.",
                      GH_RuntimeMessageLevel.Warning)
                    );
                  }
                  else break;
                }
              }
            }
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException e)
      {
        committed = false;
        messages.Clear();
        messages.Add
        (
          new GH_RuntimeMessage(e.Message.Split
          (new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0],
          GH_RuntimeMessageLevel.Error, Attributes.GetTopLevel.DocObject.Name)
        );
      }

      foreach (var group in groups)
      {
        using (group)
          if (committed) group.Assimilate();
          else group.RollBack();
      }

      return committed;
    }

    void Menu_DeleteElements(object sender, EventArgs e)
    {
      bool enableSolutions = Guest.DocumentChangedEvent.EnableSolutions;
      Guest.DocumentChangedEvent.EnableSolutions = false;
      var enabledCanvas = Grasshopper.Instances.ActiveCanvas.Enabled;
      Grasshopper.Instances.ActiveCanvas.Enabled = false;

      try
      {
        var commited = DeleteElements(out var messages);
        Guest.ShowEditor();

        if (commited)
        {
          // Show feedback on the component
          ClearRuntimeMessages();
          foreach (var message in messages)
            AddRuntimeMessage(message.Type, message.Message);

          ReloadVolatileData();
          ExpireDownStreamObjects();
          OnPingDocument()?.NewSolution(false);
        }
        else
        {
          // Show feedback on Grasshopper
          foreach (var message in messages)
            Grasshopper.Instances.DocumentEditor.SetStatusBarEvent(message);
        }
      }
      finally
      {
        Grasshopper.Instances.ActiveCanvas.Enabled = enabledCanvas;
        Guest.DocumentChangedEvent.EnableSolutions = enableSolutions;
      }
    }
    #endregion

    TrackingMode TrackingMode => (Attributes.GetTopLevel.DocObject as IGH_TrackingComponent)?.TrackingMode ?? TrackingMode.NotApplicable;

    #region IGH_TrackingParam
    ElementStreamMode IGH_TrackingParam.StreamMode { get; set; }

    internal ElementStreamDictionary<R> ElementStreams;

    void IGH_TrackingParam.OpenTrackingParam(ARDB.Document document)
    {
      if (TrackingMode == TrackingMode.NotApplicable) return;

      // Open an ElementStreamDictionary to store ouput param on multiple documents
      var streamId = new ElementStreamId(this, string.Empty);
      var streamMode = ((IGH_TrackingParam) this).StreamMode | (document is object ? ElementStreamMode.CurrentDocument : default);
      ElementStreams = new ElementStreamDictionary<R>(document, streamId, streamMode);

      // This deletes all elements tracked from previous iterations.
      // It helps to avoid name collisions on named elements.
      if (TrackingMode < TrackingMode.Reconstruct)
      {
        var chain = Attributes.GetTopLevel.DocObject as Components.TransactionalChainComponent;
        foreach (var stream in ElementStreams)
        {
          if (stream.Value.Length == 0) continue;

          // Insert the Clear operation on the TransactionChain if available.
          chain?.StartTransaction(stream.Key);
          stream.Value.Clear();
        }
      }
    }

    void IGH_TrackingParam.CloseTrackingParam()
    {
      if (TrackingMode == TrackingMode.NotApplicable) return;

      // Close all element streams and delete excess elements.
      using (ElementStreams) ElementStreams = default;
    }

    IEnumerable<TOutput> IGH_TrackingParam.GetTrackedElements<TOutput>(ARDB.Document doc)
    {
      if (TrackingMode == TrackingMode.Reconstruct)
        return ElementStreams[doc].Cast<TOutput>();

      return Enumerable.Empty<TOutput>();
    }

    bool IGH_TrackingParam.ReadTrackedElement<TOutput>(ARDB.Document doc, out TOutput element)
    {
      if (TrackingMode == TrackingMode.Reconstruct)
      {
        var elementStream = ElementStreams[doc];
        if (elementStream.Read(out var previous) && previous is TOutput output)
        {
          element = output;
          return true;
        }
      }

      element = default;
      return false;
    }

    void IGH_TrackingParam.WriteTrackedElement<TInput>(ARDB.Document doc, TInput element)
    {
      if (TrackingMode > TrackingMode.Disabled)
        ElementStreams[doc].Write(element as R);
    }
    #endregion
  }

  public class Element : Element<Types.IGH_Element, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("F3EA4A9C-B24F-4587-A358-6A7E6D8C028B");

    public Element() : base("Element", "Element", "Contains a collection of Revit elements", "Params", "Revit Primitives") { }

    protected override Types.IGH_Element InstantiateT() => new Types.Element();
  }
}
