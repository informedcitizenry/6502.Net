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

namespace Sixty502DotNet.Shared.Lex;

public static class CharExtensions
{
    extension(char c)
    {
        public bool IsHorizontalWhitespace()
            => c is
                '\u0009' or
                '\u0020' or
                '\u00a0' or
                '\u1680' or
                '\u180e' or
                '\u2000' or
                '\u2001' or
                '\u2002' or
                '\u2003' or
                '\u2004' or
                '\u2005' or
                '\u2006' or
                '\u2008' or
                '\u2009' or
                '\u200a' or
                '\u202f' or
                '\u3000' or
                '\u205f';
        
        public bool IsVerticalWhitespace() 
            => c is 
                '\n' or 
                '\r' or 
                '\u0085' or 
                '\u2028' or 
                '\u2029';

        public bool IsIdentHead()
            => char.IsLetter(c) || c == '_';

        public bool IsIdent()
            => c.IsIdentHead() || char.IsDigit(c);

        public bool IsBaseDigit(int radix)
            => char.IsAsciiDigit(c) || (radix == 16 && char.IsAsciiHexDigit(c));
        
        public bool IsBasePrefix()
            => c is 'B' or 'O' or 'X' or 'b' or 'o' or 'x';
        
        public bool IsExponent()
            => c is 'E' or 'P' or 'e' or 'p';
    }
}