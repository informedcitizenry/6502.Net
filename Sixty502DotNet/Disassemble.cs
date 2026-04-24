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

using System.Diagnostics;
using Sixty502DotNet.CLI;
using Sixty502DotNet.Shared;
using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Lex;

namespace Sixty502DotNet;

public static class Disassemble
{
    public static int WithOptions(ParsedCliOptions options)
    {
        if (!options.Quiet)
        {
            Console.WriteLine(App.Name);
            Console.WriteLine(App.Version);
            if (options.InputPaths.Count > 1)
            {
                Console.WriteLine("Ignoring extra input files.");
            }
        }
        Debug.Assert(options.DisassemblyOptions != null);
        var codeBytes = GetCodeBytes(options.InputPaths[0], options.DisassemblyOptions.IncludePath);
        if (codeBytes == null)
        {
            Assemble.WriteFatalError($"Cannot open file `{options.InputPaths[0]}` for reading.");
            return 1;
        }
        var cpu = Cpu.M6502;
        if (!string.IsNullOrEmpty(options.Cpu))
        {
            var optionCpu = CpuLookup.ByName(options.Cpu);
            if (optionCpu == null)
            {
                Assemble.WriteFatalError($"Option `{options.Cpu}` is not valid.");
                return 1;
            }
            cpu = optionCpu.Value;
        }
        try
        {
            var output = Disassembler.Disassemble(codeBytes, cpu, options.DisassemblyOptions);
            var fullOutput = $";; Output of {options.Output}\n\n{output}";
            File.WriteAllText(options.Output, fullOutput);
            Console.WriteLine("Disassembly complete.");
            return 0;
        }
        catch (DecodeException decodeException)
        {
            Assemble.WriteFatalError(decodeException.Message);
            return 1;
        }
        catch
        {
            Assemble.WriteFatalError("error: Could not write to output file.");
            return 1;
        }
    }

    private static byte[]? GetCodeBytes(string input, string? includePath)
    {
        var filePath = FileSourceReader.GetFilePath(input, includePath);
        return !string.IsNullOrEmpty(filePath) ? File.ReadAllBytes(filePath) : null;
    }
}