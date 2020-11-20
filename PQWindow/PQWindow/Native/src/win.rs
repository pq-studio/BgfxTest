//!
//! 渲染器窗口程序
//!
//! 运行在主线程上
//! 以便跨平台兼容性
//!
//! https://docs.rs/winit/0.20.0-alpha5/winit/event_loop/struct.EventLoop.html
//! iOS: Can only be called on the main thread.
//!
use std::{
    ffi::{c_void, CStr},
    ptr::null_mut,
};

use crate::WindowHandle;

use super::{events::Events, DisplayConfig};
use raw_window_handle::HasRawWindowHandle;
use winit::{
    dpi::PhysicalSize,
    event_loop::EventLoop,
    monitor::{MonitorHandle, VideoMode},
    window::{Fullscreen, Window, WindowBuilder},
};

///
/// 窗口
/// iOS: Can only be called on the main thread.
///
pub struct Win {
    ///
    /// gfx 窗口
    ///
    pub win: Window,
}

impl Win {
    pub fn builder() -> WinBuilder {
        WinBuilder {
            events: Events::new(),
            inner: WindowBuilder::new(),
        }
    }
}

pub struct WinBuilder {
    events: Events,
    inner: WindowBuilder,
}

impl WinBuilder {
    ///
    /// 设置窗口标题
    ///
    pub fn title(mut self, ele: String) -> WinBuilder {
        self.inner = self.inner.with_title(&ele);
        self
    }

    ///
    /// 根据配置文件设置窗口属性
    ///
    pub fn config(mut self, config: &DisplayConfig) -> WinBuilder {
        let title = unsafe {
            String::from(
                CStr::from_ptr(config.title)
                    .to_str()
                    .unwrap_or("窗口Title设置失败"),
            )
        };
        let fullscreen = config.fullscreen;
        self.inner = self
            .inner
            .with_visible(if config.visibility == 1 { true } else { false })
            .with_max_inner_size(PhysicalSize::new(
                config.max_dimensions.v1,
                config.max_dimensions.v2,
            ))
            .with_min_inner_size(PhysicalSize::new(
                config.min_dimensions.v1,
                config.min_dimensions.v2,
            ))
            .with_inner_size(PhysicalSize::new(
                config.dimensions.v1,
                config.dimensions.v2,
            ));
        self.fullscreen(fullscreen).title(title)
    }

    ///
    /// 设置全屏模式
    ///
    /// `fullscreen` 0 不全屏 1 全屏无边界 2 全屏有边界
    ///
    pub fn fullscreen(mut self, fullscreen: u8) -> WinBuilder {
        let fullscreen = Some(match fullscreen {
            0 => None::<Fullscreen>,
            1 => Some(Fullscreen::Exclusive(Self::prompt_for_video_mode(
                &Self::prompt_for_monitor(&self.events.events_loop),
            ))),
            2 => Some(Fullscreen::Borderless(Some(Self::prompt_for_monitor(
                &self.events.events_loop,
            )))),
            _ => panic!("未知参数"),
        });

        self.inner = self.inner.with_fullscreen(fullscreen.unwrap());
        self
    }

    fn prompt_for_monitor(event_loop: &EventLoop<()>) -> MonitorHandle {
        let monitor = event_loop
            .available_monitors()
            .nth(0)
            .expect("available_monitors");

        println!("Using {:?}", monitor.name());

        monitor
    }

    fn prompt_for_video_mode(monitor: &MonitorHandle) -> VideoMode {
        let video_mode = monitor.video_modes().nth(0).expect("video_modes");

        println!("Using {}", video_mode);

        video_mode
    }

    pub fn build(self) -> WindowHandle {
        let win = self.inner.build(&self.events.events_loop).unwrap();
        let scalar = win.scale_factor();

        let mut nwh: *mut c_void = null_mut();
        let handle = win.raw_window_handle();
        #[cfg(target_os = "android")]
        {
            match handle {
                raw_window_handle::RawWindowHandle::Android(android) => {
                    nwh = android.a_native_window;
                }
                _ => {}
            }
        }

        #[cfg(target_os = "ios")]
        {
            match handle {
                raw_window_handle::RawWindowHandle::IOS(ios) => {
                    nwh = ios.ui_window;
                }
                _ => {}
            }
        }

        #[cfg(target_os = "macos")]
        {
            match handle {
                raw_window_handle::RawWindowHandle::MacOS(macos) => {
                    nwh = macos.ns_window;
                }
                _ => {}
            }
        }

        #[cfg(target_os = "windows")]
        {
            match handle {
                raw_window_handle::RawWindowHandle::Windows(window) => {
                    nwh = window.hwnd;
                }
                _ => {}
            }
        }

        if nwh.is_null() {
            panic!("get native windows handle error");
        }

        WindowHandle {
            scalar,
            raw: Box::into_raw(Box::new(Win { win })),
            events: Box::into_raw(Box::new(self.events)),
            nwh,
        }
    }
}
