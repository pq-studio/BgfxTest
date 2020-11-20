using PQEngine.KeyCodes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PQEngine.Window
{
    public enum ReturnCode : uint
    {
        ///
        /// 退出
        ///
        Exit,

        ///
        /// 继续
        ///
        POLL,

        ///
        /// 等待
        ///
        WAIT,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint OnUpdateDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint OnTouchDelegate(uint touchId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnExitDelegate();

    [StructLayout(LayoutKind.Sequential)]
    public struct TupleValue
    {
        public uint v1;
        public uint v2;

        public static implicit operator Tuple<uint, uint>(TupleValue t)
        {
            return Tuple.Create(t.v1, t.v2);
        }

        public static implicit operator (uint, uint)(TupleValue t)
        {
            return (t.v1, t.v2);
        }

        public static implicit operator TupleValue(Tuple<uint, uint> t)
        {
            return new TupleValue { v1 = t.Item1, v2 = t.Item2 };
        }

        public static implicit operator TupleValue((uint, uint) t)
        {
            return new TupleValue { v1 = t.Item1, v2 = t.Item2 };
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeWindowHandle
    {
        public double scalar;
        public IntPtr win;
        public IntPtr events;
        public unsafe void* nwh;
    }

    /// <summary>
    /// 渲染器配置
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfig
    {
        ///
        /// 窗口名称
        ///
        public string title;

        ///
        /// 窗口显示图标
        ///
        public string icon;

        ///
        /// 是否全屏
        /// 
        ///  `fullscreen` 0 不全屏 1 全屏无边界 2 全屏有边界
        ///
        public byte fullscreen;

        ///
        /// 当前窗口尺寸,以像素为单位
        ///
        public TupleValue dimensions;

        ///
        /// 最小窗口尺寸,以像素为单位
        ///
        public TupleValue min_dimensions;

        ///
        /// 最大窗口尺寸,以像素为单位
        ///
        public TupleValue max_dimensions;

        ///
        /// 启用或禁用垂直同步
        ///
        public byte vsync;

        ///
        /// MSAA抗锯齿级别
        ///
        public ushort multisampling;

        ///
        /// 设置窗口的可见性
        ///
        public byte visibility;

        ///
        /// 设置更新频率
        ///
        public float fps;

        public DisplayConfig initialize(TupleValue dimensions = default, TupleValue min_dimensions = default, TupleValue max_dimensions = default, string title = "default_title", string icon = "", byte fullscreen = 0, byte vsync = 0, ushort multisampling = 1, byte visibility = 1, float fps = 60)
        {
            this.title = title;
            this.icon = icon;
            this.fullscreen = fullscreen;
            this.dimensions = dimensions;
            this.min_dimensions = min_dimensions;
            this.max_dimensions = max_dimensions;
            this.vsync = vsync;
            this.multisampling = multisampling;
            this.visibility = visibility;
            this.fps = fps;
            return this;
        }
    }

    internal class WindowHelper
    {
        [DllImport("Libs/pq_window.dll", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable IDE1006 // 命名样式
        internal static extern NativeWindowHandle create_window(DisplayConfig display);

        [DllImport("Libs/pq_window.dll", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable IDE1006 // 命名样式
        internal static extern void free_win(IntPtr ptr);
        [DllImport("Libs/pq_window.dll", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable IDE1006 // 命名样式
        internal static extern void free_display(DisplayConfig ptr);
#pragma warning disable IDE1006 // 命名样式
        [DllImport("Libs/pq_window.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void run_loop(NativeWindowHandle ptr, OnTouchDelegate touch, OnUpdateDelegate update, OnExitDelegate exit);
    }

    public class PQWindow : IDisposable
    {
        private NativeWindowHandle _handle;
        protected OnUpdateDelegate _onUpdate;
        protected OnTouchDelegate _onTouch;
        protected OnExitDelegate _onExit;
        protected DisplayConfig _display;

        public float width => _display.dimensions.v1;
        public float height => _display.dimensions.v2;

        public void bindUpdate(OnUpdateDelegate onUpdate)
        {
            _onUpdate = onUpdate;
        }

        public void bindExit(OnExitDelegate onExit)
        {
            _onExit = onExit;
        }

        public unsafe void* nwh => _handle.nwh;

        public PQWindow(DisplayConfig displayConfig = default)
        {
            _display = displayConfig;
            _handle = WindowHelper.create_window(_display);

            _onExit = OnExit;
            _onTouch = OnTouch;
            _onUpdate = OnUpdate;
        }

        private unsafe uint OnUpdate()
        {
            return (uint)OnUpdateCall();
        }

        private uint OnTouch(uint keyCode)
        {
            return (uint)OnTouchCall((KeyCode)keyCode);
        }

        private void OnExit()
        {
            OnExitCall();
        }

        /// <summary>
        /// 当窗体事件执行完毕马上开始下一轮的时候
        /// </summary>
        /// <returns></returns>
        protected virtual ReturnCode OnUpdateCall()
        {
            return ReturnCode.POLL;
        }

        /// <summary>
        /// 当有事件发生的时候
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        protected virtual ReturnCode OnTouchCall(KeyCode keyCode)
        {
            return ReturnCode.POLL;
        }

        /// <summary>
        /// 当窗体退出的时候
        /// </summary>
        protected virtual void OnExitCall()
        {

        }

        public void RunLoop()
        {
            unsafe
            {
                // 这里run_loop会把winit的窗体和原生窗体句柄都进行销毁
                WindowHelper.run_loop(_handle, _onTouch, _onUpdate, _onExit);
                _handle = default;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // 如果执行了RunLoop 内存会被rust回收
            if (disposing)
            {
                unsafe
                {
                    WindowHelper.free_display(_display);
                }
            }
        }
    }
}
