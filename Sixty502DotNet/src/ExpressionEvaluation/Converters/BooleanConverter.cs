//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    public class BooleanConverter : ICustomConverter
    {
        public Value Convert(string str) => new(System.Convert.ToBoolean(str));
    }
}
