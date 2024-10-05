using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeltaGenInternal.Core;
internal abstract class Template
{
    public virtual string Name => string.Empty;
    public abstract override string ToString();


    public string Join(string template, string separator, params IEnumerable[] objects)
    {
        List<string> strings = [];
        (var enumerators, var current) = GetEnumerator(objects);
        while (Next(enumerators))
            strings.Add(string.Format(template, Current(enumerators, current)));
        return string.Join(separator, strings);
    }

    public string Join(IEnumerable<string> values, string separator) => string.Join<string>(separator, values);

    public IEnumerable<int> Range(int start, int count) => Enumerable.Range(start, count);
    public IEnumerable<string> RangeSelect(int start, int count, Func<int, string> selector) => Enumerable.Range(start, count).Select(selector);
    public string LoopRange(int start, int count, Func<int, string> selector) => Loop(RangeSelect(start, count, selector));
    public string JoinRange(int start, int count, Func<int, string> selector, string separator) => Join(RangeSelect(start, count, selector), separator);
    public IEnumerable<string> RangeSelect(int count, Func<int, string> selector) => RangeSelect(0, count, selector);
    public string LoopRange(int count, Func<int, string> selector) => Loop(RangeSelect(0, count, selector));
    public string JoinRange(int count, Func<int, string> selector, string separator) => Join(RangeSelect(0, count, selector), separator);


    public string Loop(IEnumerable objects)
    {
        StringBuilder sb = new();
        foreach (var item in objects)
            sb.Append(item.ToString()).AppendLine();
        return sb.ToString();
    }

    public string Loop(string template, params IEnumerable[] objects)
    {
        StringBuilder sb = new();
        (var enumerators, var current) = GetEnumerator(objects);
        while (Next(enumerators))
            sb.Append(string.Format(template, Current(enumerators, current))).AppendLine();
        return sb.ToString();
    }

    private (IEnumerator[] enumerators, object[] current) GetEnumerator(IEnumerable[] objects)
    {
        int length = objects.Length;
        object[] current = new object[length];
        IEnumerator[] enumerators = new IEnumerator[length];
        for (int i = 0; i < length; i++)
            enumerators[i] = objects[i].GetEnumerator();
        return (enumerators, current);
    }

    private bool Next(IEnumerator[] enumerators)
    {
        int length = enumerators.Length;
        Span<bool> moveNext = stackalloc bool[length];
        for (int i = 0; i < length; i++)
            moveNext[i] = enumerators[i].MoveNext();
        return All(moveNext, true) ||
              (All(moveNext, false) ? false : throw new Exception());
    }

    private object[] Current(IEnumerator[] enumerators, object[] current)
    {
        for (int i = 0; i < enumerators.Length; i++)
            current[i] = enumerators[i].Current;
        return current;
    }

    private bool All(Span<bool> span, bool value)
    {
        for (int i = 0; i < span.Length; i++)
            if (span[i] != value)
                return false;
        return true;
    }
}

internal abstract class Template<T> : Template where T : Model
{
    protected T Model { get; private set; }

    public Template(T model)
    {
        Model = model;
    }
}