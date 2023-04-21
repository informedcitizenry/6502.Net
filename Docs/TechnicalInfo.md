# Technical Info About the Core Assembler Functionality

Previous version of 6502.Net were implemented as a unified project, with both the command-line executable and the assembler functionality in one .Net assembly. As of Version 4, the core assembler functionality is now packaged in its own .Net library, called `Sixty502DotNet.Shared`. The rationale is for developers to further extend this project or integrate into their own solutions.

The library is organized in such a way that it is easy not only to build a different front-end from the one provided with this solution, but also to easily add support for other CPUs. The library is organized generally in this way:

- CodeGen - Various code generation functionality such as encoders, code analysis, and format providers
 - Encoders - Responsible for converting source code into architecture-specific machine language
 - FormatProviders - Responsible for generating output according to specific formats
- Errors - All error handling classes and
- Expressions - The core expression engine of the assembler
 - BuiltInFunctions - The various built-in functions and type methods
 - Converters - Responsible for converting text literals into various types, such as numbers and strings
 - Symbols - Symbol functionality
   - Scopes - Scope classes for symbols
 - ValueTypes - Defines all the possible value types, including complex types such as arrays, dictionaries and function objects
- Helpers - Helper classes and extensions
- JSON - JSON deserialization and validation support
- Parser - The parser classes and grammars, support for macros
 - ANTLR - The ANTLR-specific files
   - CSharp - The LexerBase and ParserBase classes
   - Grammar - The grammars used for generating the parsers
- Runtime - The shared assembly services and the interpreter responsible for overall compilation

Developers interested in extending the assembler would presumably be most interested in looking at the Parser/ANTLR folder for adding CPU support, and for creating new directives. Inherit from the `CpuEncoderBase` class to add support for more CPUs and create visit methods for the various parsed instructions. Look at the existing implementations for details.

One other possible use-case is to create a web front-end that calls the assembler library. The developer would probably want to extend the `CustomCharStreamFactory` class in the Parser folder.

```csharp

// Implement the IBinaryFileReader interface and extend the CustomCharStreamFactory class

Interpreter interpreter = new(options, new WSBinaryReader());
AssemblyState state = interpreter.Exec(request, new WSCharStreamFactory(handlerFunc));
if (state.Errors.Count == 0)
{
    // response
}
```