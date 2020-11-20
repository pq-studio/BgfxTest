using PQEngine.Window;
using System;
using System.Threading;

namespace PQBgfxTest
{
    class Program : PQWindow
    {
        protected override ReturnCode OnUpdateCall()
        {

            Bgfx.bgfx.render_frame(-1);
            return ReturnCode.POLL;
        }

        [STAThread]
        unsafe static void Main(string[] args)
        {
            try
            {
                Bgfx.bgfx.PlatformData pd = default;
                using var aw = new Program();
                Bgfx.bgfx.render_frame(-1);

                var t = new Thread(() =>
                {
                    Bgfx.bgfx.Init init = default;
                    Bgfx.bgfx.init_ctor(&init);
                    pd.nwh = aw.nwh;
                    init.type = Bgfx.bgfx.RendererType.Direct3D12;
                    init.vendorId = 0;
                    init.platformData = pd;
                    init.resolution.width = 800;
                    init.resolution.height = 600;
                    init.resolution.reset = (uint)Bgfx.bgfx.ResetFlags.Vsync;

                    Bgfx.bgfx.init(&init);
                    Bgfx.bgfx.reset(800, 600, (uint)Bgfx.bgfx.ResetFlags.Vsync, init.resolution.format);
                    Bgfx.bgfx.set_debug((uint)Bgfx.bgfx.DebugFlags.Stats);
                    Bgfx.bgfx.set_view_clear(0, (ushort)Bgfx.bgfx.ClearFlags.Color | (ushort)Bgfx.bgfx.ClearFlags.Depth, 0x50206000, 1.0f, 0);
                    Bgfx.bgfx.frame(false);

                    for (; ; )
                    {
                        Bgfx.bgfx.set_view_rect(0, 0, 0, 800, 600);
                        var encoder = Bgfx.bgfx.encoder_begin(true);
                        Bgfx.bgfx.encoder_touch(encoder, 0);
                        Bgfx.bgfx.encoder_end(encoder);
                        //Bgfx.bgfx.touch(0);

                        Bgfx.bgfx.dbg_text_clear(0, false);
                        Bgfx.bgfx.dbg_text_printf(0, 1, 0x0f, "sss", "");
                        Bgfx.bgfx.set_view_clear(0, (ushort)Bgfx.bgfx.ClearFlags.Color | (ushort)Bgfx.bgfx.ClearFlags.Depth, 0x50206000, 1.0f, 0);

                        Bgfx.bgfx.frame(false);
                    }
                });
                t.Name = "1";

                t.Start();
                aw.RunLoop();
                Thread.CurrentThread.Name = "2";
                Console.WriteLine(Thread.CurrentThread.Name);
                Console.WriteLine("1ss");
            }
            catch
            {
                Console.WriteLine("12ss");
            }
        }
    }
}