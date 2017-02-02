using System;
using System.Drawing;
using Utils;

namespace T100k_NAL
{
    public static class Demo
    {
        static bool run = true;

        public static int Main(string[] args)
        {
            T100kConfig cfg = T100kConfig.Default;
            T100kNAL nal = new T100kNAL();
            Console.CancelKeyPress += (sender, e) => run = false;

            try
            {
                nal.Init(cfg);
                nal.Start();

                while (run)
                {
                    var s = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(s))
                        break; // exit on newline or whitespace

                    var x = s.Split(' ');

                    if (x.Length < 4) continue;

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

