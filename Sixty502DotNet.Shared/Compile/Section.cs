// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Sixty502DotNet.Shared.Compile;

public enum SectionResult
{
    /// <summary>
    /// A successful addition to the section collection.
    /// </summary>
    Success,
    /// <summary>
    /// An unsuccessful addition to the section collection due to overlapping
    /// start and address ranges with an existing section.
    /// </summary>
    RangeOverlap,
    /// <summary>
    /// An unsuccessful addition to the section collection due to existing
    /// section name.
    /// </summary>
    Duplicate,
    /// <summary>
    /// An unsuccessful reference to a section in the collection due to the
    /// section being previously selected.
    /// </summary>
    PreviouslySelected,
    /// <summary>
    /// An unsuccessful reference to a section in the collection due to the
    /// section not being found.
    /// </summary>
    NotFound
}

public sealed class SectionCollection(StringComparer comparer)
{
    private class Section(string name, int starts, int ends)
    {
        public bool AddressInBounds(int address) => address >= Starts && address < Ends;

        public string Name { get; } = name;

        public int Starts { get; } = starts;

        public int Ends { get; } = ends;

        public bool Selected { get; set; }

        public int OutputCount { get; set; }
    }
    
    private readonly Dictionary<string, Section> _collection = new(comparer);

    Section? _current;

    public SectionResult Add(string name, int starts, int ends)
    {
        Section section = new(name, starts, ends);

        if (_collection.Any(kvp =>
                section.Starts <= kvp.Value.Ends - 1 && kvp.Value.Starts <= section.Ends - 1))
            return SectionResult.RangeOverlap;

        return !_collection.TryAdd(section.Name, section) 
            ? SectionResult.Duplicate 
            : SectionResult.Success;
    }
    
    public void Reset()
    {
        _current = null;
        _collection.Values.ToList().ForEach(s =>
        {
            s.OutputCount = 0;
            s.Selected = false;
        });
    }
    
    public int GetSectionStart(string name)
    {
        if (_collection.TryGetValue(name, out var section))
            return section.Starts;
        return int.MinValue;
    }
    
    public SectionResult SetCurrentSection(string name)
    {
        if (_collection.TryGetValue(name, out _current))
        {
            if (_current.Selected)
                return SectionResult.PreviouslySelected;
            _current.Selected = true;
            return SectionResult.Success;
        }
        return SectionResult.NotFound;
    }
    
    public int GetSectionOutputCount()
    {
        if (_current != null)
            return _current.OutputCount;
        return -1;
    }
    
    public bool SetOutputCount(int count)
    {
        if (_current != null)
        {
            _current.OutputCount = count;
            return true;
        }
        return false;
    }
    
    public bool AddressInBounds(int address) =>
        _current != null && _current.AddressInBounds(address);

    public IList<string> GetUnusedSections() 
        => (from section in _collection 
            where !section.Value.Selected select section.Key).ToList();

    public bool IsEmpty => _collection.Count == 0;

    public int SelectedStartAddress => _current?.Starts ?? int.MinValue;

    public int SelectedEndAddress => _current?.Ends ?? int.MaxValue;
    
    public bool SectionSelected => _current != null;
    
    public IEnumerable<string> SectionsNotSelected =>
        _collection.Where(kvp => !kvp.Value.Selected).Select(kvp => kvp.Key.ToString());

}