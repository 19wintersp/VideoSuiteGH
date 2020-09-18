using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Reflection;

namespace Stitcher
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);

            Application.EnableVisualStyles();

            //Application.SetCompatibleTextRenderingDefault(false);
            Application.SetCompatibleTextRenderingDefault(true);
            StartApp(args); 
        }

        static void StartApp(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args[0] == "--setup")
                    {
                        //registry setup 'n' stuff
                        return;
                    }
                    if (System.IO.File.Exists(args[0]) && args[0].Substring(args[0].Length - 5) == ".ndls")
                    {
                        Console.WriteLine("Opening " + args[0]);
                        Application.Run(new Needle.Needle(args[0]));
                    }
                    else
                    {
                        Console.WriteLine("File not valid. Please check that it ends with '.ndls' and exists.");
                    }
                }
                else
                {
                    Application.Run(new Needle.Needle()
                    {
                        MaximizeBox = true
                    });
                }
            } catch(Exception e)
            {
                if (MessageBox.Show("Stitcher has crashed. We apologise for the inconvenience, and will try to fix this bug if you can report it to us. The full crash details are as follows:\n\n" + e.ToString() + "\n\nWould you like to relaunch Stitcher?", "Stitcher crash", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes) StartApp(args);
            }
        }
    }
}
