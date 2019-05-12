//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using DotNetAsm;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Asm6502.Net
{
    public class Program
    {
        static string DisplayBannerEventHandler(object sender, bool showVersionOnly)
        {
            var sb = new StringBuilder();

            sb.Append("6502.Net, A Simple .Net 6502 Cross Assembler\n(C) Copyright 2017-2019 informedcitizenry.");
            sb.AppendLine();
            sb.AppendFormat("Version {0}.{1} Build {2}",
                            Assembly.GetEntryAssembly().GetName().Version.Major,
                            Assembly.GetEntryAssembly().GetName().Version.Minor,
                            Assembly.GetEntryAssembly().GetName().Version.Build);
            sb.AppendLine();
            if (!showVersionOnly)
            {
                sb.Append("6502.Net comes with ABSOLUTELY NO WARRANTY; see LICENSE!");
            }
            return sb.ToString();
        }

        static byte[] WriteHeaderEventHandler(object sender)
        {
            var controller = sender as IAssemblyController;

            var arch = Assembler.Options.Architecture.ToLower();
            var progstart = Convert.ToUInt16(Assembler.Output.ProgramStart);
            var progend = Convert.ToUInt16(Assembler.Output.ProgramCounter);
            var progsize = Assembler.Output.GetCompilation().Count;

            using (var ms = new MemoryStream()) {
            using (var writer = new BinaryWriter(ms))
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
                    throw new ArgumentException(error);
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