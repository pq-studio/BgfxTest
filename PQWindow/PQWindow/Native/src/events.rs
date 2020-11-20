//!
//! 事件系统包装
//!

use winit::event_loop::EventLoop;

pub struct Events {
    pub events_loop: EventLoop<()>,
}

impl Events {
    pub fn new() -> Self {
        Self {
            events_loop: EventLoop::new(),
        }
    }

    pub fn ele(self) -> EventLoop<()> {
        self.events_loop
    }
}