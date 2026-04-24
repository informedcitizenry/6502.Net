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

using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Compile;

public enum JumpType
{
    None,
    Break,
    Continue,
    Goto,
    Return,
    Exit
}

public readonly struct Jump
{
    public Jump(JumpType jumpType)
    {
        Type = jumpType;
    }

    public Jump(JumpType jumpType, Statement statement)
    {
        Type =  jumpType;
        JumpStatement = statement;
    }
    
    public Jump(int gotoIndex, Statement statement)
    {
        Type = JumpType.Goto;
        GotoIndex = gotoIndex;
        JumpStatement = statement;
    }

    public Jump(Value returnValue, Statement statement)
    {
        Type = JumpType.Return;
        ReturnValue = returnValue;
        JumpStatement = statement;
    }

    public Value? ReturnValue { get; } 
    
    public int GotoIndex { get; } = -1;

    public Statement? JumpStatement { get; }
    
    public JumpType Type { get; }
    
    public static Jump NoJump => new (JumpType.None);
}