//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{

    /// <summary>
    /// An error for an invalid operation with a section.
    /// </summary>
    public class SectionException : ExpressionException
    {
        /// <summary>
        /// Creates an instance of a section error.
        /// </summary>
        /// <param name="position">The position in the source line that is the source of the error.</param>
        /// <param name="">The custom section error message.</param>
        public SectionException(int position, string message)
            : base(position, message)
        {

        }
    }

    /// <summary>
    /// The result of an attempt to add a section to a <see cref="SectionCollection"/>.
    /// </summary>
    public enum CollectionResult
    {
        Success,
        RangeOverlap,
        Duplicate,
        PreviouslySelected,
        NotFound
    }

    /// <summary>
    /// A collection of named sections with start and end addresses.
    /// </summary>
    public class SectionCollection
    {
        #region Subclasses
        class Section
        {
            public Section(StringView name, int starts, int ends)
            {
                Name = name;
                Starts = starts;
                Ends = ends;
                Selected = false;
                OutputCount = 0;
            }

            public bool AddressInBounds(int address) => address >= Starts && address < Ends;

            public StringView Name { get; }

            public int Starts { get; }

            public int Ends { get; }

            public bool Selected { get; set; }

            public int OutputCount { get; set; }
        }

        #endregion

        #region Members

        readonly Dictionary<StringView, Section> _collection;

        Section _current;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="SectionCollection"/> class.
        /// </summary>
        public SectionCollection(bool caseSensitive)
            => _collection = new Dictionary<StringView, Section>(caseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase);

        #endregion

        #region Methods

        /// <summary>
        /// Add a section to the collection.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="starts">The section start address.</param>
        /// <param name="ends">The section end address.</param>
        /// <returns>The <see cref="CollectionResult"/> of the attempt to add the section to the collection..</returns>
        /// <exception cref="ExpressionException"/>
        public CollectionResult Add(StringView name, int starts, int ends)
        {
            Section section = new Section(name, starts, ends);
            name = section.Name;

            if (_collection.Any(kvp =>
                section.Starts <= (kvp.Value.Ends - 1) && kvp.Value.Starts <= (section.Ends - 1)))
                return CollectionResult.RangeOverlap;

            if (!_collection.TryAdd(section.Name, section))
                return CollectionResult.Duplicate;

            return CollectionResult.Success;
        }

        /// <summary>
        /// Resets the collection, while preserving all its elements.
        /// </summary>
        public void Reset()
        {
            _current = null;
            _collection.Values.ToList().ForEach(s =>
            {
                s.OutputCount = 0;
                s.Selected = false;
            });
        }

        /// <summary>
        /// Gets the start address for a given section.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <returns>The start address for the section.</returns>
        public int GetSectionStart(StringView name)
        {
            if (_collection.TryGetValue(name, out var section))
                return section.Starts;
            return int.MinValue;
        }

        /// <summary>
        /// Sets the current section by name.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <returns>The <see cref="CollectionResult"/> of setting the section.</returns>
        public CollectionResult SetCurrentSection(StringView name)
        {
            if (_collection.TryGetValue(name, out _current))
            {
                if (_current.Selected)
                    return CollectionResult.PreviouslySelected;
                _current.Selected = true;
                return CollectionResult.Success;
            }
            return CollectionResult.NotFound;
        }

        /// <summary>
        /// Get the currently selected section's output count.
        /// </summary>
        /// <returns>The output count of the selected section.</returns>
        public int GetSectionOutputCount()
        {
            if (_current != null)
                return _current.OutputCount;
            return -1;
        }


        /// <summary>
        /// Sets the output byte count of the current section.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns><c>true</c> if the current section's output count was updated,
        /// otherwise <c>false</c>.</returns>
        public bool SetOutputCount(int count)
        {
            if (_current != null)
            {
                _current.OutputCount = count;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the specified address is in the current selected section's bounds.
        /// </summary>
        /// <param name="address">The address to test.</param>
        /// <returns><c>true</c>, if the address lies within the current selected section's bounds,
        /// otherwise <c>false</c>.</returns>
        public bool AddressInBounds(int address) =>
            _current != null && _current.AddressInBounds(address);

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value that indicates whether the collection is empty.
        /// </summary>
        public bool IsEmpty => _collection.Count == 0;

        /// <summary>
        /// Gets the selected section's start address, or the minimum <see cref="int"/> value
        /// if no section is selected.
        /// </summary>
        public int SelectedStartAddress => _current != null ? _current.Starts : int.MinValue;

        /// <summary>
        /// Gets if a section has been selected.
        /// </summary>
        public bool SectionSelected => _current != null;

        /// <summary>
        /// Gets the names of the sections in the collection that have not been selected.
        /// </summary>
        public IEnumerable<string> SectionsNotSelected =>
            _collection.Where(kvp => !kvp.Value.Selected).Select(kvp => kvp.Key.ToString());

        #endregion
    }
}