//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// Implements an equality comparer for <see cref="JToken"/> objects that is
    /// *slightly* more forgiving than the stricter <see cref="JTokenEqualityComparer"/> one.
    /// </summary>
    public sealed class SchemaValueEqualityComparer : IEqualityComparer<JToken>
    {
        public bool Equals([AllowNull] JToken x, [AllowNull] JToken y)
        {
            if (x == null || y == null)
                return x == null && y == null;
            if ((x.Type == JTokenType.Integer && y.Type == JTokenType.Float) ||
                (x.Type == JTokenType.Float && y.Type == JTokenType.Integer))
                return (double)x == (double)y;
            if (x.Type == y.Type)
            {
                if (x.Type == JTokenType.Array)
                {
                    var xArr = (JArray)x;
                    var yArr = (JArray)y;
                    if (xArr.Count == yArr.Count)
                    {
                        for (var i = 0; i < xArr.Count; i++)
                        {
                            if (!Equals(xArr[i], yArr[i]))
                                return false;
                        }
                        return true;
                    }
                    return false;
                }
                if (x.Type == JTokenType.Object)
                {
                    var xObj = (JObject)x;
                    var yObj = (JObject)y;
                    var xProps = xObj.Properties().Select(p => p.Name);
                    var yProps = yObj.Properties().Select(p => p.Name);
                    if (xProps.Count() == yProps.Count() && !xProps.Except(yProps).Any())
                    {
                        foreach (var prop in xProps)
                        {
                            if (!Equals(xObj[prop], yObj[prop]))
                                return false;
                        }
                        return true;
                    }
                    return false;
                }
            }
            return JToken.EqualityComparer.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] JToken obj)
            => obj.GetHashCode();
    }
}
