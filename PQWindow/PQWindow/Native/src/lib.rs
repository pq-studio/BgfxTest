//! 这个库是包装winit跨平台窗口管理
//!
//! 用来制作共享库以便类似C#之类的语言可以方便制作跨平台窗口系统
//!
//! 由于IOS的限制只能在主线程创建窗口
//!
//! 该库用共享库的方式暴露给PQEngine使用
//!
//! 编译windows:
//!
//! # Example
//!
//! ``
//! cargo build --release
//! ``
//!
//! 假设开发者的android编译环境已经配好.
//! 交叉编译android:
//!
//! # Example   
//!
//! ``
//! cargo build --target armv7-linux-androideabi --release
//! ``
//!

use events::Events;
use std::convert::From;
use std::{ffi::c_void, ffi::CString, os::raw::c_char};
use win::Win;
use winit::{event::KeyboardInput, event_loop::ControlFlow};
pub mod events;
pub mod win;

///
/// 返回码
///
#[repr(u32)]
pub enum ReturnCode {
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

impl From<u32> for ReturnCode {
    fn from(item: u32) -> Self {
        match item {
            0 => ReturnCode::Exit,
            1 => ReturnCode::POLL,
            2 => ReturnCode::WAIT,
            _ => panic!("传入异常返回码!!!"),
        }
    }
}

impl From<ReturnCode> for ControlFlow {
    fn from(item: ReturnCode) -> Self {
        match item {
            ReturnCode::Exit => ControlFlow::Exit,
            ReturnCode::POLL => ControlFlow::Poll,
            ReturnCode::WAIT => ControlFlow::Wait,
        }
    }
}

///
/// 元组包装
///
/// 方便C#端和rust端的元组进行通讯
///
#[derive(Debug)]
#[repr(C)]
pub struct Tuple {
    v1: u32,
    v2: u32,
}

impl From<(u32, u32)> for Tuple {
    fn from(tup: (u32, u32)) -> Tuple {
        Tuple {
            v1: tup.0,
            v2: tup.1,
        }
    }
}

impl From<Tuple> for (u32, u32) {
    fn from(tup: Tuple) -> (u32, u32) {
        (tup.v1, tup.v2)
    }
}

///
/// 创建窗口的设置
///
#[derive(Debug)]
#[repr(C)]
pub struct DisplayConfig {
    ///
    /// 窗口名称
    ///
    pub title: *const c_char,

    ///
    /// 窗口显示图标
    ///
    pub icon: *const c_char,

    ///
    /// 全屏
    ///
    pub fullscreen: u8,

    ///
    /// 当前窗口尺寸,以像素为单位。
    ///
    pub dimensions: Tuple,

    ///
    /// 最小窗口尺寸,以像素为单位。
    ///
    pub min_dimensions: Tuple,

    ///
    /// 最大窗口尺寸,以像素为单位。
    ///
    pub max_dimensions: Tuple,

    ///
    /// 启用或禁用垂直同步。
    ///
    pub vsync: u8,

    ///
    /// MSAA抗锯齿级别。
    ///
    pub multisampling: u16,

    ///
    /// 设置窗口的可见性
    ///
    pub visibility: u8,

    ///
    /// 设置更新频率
    ///
    pub fps: f32,
}

impl Default for DisplayConfig {
    fn default() -> Self {
        DisplayConfig {
            title: default_title(),
            icon: CString::new("").unwrap().as_ptr(),
            fullscreen: 0,
            dimensions: Tuple { v1: 800, v2: 600 },
            min_dimensions: Tuple { v1: 800, v2: 600 },
            max_dimensions: Tuple { v1: 800, v2: 600 },
            vsync: default_vsync(),
            multisampling: default_multisampling(),
            visibility: default_visibility(),
            fps: default_fps(),
        }
    }
}

///
/// 窗口默认名称
///
fn default_title() -> *const c_char {
    CString::new("default_title").unwrap().as_ptr()
}

///
/// 默认关闭垂直同步
///
fn default_vsync() -> u8 {
    0
}

///
/// mass默认级别
///
fn default_multisampling() -> u16 {
    1
}

///
/// 默认显示窗口
///
fn default_visibility() -> u8 {
    1
}

///
/// 默认显示窗口
///
fn default_fps() -> f32 {
    60_f32
}

///
/// 窗口句柄
///
#[repr(C)]
pub struct WindowHandle {
    ///
    /// DPI
    ///
    pub(crate) scalar: f64,

    ///
    /// 包装的winit的窗口句柄
    ///
    pub(crate) raw: *mut Win,

    ///
    /// 窗口事件循环
    ///
    pub(crate) events: *mut Events,

    ///
    /// 原生窗口句柄
    ///
    pub(crate) nwh: *mut c_void,
}

impl WindowHandle {
    pub unsafe fn free(self) -> (Win, Events, *mut c_void) {
        (
            *Box::from_raw(self.raw),
            *Box::from_raw(self.events),
            self.nwh,
        )
    }
}

///
/// 创建一个窗口句柄
///
#[no_mangle]
pub extern "C" fn create_window(config: DisplayConfig) -> WindowHandle {
    println!("{:#?}", config);

    let wh = Win::builder().config(&config).build();
    std::mem::forget(config);
    return wh;
}

///
/// 回收DisplayConifg
///
#[no_mangle]
pub extern "C" fn free_display(config: *mut DisplayConfig) {
    if config.is_null() {
        return;
    }

    unsafe {
        Box::from_raw(config);
    }
}

///
/// 回收窗口
///
#[no_mangle]
pub extern "C" fn free_win(win: *mut WindowHandle) {
    if win.is_null() {
        return;
    }

    unsafe {
        Box::from_raw(win);
    }
}

pub type KeysFunc = extern "C" fn(u32) -> u32;
pub type UpdateFunc = extern "C" fn() -> u32;
pub type ExitFunc = extern "C" fn();

#[no_mangle]
pub unsafe extern "C" fn run_loop(
    handle: WindowHandle,
    keys: KeysFunc,
    update: UpdateFunc,
    exit: ExitFunc,
) {
    let (_win, event, _nwh) = handle.free();
    event.events_loop.run(move |event, _, control_flow| {
        *control_flow = ControlFlow::Poll;

        use winit::event::Event::*;
        match event {
            NewEvents(_) => {}
            WindowEvent {
                window_id: _,
                event,
            } => match event {
                winit::event::WindowEvent::KeyboardInput {
                    device_id: _,
                    input,
                    is_synthetic: _,
                } => match input {
                    KeyboardInput {
                        virtual_keycode: Some(key),
                        ..
                    } => match key {
                        _ => {
                            *control_flow = ControlFlow::from(ReturnCode::from(keys(key as u32)));
                        }
                    },
                    _ => {}
                },
                _ => {}
            },
            DeviceEvent {
                device_id: _,
                event: _,
            } => {}
            UserEvent(_) => {}
            Suspended => {}
            Resumed => {}
            MainEventsCleared => {
                *control_flow = ControlFlow::from(ReturnCode::from(update()));
            }
            RedrawRequested(_) => {}
            RedrawEventsCleared => {}
            LoopDestroyed => exit(),
        }
    });
}
