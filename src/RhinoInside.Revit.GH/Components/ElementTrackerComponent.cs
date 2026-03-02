using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components
{
  using Convert.Display;
  using Convert.Geometry;
  using ElementTracking;
  using External.DB.Extensions;

  public abstract class ElementTrackerComponent : TransactionalChainComponent, IGH_TrackingComponent
  {
    protected ElementTrackerComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    protected virtual bool CurrentDocumentOnly
    {
      get
      {
        if(Params.Input<Parameters.Document>("Document") is IGH_Param document)
          return document.SourceCount == 0 && document.DataType == GH_ParamData.@void;

        return Inputs.Any(x => x.Param is Parameters.Document && x.Param.Name == "Document");
      }
    }

    public override void ClearData()
    {
      base.ClearData();

      if (TrackingMode != TrackingMode.NotApplicable)
        Message = TrackingMode == TrackingMode.Disabled ? "No Tracking" : string.Empty;
    }

    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();

      var currentDocument = CurrentDocumentOnly ? Revit.ActiveDBDocument : default;
      foreach (var output in Params.Output.OfType<IGH_TrackingParam>())
        output.OpenTrackingParam(currentDocument);
    }

    protected override void AfterSolveInstance()
    {
      foreach (var output in Params.Output.OfType<IGH_TrackingParam>())
        output.CloseTrackingParam();

      base.AfterSolveInstance();
    }

    protected T ReconstructElement<T>
    (
      ARDB.Document document, string parameterName,
      Func<T, T> update
    ) where T : ARDB.Element
    {
      return ReconstructElement(document, parameterName, x => true, update);
    }

    protected T ReconstructElement<T>
    (
      ARDB.Document document, string parameterName,
      Predicate<T> validate, Func<T, T> update
    )
    where T : ARDB.Element
    {
      var output = default(T);

      if (Params.ReadTrackedElement(parameterName, document, out T input))
      {
        if (input?.DesignOption?.Id is ARDB.ElementId elementDesignOptionId)
        {
          var activeDesignOptionId = ARDB.DesignOption.GetActiveDesignOptionId(input.Document);

          if (elementDesignOptionId != activeDesignOptionId)
            input = null;
        }

        var grouped = input is object && input.GroupId != ARDB.ElementId.InvalidElementId;
        var graphical = input is object && Types.GraphicalElement.IsValidElement(input);
        var pinned = input?.Pinned != false;

        try
        {
          if (!graphical || (pinned && !grouped))
          {
            if (validate(input))
              UpdateDocument(document, () => output = update(input));
            else
              output = null;
          }
          else
          {
            if (graphical && input is object)
            {
              var mesh = default(Rhino.Geometry.Mesh);
              var message = default(string);

              if (grouped)
                message = "Highlighted elements can't be updated because are grouped.";
              else if (!pinned)
                message = "Highlighted elements were not updated because are unpinned.";

              using
              (
                var options = input.ViewSpecific ?
                new ARDB.Options() { View = input.Document.GetElement(input.OwnerViewId) as ARDB.View } :
                new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Medium }
              )
              using (var geometry = input.GetGeometry(options))
              {
                if (geometry is object)
                {
                  mesh = new Rhino.Geometry.Mesh();
                  mesh.Append(geometry.GetPreviewMeshes(input.Document, null));
                  if (mesh.Faces.Count == 0)
                  {
                    var inch = Revit.ModelUnits / 12.0;
                    var box = input.GetBoundingBoxXYZ().ToBox(); box.Inflate(inch, inch, inch);
                    mesh = Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1);
                  }
                }
              }

              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message, mesh);
            }

            output = input;
          }
        }
        catch
        {
          if (FailureProcessingMode <= ARDB.FailureProcessingResult.ProceedWithCommit)
            output = input;

          throw;
        }
        finally
        {
          Params.WriteTrackedElement(parameterName, document, output);

          if (pinned && Types.GraphicalElement.IsValidElement(output))
          {
            // Reset CreatedPhaseId to last phase available
            if (!document.IsFamilyDocument && output.CreatedPhaseId != ARDB.ElementId.InvalidElementId && output.ArePhasesModifiable())
            {
              using (var phases = document.Phases)
              {
                if (!phases.IsEmpty)
                {
                  try
                  {
                    var createdPhaseId = document.Phases.Cast<ARDB.Phase>().Last().Id;
                    if (output.CreatedPhaseId != createdPhaseId && output.IsPhaseCreatedValid(createdPhaseId))
                    {
                      output.DemolishedPhaseId = ARDB.ElementId.InvalidElementId;
                      output.CreatedPhaseId = createdPhaseId;
                    }
                  }
                  catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
                }
              }
            }

            // In case element is crated on this iteration we pin it here by default
            if (!output.Pinned)
            {
              try { output.Pinned = true; }
              catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
            }
          }
        }
      }

      return output;
    }

    /// <summary>
    /// Check if there is a non tracked element with the desired name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="paramName"></param>
    /// <param name="document"></param>
    /// <param name="elementNomen"></param>
    /// <param name="element"></param>
    /// <param name="categoryId"></param>
    /// <returns></returns>
    protected bool CanReconstruct<T>
    (
      string paramName,
      out bool untracked,
      ref T element,
      ARDB.Document document,
      string elementNomen,
      string parentNomen = default, ARDB.BuiltInCategory? categoryId = default
    )
      where T : ARDB.Element
    {
      return CanReconstruct
      (
        paramName,
        out untracked,
        ref element,
        document,
        elementNomen,
        (doc, name) =>
        {
          doc.TryGetElement(out T existing, name, parentNomen, categoryId);
          return existing;
        }
      );
    }

    protected internal bool CanReconstruct<T>
    (
      string paramName,
      out bool untracked,
      ref T element,
      ARDB.Document document,
      string elementNomen,
      Func<ARDB.Document, string, T> GetElement
    )
      where T : ARDB.Element
    {
      var nomenParameter = ARDB.BuiltInParameter.INVALID;
      if
      (
        !string.IsNullOrWhiteSpace(elementNomen) &&
        element?.GetNomen(out nomenParameter) != elementNomen
      )
      {
        // Query for an existing element.
        if (GetElement(document, elementNomen) is T existing)
        {
          Debug.Assert(existing.Id != element?.Id);

          if (Params.IsTrackedElement(paramName, existing))
          {
            // If existing is tracked and still pending to be processed
            // change its name to avoid collisions.
            existing.SetNomen(nomenParameter, existing.UniqueId);
          }
          else
          {
            untracked = true;
            return (element = PostNomenAlreadyInUse(existing)) is object;
          }
        }
      }

      untracked = false;
      return true;
    }

    protected T PostNomenAlreadyInUse<T>(T existing)
      where T: ARDB.Element
    {
      if (existing is object)
      {
        var nomen = existing.GetNomen(out var nomemParameter);
        var label = ((ERDB.Schemas.ParameterId) nomemParameter).Label;
        if (string.IsNullOrWhiteSpace(label)) label = "name";
        var message = $"The {label.ToLowerInvariant()} '{nomen}' is already in use.";

        if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
        {
          AddContinuableFailure($"{message}{OS.NewLine}Use 'Continue' to reference the existing one.");
          return null;
        }

        if (FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{message} Using existing one.");
          return existing;
        }

        if (FailureProcessingMode == ARDB.FailureProcessingResult.WaitForUserInput)
        {
          var failureId = existing is ARDB.ViewSheet ?
            ARDB.BuiltInFailures.SheetFailures.SheetNumberDuplicated :
            ARDB.BuiltInFailures.GeneralFailures.NameNotUnique;

          using (var failure = new ARDB.FailureMessage(failureId))
          {
            failure.SetFailingElement(existing.Id);
            existing.Document.PostFailure(failure);
          }

          return null;
        }

        throw new Exceptions.RuntimeException(message);
      }

      throw new Exceptions.RuntimeException();
    }

    #region IGH_TrackingComponent
    TrackingMode IGH_TrackingComponent.TrackingMode => TrackingMode;
    internal TrackingMode TrackingMode { get; set; } = TrackingMode.Reconstruct;

    public override bool DestroyParameter(GH_ParameterSide side, int index)
    {
      if (!base.DestroyParameter(side, index))
        return false;

      if (side == GH_ParameterSide.Output)
      {
        var param = Params.Output[index];

        if (param is IGH_TrackingParam)
          Guest.Instance.ObjectsDeleted(Grasshopper.Instances.ActiveCanvas, (OnPingDocument(), new IGH_DocumentObject[] { param }));
      }

      return true;
    }
    #endregion

    #region IO
    public override void AddedToDocument(GH_Document document)
    {
      if (ComponentVersion < new Version(0, 9, 0, 0))
        TrackingMode = TrackingMode.Reconstruct;

      base.AddedToDocument(document);
    }

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int mode = (int) TrackingMode.Disabled;
      reader.TryGetInt32("TrackingMode", ref mode);
      TrackingMode = (TrackingMode) mode;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (TrackingMode != TrackingMode.Disabled)
        writer.SetInt32("TrackingMode", (int) TrackingMode);

      return true;
    }
    #endregion

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      if (TrackingMode != TrackingMode.NotApplicable)
      {
        if (Params.Output.OfType<IGH_TrackingParam>().FirstOrDefault() is IGH_Param output)
        {
          var tracking = Menu_AppendItem(menu, "Tracking Mode");

          var append = Menu_AppendItem(tracking.DropDown, "Disabled", (s, a) => { TrackingMode = TrackingMode.Disabled; ExpireSolution(true); }, true, TrackingMode == TrackingMode.Disabled);
          append.ToolTipText = $"No element tracking takes part in this mode, each solution will append a new {output.TypeName}." + OS.NewLine +
                               $"The operation may fail if a {output.TypeName} with same name already exists.";

          var supersede = Menu_AppendItem(tracking.DropDown, "Enabled : Replace", (s, a) => { TrackingMode = TrackingMode.Supersede; ExpireSolution(true); }, true, TrackingMode == TrackingMode.Supersede);
          supersede.ToolTipText = $"A brand new {output.TypeName} will be created for each solution." + OS.NewLine +
                                  $"{GH_Convert.ToPlural(output.TypeName)} created on previous iterations are deleted.";

          var reconstruct = Menu_AppendItem(tracking.DropDown, "Enabled : Update", (s, a) => { TrackingMode = TrackingMode.Reconstruct; ExpireSolution(true); }, true, TrackingMode == TrackingMode.Reconstruct);
          reconstruct.ToolTipText = $"If suitable, the previous solution {output.TypeName} will be updated from the input values;" + OS.NewLine +
                                    "otherwise, a new one will be created.";
        }
      }
    }
    #endregion
  }
}
