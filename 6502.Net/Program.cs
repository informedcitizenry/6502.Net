using DotNetAsm;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Asm6502.Net
{
    class Program
    {
        static void SetBannerTexts(IAssemblyController controller)
        {
            StringBuilder sb = new StringBuilder(), vsb = new StringBuilder();

            sb.Append("6502.Net, A Simple .Net 6502 Cross Assembler\n(C) Copyright 2017 informedcitizenry.");
            sb.Append(Environment.NewLine);
            sb.Append("6502.Net comes with ABSOLUTELY NO WARRANTY; see LICENSE!");
            sb.Append(Environment.NewLine);

            vsb.Append("6502.Net, A Simple .Net 6502 Cross Assembler\n(C) Copyright 2017 informedcitizenry.");
            vsb.AppendFormat("Version {0}.{1} Build {2}",
                                    Assembly.GetEntryAssembly().GetName().Version.Major,
                                    Assembly.GetEntryAssembly().GetName().Version.Minor,
                                    Assembly.GetEntryAssembly().GetName().Version.Build);
            vsb.Append(Environment.NewLine);
            vsb.Append("6502.Net comes with ABSOLUTELY NO WARRANTY; see LICENSE!");
            vsb.Append(Environment.NewLine);

            controller.BannerText = sb.ToString();
            controller.VerboseBannerText = vsb.ToString();
        }

        private static void targetHeader(IAssemblyController controller, BinaryWriter writer)
        {
            string arch = controller.Options.Architecture.ToLower();
            ushort progstart = Convert.ToUInt16(controller.Output.ProgramStart);
            ushort progend = Convert.ToUInt16(controller.Output.ProgramCounter);
            ushort size = Convert.ToUInt16(controller.Output.GetCompilation().Count);
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
                writer.Write(size);
            }
            else if (arch.Equals("flat"))
            {
                // do nothing
            }
            else
            {
                string error = string.Format("Unknown architecture specified '{0}'", arch);
                throw new System.CommandLine.ArgumentSyntaxException(error);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                IAssemblyController controller = new AssemblyController(args);
                
                controller.AddAssembler(new Asm6502(controller));
                controller.AddAssembler(new Pseudo6502(controller));
                controller.HeaderOutputAction = targetHeader;
                SetBannerTexts(controller);

                controller.Assemble();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
