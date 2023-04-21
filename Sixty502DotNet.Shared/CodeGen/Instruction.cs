//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Describes a CPU instruction, including its opcode, byte size and disassembly
/// format.
/// </summary>
public sealed class Instruction
{
    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="opcode">The instruction's opcode</param>
    /// <param name="size">The instruction's byte size.</param>
    public Instruction(int opcode, int size = 1)
    {
        Operands = Array.Empty<int>();
        Opcode = opcode;
        Size = size;
        DisassemblyFormat = opcode.Size() == 1 ?
            $".byte {opcode:x2}" :
            $".word {opcode:x4}";
        IsRelative = false;
        Is16BitRelative = false;
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="disassemblyFormat">The instruction's disassembly format.</param>
    /// <param name="opcode">The instruction's opcode</param>
    /// <param name="size">The instruction's byte size.</param>
    /// <param name="isRelative">The flag determining whether the instruction uses
    /// relative mode addressing.</param>
    /// <param name="is16BitRelative">The flag determining whether the instruction uses
    /// 16-bit relative mode addressing.</param>
    public Instruction(string disassemblyFormat, int opcode = 0x00, int size = 1, bool isRelative = false, bool is16BitRelative = false)
    {
        DisassemblyFormat = disassemblyFormat;
        Opcode = opcode;
        Size = size;
        Operands = new int[1] { Size - opcode.Size() };
        IsRelative = isRelative;
        Is16BitRelative = IsRelative && is16BitRelative;
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="disassemblyFormat">The instruction's disassembly format.</param>
    /// <param name="opcode">The instruction's opcodes.</param>
    /// <param name="operands">The size of each of the instruction's operands.</param>
    public Instruction(string disassemblyFormat, int opcode, params int[] operands)
        : this(disassemblyFormat, opcode, false, operands)
    {
    }

    /// <summary>
    /// Construct a new instance of an instruction.
    /// </summary>
    /// <param name="disassemblyFormat">The instruction's disassembly format.</param>
    /// <param name="opcode">The instruction's opcodes.</param>
    /// <param name="isRelative">The flag determining whether the instruction uses
    /// relative mode addressing.</param>
    /// <param name="operands">The size of each of the instruction's operands.</param>
    public Instruction(string disassemblyFormat, int opcode, bool isRelative, params int[] operands)
    {
        DisassemblyFormat = disassemblyFormat;
        Opcode = opcode;
        Operands = operands;
        Size = opcode.Size() + operands.Sum();
        IsRelative = isRelative;
        Is16BitRelative = false;
    }


    public override string ToString() => $"{Opcode:X2}: {DisassemblyFormat}";

    /// <summary>
    /// Gets the instruction's opcode.
    /// </summary>
    public int Opcode { get; init; }

    /// <summary>
    /// Gets the instruction's size in bytes.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the operand sizes.
    /// </summary>
    public int[] Operands { get; }

    /// <summary>
    /// Gets the instruction's disassembly string.
    /// </summary>
    public string DisassemblyFormat { get; }

    /// <summary>
    /// Gets the flag indicating whether the instruction uses relative
    /// addressing.
    /// </summary>
    public bool IsRelative { get; }

    /// <summary>
    /// Gets the flag indicating whether the instruction uses 16-bit relative
    /// addressing.
    /// </summary>
    public bool Is16BitRelative { get; }
}

