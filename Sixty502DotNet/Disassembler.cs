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
    private static readonly HashSet<string> s_optionsNotIgnored =
    [
        "Disassemble", 
        "ErrorFile", 
        "IncludePath",
        "InputFiles", 
        "OutputFile", 
        "Quiet", 
        "WarningsAsErrors"
    ];
    
	private static (List<Error>, List<Warning>) CheckOptions(CommandLineOptions cliOptions)
	{
        var typeInfo = typeof(CommandLineOptions).GetTypeInfo();
        var errors = new List<Error>();
        var warnings = new List<Warning>();
        var properties = typeInfo.GetProperties();
        foreach (var property in properties)
        {
            if (s_optionsNotIgnored.Contains(property.Name))
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
            var message = $"Command-line option '{optionName}' is ignored in disassembly mode";
            if (cliOptions.WarningsAsErrors)
            {
                errors.Add(new Error(message));
            }
            else
            {
                warnings.Add(new Warning(message));
            }
        }
        if (cliOptions.InputFiles?.Count < 1)
        {
            errors.Add(new Error("Input file(s) not specified"));
        }
        return (errors, warnings);
    }

    private static bool IsOptionSet(object? value, Type type)
    {
        if (value == null) return false;
        if (type.IsValueType) return (bool)value;
        if (type.IsInterface) return ((IList<string>)value).Count > 0;
        return !string.IsNullOrEmpty((string)value);
    }

	public static void Disassemble(CommandLineOptions cliOptions)
	{
        Output.OutputProductInfo();
        var (errors, warnings) = CheckOptions(cliOptions);
        if (errors.Count != 0)
        {
            goto OutputErrors;
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
                errors.Add(new Error($"Could not read file '{cliOptions.InputFiles[i]}'"));
                goto OutputErrors;
            }
        }
        Options options = OptionsFactory.FromCLIOptions(cliOptions);
        Interpreter interpreter = new(options, new FileSystemBinaryReader(null));

        string disassembly = interpreter.Disassemble(objectCode, cliOptions.DisassemblyStart, cliOptions.DisassemblyOffset);
        File.WriteAllText(cliOptions.OutputFile, disassembly);

        Console.WriteLine("-------------------------------------");
        Console.WriteLine("Disassembly file created.");
OutputErrors:
        Output.OutputErrorsAndWarnings(errors, warnings, cliOptions.ErrorFile, true);
    }
}

