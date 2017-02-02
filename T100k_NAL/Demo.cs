using System;
using System.Drawing;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Utils;

namespace T100k_NAL
{
    public static class Demo
    {
        const string USAGE = "Usage: empty or whitespace to exit\n" +
                             "Usage: ControllerId[0-127] Universe[0-7] Channel[0-1024] Color[HTML Format]";
        static bool run = true;

        /// <summary>
        /// Setup log4net stack using a RollingFileAppender, ConsoleAppender
        /// </summary>
        static void setupLogging(bool verbose)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
            patternLayout.ActivateOptions();

            var console = new ConsoleAppender();
            console.Threshold = verbose ? Level.Debug : Level.Info;
            console.Layout = patternLayout;
            console.ActivateOptions();
            hierarchy.Root.AddAppender(console);

            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;
        }

        public static int Main(string[] args)
        {
            T100kConfig cfg = T100kConfig.Default;
            T100kNAL nal = new T100kNAL();
            Console.CancelKeyPress += (sender, e) => run = false;
            setupLogging(args.Length >= 1 && args[0] == "-v");

            if (!nal.Init(cfg))
                return 1;

            try
            {

                nal.Start();
                Console.WriteLine("T100k Controller © Martin Koppehel 2016");
                Console.WriteLine("use -v to get more output");
                Console.WriteLine(USAGE);

                while (run)
                {
                    var s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s))
                        break; // exit on newline or whitespace

                    var x = s.Split(' ');

                    if (x.Length < 4)
                    {
                        Console.WriteLine(USAGE);
                        continue;
                    }

                    var cid = byte.Parse(x[0]);
                    var uni = byte.Parse(x[1]);
                    var chan = ushort.Parse(x[2]);
                    var c = ColorTranslator.FromHtml(x[3]);
                    nal.UpdateData(new OutputItem
                    {
                        ControllerID = cid,
                        UniverseID = uni,
                        Channel = chan,
                        Color = new RGBColor { R = c.R, B = c.B, G = c.G }
                    }.Enumerate());

                }
            }
            finally
            {
                nal.Stop();
                nal.Destroy();
            }
            return 0;
        }
    }
}

