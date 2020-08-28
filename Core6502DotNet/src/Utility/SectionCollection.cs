//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
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
            public Section(Token parameters)
            {
                if (parameters.Children.Count == 0 || parameters.Children[0].Children.Count == 0)
                    throw new SyntaxException(parameters.Position, "Section definition missing parameters.");
                var parms = parameters.Children;
                if (parms.Count < 3)
                    throw new SyntaxException(parameters.Position, "Section definition missing one or more parameters.");
                if (parms.Count > 3)
                    throw new SyntaxException(parameters.LastChild.Position, 
                        $"Unexpected parameter \"{parms[3]}\" in section definition.");

                Name = parms[0].ToString();
                if (!Name.EnclosedInDoubleQuotes())
                    throw new SyntaxException(parameters.Position, "Section name must be a string.");
                Starts = Convert.ToInt32(Evaluator.Evaluate(parms[1], short.MinValue, ushort.MaxValue));
                Ends = Convert.ToInt32(Evaluator.Evaluate(parms[2], short.MinValue, ushort.MaxValue));
                if (Starts >= Ends)
                    throw new SyntaxException(parms[2].Position, 
                        "Section definition invalid. Start address must be less than end address.");
                Selected = false;
            }

            public bool AddressInBounds(int address) => address >= Starts && address < Ends;

            public string Name { get; }

            public int Starts { get; }

            public int Ends { get; }

            public bool Selected { get; set; }
        }

        #endregion

        #region Members

        readonly Dictionary<string, Section> _collection;

        Section _current;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="SectionCollection"/> class.
        /// </summary>
        public SectionCollection() => _collection = new Dictionary<string, Section>();

        #endregion

        #region Methods

        /// <summary>
        /// Add a section to the collection.
        /// </summary>
        /// <param name="operands">The parsed operands for the section (name, start address, and end address).</param>
        /// <param name="name">A string representing the parsed section name.</param>
        /// <returns>The <see cref="CollectionResult"/> of the attempt to add the section to the collection..</returns>
        /// <exception cref="ExpressionException"/>
        public CollectionResult Add(Token operands, out string name) 
        {
            Section section = new Section(operands);
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
            _collection.Values.ToList().ForEach(s => s.Selected = false);
        }

        /// <summary>
        /// Sets the current section by name.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <returns>The <see cref="CollectionResult"/> of setting the section.</returns>
        public CollectionResult SetCurrentSection(string name)
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
            _collection.Where(kvp => !kvp.Value.Selected).Select(kvp => kvp.Key);

        #endregion
    }
}