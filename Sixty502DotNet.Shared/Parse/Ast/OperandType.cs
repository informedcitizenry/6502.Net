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

namespace Sixty502DotNet.Shared.Parse.Ast;

public enum OperandType
{
    Implied,
    /// <summary>
    /// <c>#n</c>
    /// </summary>
    Immediate,
    /// <summary>
    /// <c>r, n</c>
    /// </summary>
    Immediate80,
    /// <summary>
    /// <c>#n, n</c>
    /// </summary>
    ImmediateBranch,
    /// <summary>
    /// <c>#n, n, r</c>
    /// </summary>
    ImmediateBranchIndexed,
    /// <summary>
    /// <c>hl, sp+n</c>  
    /// </summary>
    GbStackOffset,
    /// <summary>
    /// <c>a, (n+n)</c>
    /// </summary>
    GbImmediateIndirect,
    /// <summary>
    /// <c>a, (n+c)</c>
    /// </summary>
    GbImmediateIndirectIndexed,
    /// <summary>
    /// <c>(n+n), a</c>
    /// </summary>
    GbIndirect,
    /// <summary>
    /// <c>(n+c), a</c>
    /// </summary>
    GbIndirectIndexed,
    /// <summary>
    /// <c>a, (hl+)</c>
    /// </summary>
    GbAccumulatorHlIncrement,
    /// <summary>
    /// <c>a, (hl-)</c>
    /// </summary>
    GbAccumulatorHlDecrement,
    /// <summary>
    /// <c>(hl+), a</c>
    /// </summary>
    GbHlIncrementAccumulator,
    /// <summary>
    /// <c>(hl-), a</c>
    /// </summary>
    GbHlDecrementAccumulator,
    /// <summary>
    /// <c>n</c>
    /// </summary>
    Address,
    /// <summary>
    /// <c>n, n</c>
    /// </summary>
    TwoExpression,
    /// <summary>
    /// <c>n, n, n</c>
    /// </summary>
    ThreeExpression,
    /// <summary>
    /// <c>(n)</c>
    /// </summary>
    Indirect,
    /// <summary>
    /// <c>[n]</c>
    /// </summary>
    IndirectLong,
    /// <summary>
    /// <c>n, r</c>
    /// </summary>
    Indexed,
    /// <summary>
    /// <c>(r+n)</c>
    /// </summary>
    Indexed80,
    /// <summary>
    /// <c>n, (r+d)</c>
    /// </summary>
    Indexed80Bit,
    /// <summary>
    /// <c>(n, r)</c>
    /// </summary>
    IndexedIndirect,
    /// <summary>
    /// <c>(n, r), r</c>
    /// </summary>
    IndexedIndirectIndexed,
    /// <summary>
    /// <c>(n), r</c>
    /// </summary>
    IndirectIndexed,
    /// <summary>
    /// <c>(r+d), r</c>
    /// </summary>
    IndirectIndexed80,
    /// <summary>
    /// <c>b, (r+d), r</c>
    /// </summary>
    IndirectIndexed80Bit,
    /// <summary>
    /// <c>(r+d), n</c>
    /// </summary>
    IndirectIndexed80Immediate,
    /// <summary>
    /// <c>[n, r]</c>
    /// </summary>
    IndexedIndirect6809,
    /// <summary>
    /// <c>[n], y</c>
    /// </summary>
    IndirectLongIndexed,
    /// <summary>
    /// <c>[n], z</c>
    /// </summary>
    IndirectLongZ,
    /// <summary>
    /// <c>(r)</c>
    /// </summary>
    IndirectRegister,
    /// <summary>
    /// <c>(r), n</c>
    /// </summary>
    IndirectRegisterImmediate,
    /// <summary>
    /// <c>r, r</c>
    /// </summary>
    RegisterRegister,
    /// <summary>
    /// <c>(r), r</c>
    /// </summary>
    IndirectRegisterRegister,
    /// <summary>
    /// <c>b, (hl)</c>
    /// </summary>
    IndirectHlBit,
    /// <summary>
    /// <c>r</c>
    /// </summary>
    Register,
    /// <summary>
    /// <c>r, (r)</c>
    /// </summary>
    RegisterIndirectRegister,
    /// <summary>
    /// <c>r, (n)</c>
    /// </summary>
    RegisterIndirect,
    /// <summary>
    /// <c>r, (r+d)</c>
    /// </summary>
    RegisterIndirectIndexed80,
    /// <summary>
    /// <c>r, r, r[,..r]</c>
    /// </summary>
    RegisterList,
    /// <summary>
    /// <c>,r</c>
    /// </summary>
    RegisterOffset,
    /// <summary>
    /// <c>[,r]</c>
    /// </summary>
    IndirectRegisterOffset,
    /// <summary>
    /// <c>[r, r]</c>
    /// </summary>
    IndirectRegisterRegister6809,
    /// <summary>
    /// <c>[, --r]</c>
    /// </summary>
    IndirectAutoDecrement,
    /// <summary>
    /// <c>[,r++]</c>
    /// </summary>
    IndirectAutoIncrement,
    /// <summary>
    /// <c>,-r</c>
    /// </summary>
    AutoDecrement,
    /// <summary>
    /// <c>,r+</c>
    /// </summary>
    AutoIncrement,
    /// <summary>
    /// <c>,--r</c>
    /// </summary>
    AutoDecrement2,
    /// <summary>
    /// <c>,r++</c>
    /// </summary>
    AutoIncrement2,
    /// <summary>
    /// <c>r, [n]</c>
    /// </summary>
    RegisterDirect,
    /// <summary>
    /// <c>r, [r]</c>
    /// </summary>
    RegisterIndirectRegister86,
    /// <summary>
    /// <c>r, [r+r]</c>
    /// </summary>
    RegisterBaseIndex,
    /// <summary>
    /// <c>r, [r+n]</c>
    /// </summary>
    RegisterBaseDisplacement,
    /// <summary>
    /// <c>r, [r+r+n]</c>
    /// </summary>
    RegisterBaseIndexDisplacement,
    /// <summary>
    /// <c>[r]</c>
    /// </summary>
    IndirectRegister86,
    /// <summary>
    /// <c>[r], r</c>
    /// </summary>
    IndirectRegister86Register,
    /// <summary>
    /// <c>[r], n</c>
    /// </summary>
    IndirectRegister86Imm,
    /// <summary>
    /// <c>[n], r</c>
    /// </summary>
    DirectRegister,
    /// <summary>
    /// <c>[n], n</c>
    /// </summary>
    DirectImm,
    /// <summary>
    /// <c>[r+r]</c>
    /// </summary>
    BaseIndex,
    /// <summary>
    /// <c>[r+r], r</c>
    /// </summary>
    BaseIndexRegister,
    /// <summary>
    /// <c>[r+r], n</c>
    /// </summary>
    BaseIndexImm,
    /// <summary>
    /// <c>[r+n]</c>
    /// </summary>
    BaseDisplacement,
    /// <summary>
    /// <c>[r+n], r</c>
    /// </summary>
    BaseDisplacementRegister,
    /// <summary>
    /// <c>[r+n], n</c>
    /// </summary>
    BaseDisplacementImm,
    /// <summary>
    /// <c>[r+r+n]</c>
    /// </summary>
    BaseIndexDisplacement,
    /// <summary>
    /// <c>[r+r+n], r</c>
    /// </summary>
    BaseIndexDisplacementRegister,
    /// <summary>
    /// <c>[r+r+n], n</c>
    /// </summary>
    BaseIndexDisplacementImm,
    /// <summary>
    /// <c>r, r:[n]</c>
    /// </summary>
    RegisterSegmentOverrideDirect,
    /// <summary>
    /// <c>r, r:[r]</c>
    /// </summary>
    RegisterSegmentOverrideRegister,
    /// <summary>
    /// <c>r, r:[r+r]</c>
    /// </summary>
    RegisterSegmentOverrideBaseIndex,
    /// <summary>
    /// <c>r, r:[r+n]</c>
    /// </summary>
    RegisterSegmentOverrideBaseDisplacement,
    /// <summary>
    /// <c>r, r:[r+r+n]</c>
    /// </summary>
    RegisterSegmentOverrideBaseIndexDisplacement,
    /// <summary>
    /// <c>r:[n]</c>
    /// </summary>
    SegmentOverrideDirect,
    /// <summary>
    /// <c>n:n</c>
    /// </summary>
    SegmentAbsoluteDirect,
    /// <summary>
    /// <c>r:[n], r</c>
    /// </summary>
    SegmentOverrideDirectRegister,
    /// <summary>
    /// <c>r:[n], n</c>
    /// </summary>
    SegmentOverrideDirectImm,
    /// <summary>
    /// <c>r:[r]</c>
    /// </summary>
    SegmentOverrideRegister,
    /// <summary>
    /// <c>r:[r], r</c>
    /// </summary>
    SegmentOverrideRegisterRegister,
    /// <summary>
    /// <c>r:[r], r:[r]</c>
    /// </summary>
    SegmentOverrideRegisterSegmentOverrideRegister,
    /// <summary>
    /// <c>r:[r], n</c>
    /// </summary>
    SegmentOverrideRegisterImm,
    /// <summary>
    /// <c>r:[r+r]</c>
    /// </summary>
    SegmentOverrideBaseIndex,
    /// <summary>
    /// <c>r:[r+r], r</c>
    /// </summary>
    SegmentOverrideBaseIndexRegister,
    /// <summary>
    /// <c>r:[r+r], n</c>
    /// </summary>
    SegmentOverrideBaseIndexImm,
    /// <summary>
    /// <c>r:[r+n]</c>
    /// </summary>
    SegmentOverrideBaseDisplacement,
    /// <summary>
    /// <c>r:[r+n], r</c>
    /// </summary>
    SegmentOverrideBaseDisplacementRegister,
    /// <summary>
    /// <c>r:[r+n], n</c>
    /// </summary>
    SegmentOverrideBaseDisplacementImm,
    /// <summary>
    /// <c>r:[r+r+n]</c>
    /// </summary>
    SegmentOverrideBaseIndexDisplacement,
    /// <summary>
    /// <c>r:[r+r+n], r</c>
    /// </summary>
    SegmentOverrideBaseIndexDisplacementRegister,
    /// <summary>
    /// <c>r:[r+r+n], n</c>
    /// </summary>
    SegmentOverrideBaseIndexDisplacementImm
}