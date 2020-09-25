using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Core6502DotNet.m65xx
{
    /// <summary>
    /// A class that encapsulates assembly output into a .T64 formatted tape file.
    /// </summary>
    public class T64FormatProvider : Core6502Base , IBinaryFormatProvider
    {
        const string Header = "C64S tape image file\r\n";

        /// <summary>
        /// Creates a new instance of the T64 format provider.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public T64FormatProvider(AssemblyServices services)
            : base(services)
        {
        }

        byte[] GetNameBytes(string file, int size)
        {
            if (file.Length > size)
                file = file.Substring(0, size);
            else if (file.Length < size)
                file = file.PadRight(size);
            return Encoding.ASCII.GetBytes(file);
        }

        public IEnumerable<byte> GetFormat(IEnumerable<byte> objectBytes)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            byte startL = (byte)(Services.Output.ProgramStart & 0xFF);
            byte startH = (byte)(Services.Output.ProgramStart / 256);
            byte endL = (byte)(Services.Output.ProgramEnd & 0xFF);
            byte endH = (byte)(Services.Output.ProgramEnd / 256);

            var start = Services.Output.ConvertToBytes(Services.Output.ProgramStart).ToArray();
            var end = Services.Output.ConvertToBytes(Services.Output.ProgramEnd).ToArray();
            var file = Services.Options.OutputFile.ToUpper();
            if (file.Length > 4 && file.EndsWith(".T64"))
                file = file[0..^4];
            writer.Write(Encoding.ASCII.GetBytes(Header));  // 00-1F
            writer.Write(new byte[32 - Header.Length]); 
            writer.Write((ushort)0x0101);                   // 20-21
            writer.Write((byte)0x1e);                       // 22
            writer.Write((byte)0x00);                       // 23     
            writer.Write((byte)0x01);                       // 24
            writer.Write((byte)0x00);                       // 25
            writer.Write(new byte[2]);                      // 26-27
            writer.Write(GetNameBytes(file, 24));           // 28-3F
            writer.Write((byte)0x01);                       // 40
            writer.Write((byte)0x82);                       // 41
            writer.Write(startL);                           // 42
            writer.Write(startH);                           // 43
            writer.Write(endL);                             // 44
            writer.Write(endH);                             // 45
            writer.Write(new byte[2]);                      // 46-47
            writer.Write(0x0400);                           // 48-4B
            writer.Write(new byte[4]);                      // 4C-4F
            writer.Write(GetNameBytes(file, 16));           // 50-5F
            writer.Write(new byte[0x3A0]);                  // 60-3FF
            writer.Write(objectBytes.ToArray());            // 400-...
            return ms.ToArray();
        }
    }
}
