//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Sixty502DotNet.Shared;
using System.Reflection;

namespace Sixty502DotNet.CLI;

public static class Disassembler
{
    private static readonly HashSet<string> s_excluded =
    [
        "Disassemble", "InputFiles", "OutputFile"
    ];
    
	private static bool CheckOptions(CommandLineOptions cliOptions)
	{
        var typeInfo = typeof(CommandLineOptions).GetTypeInfo();
        var properties = typeInfo.GetProperties();
        foreach (var property in properties)
        {
            if (s_excluded.Contains(property.Name))
            {
                continue;
            }
            var optionAttribute = property.GetCustomAttribute<CommandLine.OptionAttribute>();
            if (optionAttribute == null)
            {
                continue;
            }
            var propValue = property.GetValue(cliOptions);
            var propType = property.PropertyType;

            if (!IsOptionSet(propValue, propType))
            {
                continue;
            }
            var optionName = !string.IsNullOrEmpty(optionAttribute.LongName) 
                        ? $"--{optionAttribute.LongName}"
                        : $"-{optionAttribute.ShortName}";
            Output.OutputError(new Warning($"Command-line option '{optionName}' is ignored sin disassembly mode"), false);
        }
        if (cliOptions.InputFiles?.Count < 1)
        {
            Output.OutputError(new Error("Input file(s) not specified"), false);
            return false;
        }
        return true;
    }

    private static bool IsOptionSet(object? value, Type type)
    {
        if (value == null)
        {
            return false;
        }
        if (type.IsInterface)
        {
            return ((IList<string>)value).Count > 0;
        }
        if (type.IsValueType)
        {
            return (bool)value;
        }
        return !string.IsNullOrEmpty((string)value);
    }

	public static void Disassemble(CommandLineOptions cliOptions)
	{
        Output.OutputProductInfo();

        if (!CheckOptions(cliOptions))
        {
            return;
        }
        FileSystemBinaryReader binaryReader = new(cliOptions.IncludePath);

        List<byte[]> objectCode = [];
        for (int i = 0; i < cliOptions.InputFiles!.Count; i++)
        {
            try
            {
                objectCode.Add(binaryReader.ReadAllBytes(cliOptions.InputFiles[i]));
            }
            catch
            {
                Output.OutputError(new Error($"Could not read file '{cliOptions.InputFiles[i]}'"), true);
                return;
            }
        }
        Options options = OptionsFactory.FromCLIOptions(cliOptions);
        Interpreter interpreter = new(options, new FileSystemBinaryReader(null));

        string disassembly = interpreter.Disassemble(objectCode, cliOptions.DisassemblyStart, cliOptions.DisassemblyOffset);
        File.WriteAllText(cliOptions.OutputFile, disassembly);

        Console.WriteLine("-------------------------------------");
        Console.WriteLine("Disassembly file created.");
    }
}

