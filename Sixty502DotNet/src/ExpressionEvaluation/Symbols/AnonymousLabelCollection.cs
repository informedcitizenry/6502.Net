//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// Maintains a collection of <see cref="AnonymousLabel"/> symbols and
    /// resolves references to them.
    /// </summary>
    public class AnonymousLabelCollection
    {
        readonly private List<KeyValuePair<int, AnonymousLabel>> _forwardRefs;
        readonly private List<KeyValuePair<int, AnonymousLabel>> _backwardRefs;
        readonly private IScope _scope;

        /// <summary>
        /// Construct a new instance of the <see cref="AnonymousLabelCollection"/>
        /// class.
        /// </summary>
        /// <param name="scope">The collection's scope.</param>
        public AnonymousLabelCollection(IScope scope)
        {
            _scope = scope;
            _forwardRefs = new List<KeyValuePair<int, AnonymousLabel>>();
            _backwardRefs = new List<KeyValuePair<int, AnonymousLabel>>();
        }

        /// <summary>
        /// Define a <see cref="AnonymousLabel"/> in the collection.
        /// </summary>
        /// <param name="reference"></param>
        public void Define(AnonymousLabel reference)
        {
            if (reference.LabelType == AnonymousLabel.Backward)
            {
                _backwardRefs.Insert(0, new KeyValuePair<int, AnonymousLabel>(reference.Index, reference));
            }
            else
            {
                _forwardRefs.Add(new KeyValuePair<int, AnonymousLabel>(reference.Index, reference));
            }
        }

        /// <summary>
        /// Resolve a <see cref="AnonymousLabel"/> in the collection.
        /// </summary>
        /// <param name="atIndex">The index of the anonymous label.</param>
        /// <returns>The label if found at the index, otherwise
        /// <c>null</c>.</returns>
        public AnonymousLabel? Resolve(int atIndex)
        {
            var lineRef = _forwardRefs.FirstOrDefault(lr => lr.Key == atIndex);
            if (lineRef.Value == null)
            {
                lineRef = _backwardRefs.FirstOrDefault(lr => lr.Key == atIndex);
            }
            if (lineRef.Value == null)
            {
                return _scope?.EnclosingScope?.ResolveAnonymousLabel(atIndex);
            }
            return lineRef.Value;
        }

        /// <summary>
        /// Get the collection of all unreferenced anonymous labels.
        /// </summary>
        /// <returns>A collection of unreferenced anonymous labels.</returns>
        public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
        {
            var unreferenced = new List<SymbolBase>();
            foreach (var ref_ in _forwardRefs)
            {
                if (!ref_.Value.IsReferenced)
                    unreferenced.Add(ref_.Value);
            }
            foreach (var ref_ in _backwardRefs)
            {
                if (!ref_.Value.IsReferenced)
                    unreferenced.Add(ref_.Value);
            }
            return unreferenced.AsReadOnly();
        }

        /// <summary>
        /// Resolve a <see cref="AnonymousLabel"/> in the collection.
        /// </summary>
        /// <param name="name">The reference name.</param>
        /// <param name="fromIndex">The starting index from which to
        /// refer.</param>
        /// <returns>The anonymous label if found by name from the given index,
        /// otherwise <c>null</c>.</returns>
        public AnonymousLabel? Resolve(string name, int fromIndex)
        {
            IList<KeyValuePair<int, AnonymousLabel>> tolook;
            int refsCount;
            if (name[0] == '+')
            {
                tolook = _forwardRefs.Where(kvp => kvp.Key > fromIndex).ToList();
                refsCount = tolook.Count;
            }
            else
            {
                tolook = _backwardRefs.Where(kvp => kvp.Key <= fromIndex).ToList();
                refsCount = tolook.Count;
            }
            int remain = name.Length - refsCount;
            if (remain <= 0)
            {
                return tolook[name.Length - 1].Value;
            }
            // not in my scope, is it in my parent's?
            string trimmed = refsCount == 0 ? name : name.Substring(0, remain);
            return _scope.EnclosingScope?.ResolveAnonymousLabel(trimmed, fromIndex);
        }
    }
}
