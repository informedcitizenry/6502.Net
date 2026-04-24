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

namespace Sixty502DotNet.CLI;

public class Command
{
    public SortedDictionary<string, Option> Options { get; } = new()
    {
        {
            "input",
            new Option
            {
                Name = string.Empty,
                HelpText = "The input file(s)",
                Type = OptionType.List
            }
        }
    };
    
    public bool RequiresInput { get; init; }
    
    public Option Input => Options["input"];

    public SortedDictionary<string, Command> SubCommands { get; } = new();

    public void AddOption(Option option)
    {
        if (!string.IsNullOrEmpty(option.Name))
        {
            Options.Add($"--{option.Name}", option);
        }

        if (option.ShortName != '\0')
        {
            Options.Add($"-{option.ShortName}", option);
        }
    }

    public void AddCommand(Command command) => SubCommands.Add(command.Name, command);

    public string Name { get; init; } = string.Empty;
    
    public string Description { get; init; } = string.Empty;

    public string MetaInfo { get; init; } = string.Empty;
    
    public bool HideFromHelp { get; init; }

    public HashSet<string> ShowHelpOnlyFor { get; } = [];
}