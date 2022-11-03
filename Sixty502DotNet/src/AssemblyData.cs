using System.Collections.Generic;
using Sixty502DotNet.Runtime;

namespace Sixty502DotNet;

public class AssemblyData
{
    public byte[] ProgramBinary { get; }
    public IEnumerable<DebugEntry> DebugInfo { get; }

    public AssemblyData(byte[] programBinary, IEnumerable<DebugEntry> debugInfo)
    {
        ProgramBinary = programBinary;
        DebugInfo = debugInfo;
    }
}