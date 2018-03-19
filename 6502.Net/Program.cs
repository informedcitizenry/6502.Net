//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using DotNetAsm;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Asm6502.Net
{
    class Program
    {
        static string DisplayBannerEventHandler(object sender, bool isVerbose)
        {
            var sb = new StringBuilder();

            if (isVerbose)
            {
                sb.Append("6502.Net, A Simple .Net 6502 Cross Assembler\n(C) Copyright 2017 informedcitizenry.");
                sb.Append(Environment.NewLine);
                sb.Append("6502.Net comes with ABSOLUTELY NO WARRANTY; see LICENSE!");
                sb.Append(Environment.NewLine);
            }
            else
            {
                sb.Append("6502.Net, A Simple .Net 6502 Cross Assembler\n(C) Copyright 2017 informedcitizenry.");
                sb.AppendFormat("Version {0}.{1} Build {2}",
                                Assembly.GetEntryAssembly().GetName().Version.Major,
                                Assembly.GetEntryAssembly().GetName().Version.Minor,
                                Assembly.GetEntryAssembly().GetName().Version.Build);
                sb.Append(Environment.NewLine);
                sb.Append("6502.Net comes with ABSOLUTELY NO WARRANTY; see LICENSE!");
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        static byte[] WriteHeaderEventHandler(object sender)
        {
            var controller = sender as IAssemblyController;

            var arch = controller.Options.Architecture.ToLower();
            var progstart = Convert.ToUInt16(controller.Output.ProgramStart);
            var progend = Convert.ToUInt16(controller.Output.ProgramCounter);
            var progsize = Convert.ToUInt16(controller.Output.GetCompilation().Count);

            using(MemoryStream ms = new MemoryStream()) 
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    if (string.IsNullOrEmpty(arch) || arch.Equals("cbm"))
                    {
                        writer.Write(progstart);
                    }
                    else if (arch.Equals("atari-xex"))
                    {
                        writer.Write(new byte[] { 0xff, 0xff }); // FF FF
                        writer.Write(progstart);
                        writer.Write(progend);
                    }
                    else if (arch.Equals("apple2"))
                    {
                        writer.Write(progstart);
                        writer.Write(progsize);
                    }
                    else if (!arch.Equals("flat"))
                    {
                        var error = string.Format("Unknown architecture specified '{0}'", arch);
                        throw new System.CommandLine.ArgumentSyntaxException(error);
                    }
                    return ms.ToArray();
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                IAssemblyController controller = new AssemblyController(args);

                controller.AddAssembler(new Asm6502(controller));
                controller.DisplayingBanner += DisplayBannerEventHandler;
                controller.WritingHeader += WriteHeaderEventHandler;
                controller.Assemble();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
