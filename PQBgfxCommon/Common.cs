using PQEngine.KeyCodes;
using PQEngine.Window;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static Bgfx.bgfx;

namespace PQBgfxCommon
{
    public class CommonWindow : PQWindow
    {
        public ConcurrentQueue<KeyCode> queue;
        public bool exit = false;

        public CommonWindow(DisplayConfig display) : base(display)
        {

        }

        /// <summary>
        /// 当窗体事件执行完毕马上开始下一轮的时候
        /// </summary>
        /// <returns></returns>
        protected override ReturnCode OnUpdateCall()
        {
            if (exit)
            {
                return ReturnCode.Exit;
            }
            render_frame(-1);
            return ReturnCode.POLL;
        }

        /// <summary>
        /// 当有事件发生的时候
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        protected override ReturnCode OnTouchCall(KeyCode keyCode)
        {
            queue.Enqueue(keyCode);
            return ReturnCode.POLL;
        }
    }

    public abstract class App
    {
        protected float width => window.width;
        protected float height => window.height;
        private DateTime _previousGameTime;

        /// <summary>
        /// 上一次和调用这个函数时候的时间差
        /// </summary>
        protected float delta
        {
            get
            {
                return ((float)((DateTime.UtcNow.Ticks - _previousGameTime.Ticks) / 10000000.0));
            }
        }

        public string name;
        public CommonWindow window;
        public ConcurrentQueue<KeyCode> queue;

        protected unsafe abstract bool OnUpdate();

        protected unsafe abstract void Initialize(Init* init);

        public unsafe static void Run<T>(string name) where T : App, new()
        {
            var display = new DisplayConfig().initialize((1280, 720), (1280, 720), (1280, 720),name);
            var window = new CommonWindow(display);
            window.queue = new ConcurrentQueue<KeyCode>();
            render_frame(-1);

            var task = new Task(() => ApiThread<T>(name, window));
            task.Start();
            window.RunLoop();
        }

        private unsafe static void ApiThread<T>(string name, CommonWindow window) where T : App, new()
        {
            var app = new T();
            app.window = window;
            app.name = name;
            app.queue = window.queue;

            PlatformData platform = default;

            Init ini = default;
            init_ctor(&ini);


            ini.vendorId = 0;
            ini.resolution.width = 1280;
            ini.resolution.height = 720;
            platform.nwh = window.nwh;
            ini.platformData = platform;
            ini.type = Bgfx.bgfx.RendererType.Direct3D11;
            ini.resolution.reset = (uint)Bgfx.bgfx.ResetFlags.Vsync;
            app.Initialize(&ini);

            app._previousGameTime = DateTime.UtcNow;

            bool exit = false;
            while (!exit)
            {
                exit = app.OnUpdate();
            }
            window.exit = exit;
        }
    }
}
