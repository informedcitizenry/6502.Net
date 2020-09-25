using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    public class HexFormatProvider : Core6502Base, IBinaryFormatProvider
    {
        public HexFormatProvider(AssemblyServices services)
            : base(services)
        {
        }

        public IEnumerable<byte> GetFormat(IEnumerable<byte> objectBytes)
        {
            var hex = BitConverter.ToString(objectBytes.ToArray()).Replace("-", string.Empty);
            return Encoding.ASCII.GetBytes(hex);
        }
    }
}
