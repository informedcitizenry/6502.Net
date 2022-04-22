using System.Collections.ObjectModel;
namespace Sixty502DotNet
{
    public interface IOutputFormatProvider
    {
        /// <summary>
        /// Converts the assembly output to a custom binary format.
        /// </summary>
        /// <param name="info">The format info.</param>
        /// <returns>A custom-formatted byte collection.</returns>
        ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info);

        /// <summary>
        /// Gets the format's name.
        /// </summary>
        string FormatName { get; }
    }
}
