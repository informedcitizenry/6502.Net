//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace Sixty502DotNet
{
    public class LabelListing
    {
        private readonly Dictionary<string, string> _listings;

        public LabelListing() => _listings = new Dictionary<string, string>();

        public void Log(string symbol, Value value)
            => _listings[symbol] = value.ToString();

        public void Log(IValueResolver symbol)
            => Log(symbol.Name, symbol.Value);

        public void Log(Label label)
        {
            if (label.Value.IsIntegral)
            {
                _listings[label.Name] = $"${label.Value:x}";
            }
            else
            {
                _listings[label.Name] = label.Value.ToString();
            }
        }

        public void Clear()
            => _listings.Clear();

        public override string ToString()
        {
            var sb = new StringBuilder(
                "/*****************************************************************************/\n" +
                "/* Symbol                              Value                                 */\n" +
                "/*****************************************************************************/\n");
            foreach (var listing in _listings)
            {
                sb.AppendLine($" {listing.Key.Substring(0, 34),-36}={listing.Value.Substring(0, 37)}");
            }
            return sb.ToString();
        }
    }
}
