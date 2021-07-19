using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.ElementTracking
{
  using DBXS = DB.ExtensibleStorage;

  public enum TrackingMode
  {
    /// <summary>
    /// Tracking is not applicable on this object.
    /// </summary>
    NotApplicable = -1,
    /// <summary>
    /// A brand new element should be created on each solution.
    /// Elements created on previous iterations are ignored.
    /// </summary>
    /// <remarks>
    /// No element tracking takes part in this mode, each run appends a new element.
    /// The operation may fail if an element with same name already exists.
    /// </remarks>
    Disabled = 0,
    /// <summary>
    /// A brand new element should be created for each solution.
    /// Elements created on previous iterations are deleted.
    /// </summary>
    /// <remarks>
    /// If an element with the same name already exists it will be replaced by the new one.
    /// </remarks>
    Supersede = 1,
    /// <summary>
    /// An existing element should be reconstructed from the input values if it exists;
    /// otherwise, a new one should be created.
    /// </summary>
    /// <remarks>
    /// The operation may fail if an element with this name already exists.
    /// </remarks>
    Reconstruct = 2,
  };

  internal interface IGH_TrackingComponent
  {
    /// <summary>
    /// Current tracking mode.
    /// </summary>
    /// <remarks>
    /// Default value is <see cref="TrackingMode.NotApplicable"/>.
    /// </remarks>
    TrackingMode TrackingMode { get; set; }
  }

  internal interface IGH_TrackingParam
  {
    ElementStreamMode StreamMode { get; set; }

    void OpenTrackingParam(bool currentDocumentOnly);
    void CloseTrackingParam();

    IEnumerable<T> GetTrackedElements<T>(DB.Document doc) where T : DB.Element;

    bool ReadTrackedElement<T>(DB.Document doc, out T element) where T : DB.Element;
    void WriteTrackedElement<T>(DB.Document doc, T element) where T : DB.Element;
  }

  #region ElementStream
  [Flags]
  internal enum ElementStreamMode
  {
    Unfiltered = 0,
    CurrentDocument = 1,     // Skip elements in other Documents than the current.
    CurrentView = 2,         // Skip elements in other Views than the current.
    CurrentDesignOption = 4, // Skip elements in other DesignOptions than the current.
    CurrentWorkset = 8,      // Skip elements in other Worksets than the current.
  }

  internal class ElementStreamDictionary<T> : IReadOnlyDictionary<DB.Document, ElementStream<T>>, IDisposable
    where T : DB.Element
  {
    readonly ElementStreamMode Mode;
    readonly ElementStreamId Id;
    readonly DB.BuiltInCategory CategoryId;
    readonly Dictionary<DB.Document, ElementStream<T>> streams = new Dictionary<DB.Document, ElementStream<T>>();

    public ElementStreamDictionary(ElementStreamId streamId, ElementStreamMode mode, DB.BuiltInCategory categoryId = default)
    {
      Mode = mode;
      Id = streamId;
      CategoryId = categoryId;

      Open();
    }

    void IDisposable.Dispose() => Close();

    void Open()
    {
      var documents = Mode.HasFlag(ElementStreamMode.CurrentDocument) ?
        Enumerable.Repeat(Revit.ActiveDBDocument, 1) :
        Revit.ActiveDBApplication.Documents.Cast<DB.Document>();

      foreach (var document in documents)
      {
        if (document is null) continue;
        if (document.IsLinked) continue;

        var streamFilter = new ElementStreamFilter() { CategoryId = CategoryId };

        if (Mode.HasFlag(ElementStreamMode.CurrentDesignOption))
          streamFilter.DesignOptionId = DB.DesignOption.GetActiveDesignOptionId(document);

        if (Mode.HasFlag(ElementStreamMode.CurrentView) && document.GetActiveGraphicalView() is DB.View view)
          streamFilter.OwnerViewId = view.Id;

        if (Mode.HasFlag(ElementStreamMode.CurrentWorkset))
          streamFilter.WorksetId = document.GetWorksetTable().GetActiveWorksetId();

        streams.Add(document, new ElementStream<T>(document, Id, streamFilter));
      }
    }

    void Close()
    {
      foreach (var stream in streams)
        stream.Value?.Dispose();

      streams.Clear();
    }

    #region IReadOnlyDictionary
    public ElementStream<T> this[DB.Document doc] => streams[doc];
    public IEnumerable<DB.Document> Keys => streams.Keys;
    public IEnumerable<ElementStream<T>> Values => streams.Values;

    public bool ContainsKey(DB.Document key) => streams.ContainsKey(key);
    public bool TryGetValue(DB.Document key, out ElementStream<T> value) => streams.TryGetValue(key, out value);
    #endregion

    #region IReadOnlyCollection
    public int Count => streams.Count;
    #endregion

    #region IEnumerable
    public IEnumerator<KeyValuePair<DB.Document, ElementStream<T>>> GetEnumerator() => streams.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => streams.GetEnumerator();
    #endregion
  }

  internal class ElementStream : IDisposable
  {
    public virtual void Dispose() { }

    public static bool ReleaseElement(DB.Element element)
    {
      return TrackedElementsDictionary.Remove(element);
    }

    public static bool IsElementTracked(DB.Element element)
    {
      return element is null ? false : TrackedElementsDictionary.ContainsKey(element);
    }
  }

  internal class ElementStream<T> : ElementStream, IEnumerable<T>, IDisposable
    where T : DB.Element
  {
    class ElementEnumerator<E> : IEnumerator<E> where E : DB.Element
    {
      internal readonly IDictionary<int, E> data;
      int count;
      int position;
      bool stepOnSet; // true means Current is already assigned and next set on Current will MoveNext()
      E current;

      public ElementEnumerator(IDictionary<int, E> values)
      {
        data = values;
        Reset();
      }

      public int Length => data.Keys.Count > 0 ? data.Keys.Last() + 1: 0;
      public int Position => position;
      public E Current
      {
        get => current;
        set
        {
          if (stepOnSet) MoveNext();

          if (current?.IsValidObject == true && value?.Id != current.Id)
            current.Document.Delete(current.Id);

          current = value;
          stepOnSet = true;
        }
      }
      object IEnumerator.Current => Current;

      public void Dispose()
      {
        data.Clear();
        Reset();
      }

      public DB.ElementId[] Excess
      {
        get
        {
          var excess = new DB.ElementId[count];
          var values = data.Values;

          int e = 0;
          foreach (var id in values.Skip(values.Count - count).Where(x => x.IsValidObject).Select(x => x.Id))
            excess[e++] = id;

          return excess;
        }
      }

      public bool MoveNext()
      {
        if (position == int.MaxValue) throw new IndexOutOfRangeException();

        position++;
        current = default;
        stepOnSet = false;

        if (count > 0)
        {
          if (data.TryGetValue(position, out var value))
          {
            current = value;
            count--;
          }

          return true;
        }

        return false;
      }

      public void Reset()
      {
        count = data.Count;
        position = -1;
        stepOnSet = true;
        current = default;
      }
    }

    public readonly DB.Document Document;
    public readonly ElementStreamId Id;
    public readonly DB.ElementFilter Filter;
    readonly ElementEnumerator<T> Enumerator;

    public ElementStream(DB.Document document, ElementStreamId streamId, DB.ElementFilter streamFilter = default)
    {
      Document = document;
      Id = streamId;
      Filter = streamFilter;
      try
      {
        Enumerator = new ElementEnumerator<T>(TrackedElementsDictionary.SortedKeys<T>(Document, Id, streamFilter));
      }
      catch (System.ArgumentException)
      {
        Enumerator = new ElementEnumerator<T>(new SortedList<int, T>());
        using (var scope = document.CommitScope())
        {
          var conflicts = TrackedElementsDictionary.Keys(document, streamId, streamFilter);
          document.Delete(conflicts.Select(x => x.Id).ToList());
          scope.Commit();
        }
      }
    }

    public override void Dispose()
    {
      using (Enumerator) DeleteExcess();
    }

    public void Clear()
    {
      using (Enumerator) { Enumerator.Reset(); DeleteExcess(); }
    }

    void DeleteExcess()
    {
      var excess = Enumerator.Excess;
      if (excess.Length > 0)
      {
        using (var scope = Document.CommitScope())
        {
          Document.Delete(excess);
          scope.Commit();
        }
      }
    }

    public int Length => Enumerator.Length;

    public bool Read(out T element)
    {
      if (Enumerator.MoveNext())
      {
        element = Enumerator.Current;

        if (element?.IsValidObject == false)
          element = default;

        return true;
      }

      element = default;
      return false;
    }

    public void Write(T element)
    {
      if (element is object)
      {
        if (!element.Document.Equals(Document))
          throw new ArgumentException("Element does not belong to stream document", nameof(element));

        if (Filter?.PassesFilter(element) == false)
          throw new ArgumentException("Element doesn't pass stream filter", nameof(element));
      }

      Enumerator.Current = element;

      if (Id.Authority is object && Id.Name is object && element is object)
        TrackedElementsDictionary.Add(element, Id.Authority, Id.Name, Enumerator.Position);
    }

    #region IEnumerable
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Enumerator.data.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Enumerator.data.Values.GetEnumerator();
    #endregion
  }

  struct AuthorityComparer : IEqualityComparer<IList<Guid>>
  {
    public bool Equals(IList<Guid> x, IList<Guid> y)
    {
      if (ReferenceEquals(x, y)) return true;
      if (x is null || y is null) return false;

      var count = x.Count;
      if (count != y.Count) return false;

      for (int i = 0; i < count; ++i)
        if (x[i] != y[i]) return false;

      return true;
    }

    public int GetHashCode(IList<Guid> obj)
    {
      int hash = 0;

      if (obj is object)
      {
        foreach (var guid in obj)
        {
          var value = guid.GetHashCode();
          hash ^= (value << 5) + value;
        }
      }

      return hash;
    }
  }

  internal struct ElementStreamId
  {
    public readonly IList<Guid> Authority;
    public readonly string Name;

    public ElementStreamId(IList<Guid> authority, string name)
    {
      Authority = authority;
      Name = name;
    }

    internal ElementStreamId(GH_Document document, IGH_DocumentObject authority, string name)
    {
      if (TryGetAuthority(document, authority, out Authority))
        Name = name;
      else
        Name = default;
    }

    public ElementStreamId(IGH_DocumentObject authority, string name)
    {
      if (TryGetAuthority(authority, out Authority))
        Name = name;
      else
        Name = default;
    }

    internal static bool TryGetAuthority(IGH_DocumentObject activeObject, out IList<Guid> authority)
    {
      return TryGetAuthority(activeObject.OnPingDocument(), activeObject, out authority);
    }

    internal static bool TryGetAuthority(GH_Document document, IGH_InstanceDescription activeObject, out IList<Guid> authority)
    {
      authority = new List<Guid>();

      while (activeObject is object)
      {
        authority.Add(activeObject.InstanceGuid);
        authority.Add(document?.DocumentID ?? Guid.Empty);

        if (document.Owner is IGH_DocumentOwner owner)
        {
          document = owner.OwnerDocument();
          activeObject = owner as IGH_InstanceDescription;
        }
        else return true;
      }

      return false;
    }
  }

  internal struct ElementStreamFilter
  {
    public DB.BuiltInCategory CategoryId;
    public DB.ElementId DesignOptionId;
    public DB.ElementId OwnerViewId;
    public DB.WorksetId WorksetId;
    public DB.ElementFilter ElementFilter;

    public static implicit operator ElementStreamFilter(DB.BuiltInCategory categoryId) => new ElementStreamFilter() { CategoryId = categoryId };
    public static implicit operator DB.ElementFilter(ElementStreamFilter streamFilter)
    {
      var filters = new List<DB.ElementFilter>();

      if (streamFilter.CategoryId != default)
        filters.Add(new DB.ElementCategoryFilter(streamFilter.CategoryId));

      if (streamFilter.DesignOptionId != default)
        filters.Add(new DB.ElementDesignOptionFilter(streamFilter.DesignOptionId));

      if (streamFilter.OwnerViewId != default)
        filters.Add(new DB.ElementOwnerViewFilter(streamFilter.OwnerViewId));

      if (streamFilter.WorksetId != default)
        filters.Add(new DB.ElementWorksetFilter(streamFilter.WorksetId));

      if (streamFilter.ElementFilter != default)
        filters.Add(streamFilter.ElementFilter);

      if (filters.Count > 0)
      {
        if (filters.Count == 1) return filters[0];
        return new DB.LogicalAndFilter(filters);
      }

      return default;
    }

    public bool PassesFilter(DB.Element element)
    {
      if (element is null)
        return false;

      if (CategoryId != default)
      {
        if ((element.Category?.Id ?? DB.ElementId.InvalidElementId).IntegerValue != (int) CategoryId)
          return false;
      }

      if (DesignOptionId != default)
      {
        if ((element.DesignOption?.Id ?? DB.ElementId.InvalidElementId) != DesignOptionId)
          return false;
      }

      if (OwnerViewId != default)
      {
        if (element.OwnerViewId != OwnerViewId)
          return false;
      }

      if (WorksetId != default)
      {
        if (element.WorksetId != WorksetId)
          return false;
      }

      if (ElementFilter != default)
      {
        if (!ElementFilter.PassesFilter(element))
          return false;
      }

      return true;
    }
  }

  internal static class TrackedElementsDictionary
  {
    static readonly Guid SchemaGUID = new Guid("BE96E8BD-C0B9-4B31-98C7-518E0ED70772");
    static readonly DB.ElementFilter SchemaFilter = new DBXS.ExtensibleStorageFilter(SchemaGUID);

    // TODO: Implement an Updater that cleans entities from copied Elements.

    internal static ICollection<IList<Guid>> NewAuthorityCollection() =>
      new HashSet<IList<Guid>>(default(AuthorityComparer));

    static class Fields
    {
      public static DBXS.Field UniqueId;
      public static DBXS.Field Flags;
      public static DBXS.Field Authority;
      public static DBXS.Field Name;
      public static DBXS.Field Index;
      public static DBXS.Field Data;
    }

    static DBXS.Schema schema;
    static DBXS.Schema Schema
    {
      get
      {
        if (schema != null) return schema;

        schema = DBXS.Schema.Lookup(SchemaGUID);
        if (schema is null)
        {
          var builder = new DBXS.SchemaBuilder(SchemaGUID);
          builder.SetSchemaName(typeof(TrackedElementsDictionary).FullName.Replace('.', '_'));
          builder.SetReadAccessLevel(DBXS.AccessLevel.Public);
          builder.SetWriteAccessLevel(DBXS.AccessLevel.Vendor);
          builder.SetVendorId("com.mcneel");

          {
            var field = builder.AddSimpleField("UniqueId", typeof(string));
            field.SetDocumentation("Entity owner element UniqueId");
          }

          {
            var field = builder.AddSimpleField("Flags", typeof(int));
            field.SetDocumentation("Element flags on the stream");
          }

          {
            var field = builder.AddArrayField("Authority", typeof(Guid));
            field.SetDocumentation("Stream Authority");
          }

          {
            var field = builder.AddSimpleField("Name", typeof(string));
            field.SetDocumentation("Stream Name");
          }

          {
            var field = builder.AddSimpleField("Index", typeof(int));
            field.SetDocumentation("Element index on the stream");
          }

          {
            var field = builder.AddSimpleField("Data", typeof(string));
            field.SetDocumentation("Element additional stream data");
          }

          schema = builder.Finish();
        }

        Fields.UniqueId = schema.GetField("UniqueId");
        Fields.Flags = schema.GetField("Flags");
        Fields.Authority = schema.GetField("Authority");
        Fields.Name = schema.GetField("Name");
        Fields.Index = schema.GetField("Index");
        Fields.Data = schema.GetField("Data");
        return schema;
      }
    }

    public static DB.FilteredElementCollector Keys(DB.Document doc)
    {
      return new DB.FilteredElementCollector(doc).WherePasses(SchemaFilter);
    }

    public static IList<DB.Element> Keys(DB.Document doc, ElementStreamId id, DB.ElementFilter filter = default)
    {
      if (id.Authority == default) throw new ArgumentNullException(nameof(id.Authority));

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(SchemaFilter);

        if (filter != default)
          elementCollector = elementCollector.WherePasses(filter);

        var authorityComparer = default(AuthorityComparer);
        var keys = new List<DB.Element>();
        foreach (var element in elementCollector)
        {
          if (TryGetValue(element, out var _, out var authority, out var name, out var _))
          {
            if (id.Name != default && id.Name != name)
              continue;

            if (!authorityComparer.Equals(authority, id.Authority))
              continue;

            keys.Add(element);
          }
        }

        return keys;
      }
    }

    public static IList<DB.Element> Keys(DB.Document doc, ICollection<IList<Guid>> authorities, DB.ElementFilter filter = default)
    {
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(SchemaFilter);

        if (filter != default)
          elementCollector = elementCollector.WherePasses(filter);

        var keys = new List<DB.Element>();
        foreach (var element in elementCollector)
        {
          if (TryGetValue(element, out var _, out var authority, out var _, out var _))
          {
            if (!authorities.Contains(authority))
              continue;

            keys.Add(element);
          }
        }

        return keys;
      }
    }

    public static IDictionary<int, T> SortedKeys<T>(DB.Document doc, ElementStreamId id, DB.ElementFilter filter = default)
      where T : DB.Element
    {
      if (id.Authority == default) throw new ArgumentNullException(nameof(id.Authority));
      if (id.Name == default) throw new ArgumentNullException(nameof(id.Name));

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(SchemaFilter);

        if (typeof(T) != typeof(DB.Element))
          elementCollector = elementCollector.OfClass(typeof(T));

        if (filter != default)
          elementCollector = elementCollector.WherePasses(filter);

        var authorityComparer = default(AuthorityComparer);
        var keys = new SortedList<int, T>();
        foreach (var element in elementCollector.OfType<T>())
        {
          if (TryGetValue(element, out var _, out var authority, out var name, out var index))
          {
            if (name == id.Name && authorityComparer.Equals(authority, id.Authority))
            {
              keys.Add(index, element);
            }
          }
        }

        return keys;
      }
    }

    public static void Add(DB.Element element, IList<Guid> authority, string name, int index)
    {
      DBXS.Entity GetEntity(DB.Element e)
      {
        if (e.GetEntity(Schema) is DBXS.Entity entity && entity.Schema != null)
          return entity;

        return new DBXS.Entity(Schema);
      }

      using (var entity = GetEntity(element))
      {
        entity.Set<string>(Fields.UniqueId, element.UniqueId);
        entity.Set<IList<Guid>>(Fields.Authority, authority);
        entity.Set<string>(Fields.Name, name);
        entity.Set<int>(Fields.Index, index);
        element.SetEntity(entity);
      }
    }

    public static bool Remove(DB.Element value) => value.DeleteEntity(Schema);

    public static bool TryGetValue(DB.Element element, out int flags, out IList<Guid> authority, out string name, out int index)
    {
      using (var entity = element.GetEntity(Schema))
      {
        if (entity.Schema is object)
        {
          // Check this Entity was not copied from an other DB.Element.
          if (entity.Get<string>(Fields.UniqueId) == element.UniqueId)
          {
            flags = entity.Get<int>(Fields.Flags);
            authority = entity.Get<IList<Guid>>(Fields.Authority);
            name = entity.Get<string>(Fields.Name);
            index = entity.Get<int>(Fields.Index);
            return true;
          }
        }

        flags = default;
        authority = default;
        name = default;
        index = int.MinValue;
        return false;
      }
    }

    public static bool ContainsKey(DB.Element element)
    {
      using (var entity = element.GetEntity(Schema))
        return entity.Schema != null && entity.Get<string>(Fields.UniqueId) == element.UniqueId;
    }
  }
  #endregion
}
