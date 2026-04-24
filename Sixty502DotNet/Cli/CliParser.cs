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

using System.Text;

namespace Sixty502DotNet.CLI;

public class CliParser(Command rootCommand)
{
    public ParseResult Parse(string[] argList)
    {
        if (argList.Any(arg => arg.Equals("-h") || arg.Equals("--help") || arg.Equals("-?")))
        {
            return new ParseResult(BuildHelp(), string.Empty, new Dictionary<string, ParsedOption>(), []);
        }
        if (argList.Any(arg => arg.Equals("-V") || arg.Equals("--version")))
        {
            return new ParseResult(BuildVersion(), string.Empty, new Dictionary<string, ParsedOption>(), []);
        }
        var requiresInput = rootCommand.RequiresInput;
        var command = rootCommand;
        var commandName = string.Empty;
        var errors = new List<string>();
        var options = new Dictionary<string, ParsedOption>();
        for (var i = 0; i < argList.Length && errors.Count == 0; i++)
        {
            var arg = argList[i];
            if (arg.StartsWith('-') && argList.Length > i)
            {
                var result = ParseOption(command, argList, arg, ref i);
                errors.AddRange(result.Errors);
                if (result.Errors.Count > 0) continue;
                
                var optionName = result.Options.Values.First().Option.Name;
                var optionValue = result.Options.FirstOrDefault().Value;
                if (options.TryGetValue($"--{optionName}", out var existingOption))
                {
                    if (existingOption.Option.Type != OptionType.List)
                    {
                        errors.Add($"Option `--{optionName}` already specified.");
                        continue;
                    }
                    existingOption.GetListValue().AddRange(optionValue.GetListValue());
                }
                else
                {
                    options.Add($"--{optionName}", optionValue);
                }
            }
            else
            {
                if (i == 0 &&
                    rootCommand.SubCommands.TryGetValue(arg, out var value))
                {
                    command = value;
                    commandName = arg;
                    requiresInput = command.RequiresInput;
                }
                else if (!options.TryGetValue("input", out var inputFile))
                {
                    options["input"] = new ParsedOption(command.Input, [arg]);
                }
                else
                {
                    inputFile.GetListValue().Add(arg);
                }
            }
        }
        if (requiresInput && !options.ContainsKey("input"))
        {
            errors.Add("Missing input file");
        }
        var parsed = new ParseResult(string.Empty, commandName, options, errors);
        foreach (var option in parsed.Options)
        {
            option.Value.Option.Validators.ForEach(v => v(parsed));
        }
        return parsed;
    }

    private static ParseResult ParseOption(Command command, string[] argList, string argName, ref int index)
    {
        var eqIx = argName.IndexOf('=');
        if (eqIx > 0)
        {
            argName = argName[..eqIx];
        }
        var result = new ParseResult(string.Empty, command.Name, new Dictionary<string, ParsedOption>(), []);
        if (!command.Options.TryGetValue(argName, out var option))
        {
            result.Errors.Add($"Unrecognized option `{argName}`.");
            return result;
        }
        var argValue = string.Empty;
        if (option.Type != OptionType.Boolean)
        {
            if (eqIx >= 0)
            {
                if (eqIx < argList[index].Length - 1)
                {
                    argValue = argList[index][(eqIx + 1)..];
                }
            }
            else if (index + 1 < argList.Length && !argList[index + 1].StartsWith('-'))
            {
                argValue = argList[++index];
            }
        }
        else if (option.Type == OptionType.Boolean && eqIx >= 0)
        {
            result.Errors.Add($"Option `{argName}` takes no arguments.");
            return result;
        }
        switch (option.Type)
        {
            case OptionType.Boolean:
                result.Options.Add(argName, new ParsedOption(option, true));
                break;
            case OptionType.Integer:
            {
                if (int.TryParse(argValue, out var parsed) && parsed >= 0)
                {
                    result.Options.Add(argName, new ParsedOption(option, parsed));
                }
                else
                {
                    result.Errors.Add($"Argument for option `{argName}` must be a positive integer.");
                }
                break;
            }
            case OptionType.String:
                if (!string.IsNullOrEmpty(argValue))
                {
                    result.Options.Add(argName, new ParsedOption(option, argValue));
                }
                else
                {
                    result.Errors.Add($"Argument missing for option `{argName}`.");
                }
                break;
            default:
            {
                var argValues = new List<string> { argValue };
                while (argList.Length > index + 1 && !argList[index + 1].StartsWith('-'))
                {
                    argValues.Add(argList[++index]);
                }
                result.Options.Add(argName, new ParsedOption(option, argValues));
                break;
            }
        }
        if (option.Deprecated)
        {
            Console.WriteLine($"Option `{argName}` is deprecated and will be ignored.");
        }
        return result;
    }

    private string BuildHelp()
    {
        var sb = new StringBuilder($"{App.Name}\n{App.Version}\n\n");
        sb.AppendLine("Usage:\n   6502.Net [command] [options] sources\n");
        sb.AppendLine("Options:");
        sb.Append(BuildCommandHelp(rootCommand));
        sb.AppendLine("   -?, -h, --help                     Show help and usage information");
        sb.AppendLine    ("   -V, --version                      Show version information\n"); 
        foreach (var command in rootCommand.SubCommands
                     .Where(command => !command.Value.HideFromHelp))
        {
            sb.Append($"Options for command {command.Value.Name}");
            if (!string.IsNullOrEmpty(command.Value.MetaInfo))
            {
                sb.Append($" {command.Value.MetaInfo}");
            }
            sb.AppendLine(":");
            sb.AppendLine(BuildCommandHelp(command.Value));
        }
        sb.AppendLine("Commands:");
        foreach (var command in rootCommand.SubCommands)
        {
            sb.AppendLine($"   {command.Value.Name,-35}{command.Value.Description}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string BuildCommandHelp(Command command)
    {
        var sb = new StringBuilder();
        var optionsProcessed = new HashSet<string>();
        foreach (var option in command.Options)
        {
            if (option.Value.Type == OptionType.None ||
                string.IsNullOrEmpty(option.Value.Name) ||
                (command.ShowHelpOnlyFor.Count > 0 &&
                !command.ShowHelpOnlyFor.Contains(option.Value.Name)))
            {
                continue;
            }
            var optionName = new StringBuilder();
            switch (string.IsNullOrEmpty(option.Value.Name))
            {
                case false when option.Value.ShortName != '\0':
                    optionName.Append($"-{option.Value.ShortName}, --{option.Value.Name}");
                    break;
                case false:
                    optionName.Append($"    --{option.Value.Name}");
                    break;
                default:
                {
                    if (option.Value.ShortName != '\0')
                    {
                        optionName.Append($"-{option.Value.ShortName}");
                    }
                    break;
                }
            }
            if (!string.IsNullOrEmpty(option.Value.ArgName))
            {
                optionName.Append($"=<{option.Value.ArgName}>");
            }
            var optionFullName = optionName.ToString();
            if (!optionsProcessed.Add(optionFullName)) continue;
            sb.AppendLine($"   {optionFullName,-35}{option.Value.HelpText}");
        }
        return sb.ToString();
    }

    private static string BuildVersion() => $"{App.ShortName} {App.LongVersion}";
}