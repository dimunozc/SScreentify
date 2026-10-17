using System.Collections.Immutable;
using SScreentify.Core.Geometry;

namespace SScreentify.Core.Annotations;

/// <summary>Annotations in z-order (last is on top) with snapshot-based undo and redo.</summary>
public sealed class AnnotationDocument
{
    private readonly Stack<ImmutableList<Annotation>> _undo = new();
    private readonly Stack<ImmutableList<Annotation>> _redo = new();
    private ImmutableList<Annotation> _items = ImmutableList<Annotation>.Empty;

    public IReadOnlyList<Annotation> Items => _items;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public event Action? Changed;

    public void Add(Annotation a) => Commit(_items.Add(a));

    public void Replace(Annotation next)
    {
        var index = IndexOf(next.Id);
        Commit(_items.SetItem(index, next));
    }

    public void Remove(Guid id) => Commit(_items.RemoveAt(IndexOf(id)));

    public bool Undo() => Step(_undo, _redo);

    public bool Redo() => Step(_redo, _undo);

    public Annotation? HitTest(PointD p, double tolerance)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
            if (_items[i].HitTest(p, tolerance))
                return _items[i];
        return null;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _items = ImmutableList<Annotation>.Empty;
        Changed?.Invoke();
    }

    private int IndexOf(Guid id)
    {
        var index = _items.FindIndex(a => a.Id == id);
        return index >= 0 ? index : throw new KeyNotFoundException($"No existe la anotación {id}");
    }

    private void Commit(ImmutableList<Annotation> next)
    {
        _undo.Push(_items);
        _redo.Clear();
        _items = next;
        Changed?.Invoke();
    }

    private bool Step(Stack<ImmutableList<Annotation>> from, Stack<ImmutableList<Annotation>> to)
    {
        if (from.Count == 0) return false;
        to.Push(_items);
        _items = from.Pop();
        Changed?.Invoke();
        return true;
    }
}
