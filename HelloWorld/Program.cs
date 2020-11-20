using Bgfx;
using PQBgfxCommon;
using System;
using static Bgfx.bgfx;

namespace HelloWorld
{
    class Program : App
    {

        [STAThread]
        static void Main(string[] args)
        {
            Run<Program>("HelloWorld");
        }

        protected override unsafe void Initialize(bgfx.Init* ini)
        {
            init(ini);

            // Enable debug text.
            set_debug((uint)DebugFlags.Text);

            // Set view 0 clear state.
            set_view_clear(0, (ushort)ClearFlags.Color | (ushort)ClearFlags.Depth, 0x303030ff, 1.0f, 0);
        }

        protected unsafe override bool OnUpdate()
        {
            // Set view 0 default viewport.
            set_view_rect(0, 0, 0, (ushort)width, (ushort)height);

            // This dummy draw call is here to make sure that view 0 is cleared
            // if no other draw calls are submitted to view 0.
            var encoder = encoder_begin(true);
            encoder_touch(encoder, 0);
            encoder_end(encoder);

            // Use debug font to print information about this example.
            fixed (byte* ptr = Logo.s_logo)
            {
                dbg_text_clear(0, true);
                dbg_text_image(
                      (ushort)(Math.Max((width / 2 / 8), 20) - 20)
                    , (ushort)(Math.Max((height / 2 / 16), 6) - 6)
                    , 40
                    , 12
                    , ptr
                    , 160
                    );
            }

            dbg_text_printf(0, 1, 0x0f, "Color can be changed with ANSI \x1b[9;me\x1b[10;ms\x1b[11;mc\x1b[12;ma\x1b[13;mp\x1b[14;me\x1b[0m code too.", "");
            dbg_text_printf(80, 1, 0x0f, "\x1b[;0m    \x1b[;1m    \x1b[; 2m    \x1b[; 3m    \x1b[; 4m    \x1b[; 5m    \x1b[; 6m    \x1b[; 7m    \x1b[0m", "");
            dbg_text_printf(80, 2, 0x0f, "\x1b[;8m    \x1b[;9m    \x1b[;10m    \x1b[;11m    \x1b[;12m    \x1b[;13m    \x1b[;14m    \x1b[;15m    \x1b[0m", "");

            Stats* stats = get_stats();
            dbg_text_printf(0, 2, 0x0f, $"Backbuffer {stats->width}W x {stats->height}H in pixels, debug text { stats->textWidth}W x {stats->textHeight}H in characters.", "");

            // Advance to next frame. Rendering thread will be kicked to
            // process submitted rendering primitives.
            frame(false);
            return false;
        }
    }
}
