//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Sixty502DotNet.Shared;

namespace Sixty502DotNet.CLI;

public static class Output
{
    public static void OutputProductInfo()
    {
        var assemblyName = Assembly.GetEntryAssembly()!.GetName();
        var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);

        Console.WriteLine($"{versionInfo.Comments}\n{versionInfo.LegalCopyright}");
        Console.WriteLine($"Version {assemblyName.Version!.Major}.{assemblyName.Version.Minor} Build {assemblyName.Version.Build}");
    }

    public static void OutputErrorsAndWarnings(List<Error> errors, List<Warning> warnings, string? loggingFile, bool noHighlighting)
    {
        IEnumerable<Error> allErrors = errors.Concat(warnings);
        if (string.IsNullOrEmpty(loggingFile))
        {
            foreach(Error err in allErrors)
            {
                OutputError(err, noHighlighting);
            }
            return;
        }
        using StreamWriter writer = new(loggingFile);
        foreach (Error err in allErrors)
        {
            writer.WriteLine(err.ToString());
        }
        writer.Close();
    }

    private static void OutErrorType(bool isError, bool isFatal)
    {
        ConsoleColor console = Console.ForegroundColor;
        if (isError)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (isFatal)
            {
                Console.Error.Write("Fatal ");
            }
            Console.Error.Write("error: ");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Error.Write("warning: ");
        }
        Console.ForegroundColor = console;
    }

    public static void WriteListing(string header, string listingFile, IList<string> listings)
    {
        var assembly = Assembly.GetEntryAssembly()!.GetName();
        string listingContents = $"// {assembly.Name} {assembly.Version}\n" +
                                    $"// Build time (UTC): {DateTime.Now.ToUniversalTime():s}\n" +
                                    header + "\n\n" +
                                    string.Join(System.Environment.NewLine, listings);
        File.WriteAllText(listingFile, listingContents);
    }

    public static void WriteLabelFile(string? labelFile, SymbolManager symbols, bool labelsOnly, bool viceLabels)
    {
        if (string.IsNullOrEmpty(labelFile))
        {
            return;
        }
        if (viceLabels)
        {
            File.WriteAllText(labelFile, symbols.GetViceSymbolListing());
        }
        else
        {
            File.WriteAllText(labelFile, symbols.GetSymbolListing(labelsOnly));
        }
    }

    public static void OutputCode(CommandLineOptions cliOptions, int patchAddress, CodeOutput gen)
    {
        ReadOnlyCollection<byte> objCode;
        if (!string.IsNullOrEmpty(cliOptions.OutputSection))
        {
            objCode = gen.GetCompilation(cliOptions.OutputFile, cliOptions.OutputSection);
        }
        else
        {
            objCode = gen.GetCompilation(cliOptions.OutputFile);
        }
        if (!string.IsNullOrEmpty(cliOptions.Patch))
        {
            byte[] fileBytes = File.ReadAllBytes(cliOptions.OutputFile);
            Array.Copy(objCode.ToArray(), 0, fileBytes, patchAddress, objCode.Count);
            File.WriteAllBytes(cliOptions.OutputFile, fileBytes);
        }
        else
        {
            File.WriteAllBytes(cliOptions.OutputFile, objCode.ToArray());
        }
    }

    public static void OutputError(Error error, bool noHighlighting)
    {
        string sourceName = error.SourceName;
        Token? errorToken = error.Token;
        
        if (string.IsNullOrEmpty(sourceName))
        {
            OutErrorType(error.IsError, error.IsError);
            Console.Error.WriteLine(error.Message);
            return;
        }
        if (errorToken != null)
        {
            if (!string.IsNullOrEmpty(errorToken.MacroName))
            {
                Console.Error.WriteLine($"Expanded from {errorToken.MacroName}:{errorToken.MacroLine}:");
            }
            foreach (var inclusion in errorToken.Inclusions)
            {
                Console.Error.WriteLine($"{inclusion.Item1}:{inclusion.Item2}:");
            }
        }
        Console.Error.Write($"{error.SourceName}({error.Line}:{error.Column}): ");
        OutErrorType(error.IsError, false);
        Console.Error.WriteLine(error.Message);
        if (!noHighlighting && error.Highlight != null)
        {
            Console.Error.WriteLine(error.Highlight[0]);
            if (error.Highlight.Length > 1)
            {
                ConsoleColor consoleColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Error.WriteLine(error.Highlight[1]);
                Console.ForegroundColor = consoleColor;
            }
        }
    }
}

