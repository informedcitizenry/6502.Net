//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet
{
    static class Program
    {
        static void Main(string[] args)
        {
            int loops = 0;
            int a = 9, b = 4, c = 7, d = 1, e = 3;
            while ((a = 1) == 1 && (b = 2) == 3 && (c = 3) == 3 || (d=4) == 4 || (e=5) == 7)
            {
                Console.WriteLine($"{a} {b} {c} {d} {e}");
                if (++loops > 4)
                    break;
            }
            Console.ReadKey();
            try
            {
                var assemblyController = new AssemblyController(args);
                assemblyController.Assemble();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
