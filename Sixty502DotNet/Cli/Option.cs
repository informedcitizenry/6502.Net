namespace Sixty502DotNet.CLI;

public enum OptionType
{
    None,
    String,
    Boolean,
    Integer,
    List
}

public class Option
{
    public string HelpText { get; init; } = string.Empty;

    public OptionType Type { get; init; } = OptionType.None;
    
    public string Name { get; init; } = string.Empty;

    public char ShortName { get; init; } = '\0';

    public bool Deprecated { get; init; }

    public string? ArgName { get; init; }

    public List<Action<ParseResult>> Validators { get; }= [];
}