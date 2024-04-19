//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

public sealed partial class Interpreter : SyntaxParserBaseVisitor<int>
{
    public override int VisitInstructionCpu([NotNull] SyntaxParser.InstructionCpuContext context)
    {
        if (Services.State.Symbols.InFunctionScope)
        {
            throw new Error(context, "Assembly code generation not permitted in function blocks");
        }
        try
        {
            if (!_encoder.Visit(context.cpuInstruction()))
            {
                throw new Error(context, "Addressing mode not supported");
            }
        }
        catch (Warning w)
        {
            Services.State.Warnings.Add(w);
        }
        Services.State.Output.Add(context.cpuInstruction().opcode, context.cpuInstruction().opcodeSize);
        if (context.cpuInstruction().operandSize > 0)
        {
            Services.State.Output.Add(context.cpuInstruction().operand, context.cpuInstruction().operandSize);
        }
        if (Services.State.InFirstPass)
        {
            int pc = Services.State.LongLogicalPCOnAssemble;
            _analysisContexts.Add(
                new CodeAnalysisContext(_encoder.Cpuid,
                    Services.State.Output.GetBytesFrom(pc),
                    pc,
                context));
        }
        return GenListing(context, '.', true);
    }
}

