//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using CommandLine;
using CommandLine.Text;
using Sixty502DotNet.CLI;
using Sixty502DotNet.Shared;
using System;
using System.Reflection;

var parser = new Parser(with => with.EnableDashDash = true);
CommandLineOptions? cliOptions = null;
var cliResult = parser.ParseArguments<CommandLineOptions>(args);
cliResult.WithParsed(o => cliOptions = o)
    .WithNotParsed(x =>
    {
        var helpText = HelpText.AutoBuild(cliResult, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.AutoHelp = false;     // hides --help
            return HelpText.DefaultParsingErrorsHandler(cliResult, h);
        }, e => e);
        Console.WriteLine(helpText);
    });

/*

static class Program
{
    static void Main(string[] args)
    {
        try
        {
            var assemblyController = new AssemblyController(args);
            assemblyController.Assemble();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }
}
*/