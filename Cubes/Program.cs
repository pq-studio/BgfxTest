using Bgfx;
using BgfxEx;
using PQBgfxCommon;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using static Bgfx.bgfx;
using static BgfxEx.bgfxEx;

namespace Cubes
{
    class Program : App
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PosColorVertex
        {
            float m_x;
            float m_y;
            float m_z;
            uint m_abgr;

            public PosColorVertex(float m_x, float m_y, float m_z, uint m_abgr)
            {
                this.m_x = m_x;
                this.m_y = m_y;
                this.m_z = m_z;
                this.m_abgr = m_abgr;
            }

            public static void init()
            {
                ms_layout = ms_layout
                 .begin(get_renderer_type())
                 .add(Attrib.Position, 3, AttribType.Float)
                 .add(Attrib.Color0, 4, AttribType.Uint8, true)
                 .end();
            }

            public static VertexLayout ms_layout;
        };

        static PosColorVertex[] s_cubeVertices =
        {
            new PosColorVertex(-1.0f,  1.0f,  1.0f, 0xff000000 ),
            new PosColorVertex( 1.0f,  1.0f,  1.0f, 0xff0000ff ),
            new PosColorVertex(-1.0f, -1.0f,  1.0f, 0xff00ff00 ),
            new PosColorVertex( 1.0f, -1.0f,  1.0f, 0xff00ffff ),
            new PosColorVertex(-1.0f,  1.0f, -1.0f, 0xffff0000 ),
            new PosColorVertex( 1.0f,  1.0f, -1.0f, 0xffff00ff ),
            new PosColorVertex(-1.0f, -1.0f, -1.0f, 0xffffff00 ),
            new PosColorVertex( 1.0f, -1.0f, -1.0f, 0xffffffff ),
        };

        static ushort[] s_cubeTriList =
        {
            0, 1, 2, // 0
	        1, 3, 2,
            4, 6, 5, // 2
	        5, 6, 7,
            0, 2, 4, // 4
	        4, 2, 6,
            1, 5, 3, // 6
	        5, 7, 3,
            0, 4, 1, // 8
	        4, 5, 1,
            2, 3, 6, // 10
	        6, 3, 7,
        };

        static ushort[] s_cubeTriStrip =
        {
            0, 1, 2,
            3,
            7,
            1,
            5,
            0,
            4,
            2,
            6,
            7,
            4,
            5,
        };

        static ushort[] s_cubeLineList =
        {
            0, 1,
            0, 2,
            0, 4,
            1, 3,
            1, 5,
            2, 3,
            2, 6,
            3, 7,
            4, 5,
            4, 6,
            5, 7,
            6, 7,
        };

        static ushort[] s_cubeLineStrip =
        {
            0, 2, 3, 1, 5, 7, 6, 4,
            0, 2, 6, 4, 5, 7, 3, 1,
            0,
        };

        static ushort[] s_cubePoints =
        {
            0, 1, 2, 3, 4, 5, 6, 7
        };

        static unsafe char[][] s_ptNames =
        {
            "Triangle List".ToCharArray(),
            "Triangle Strip".ToCharArray(),
            "Lines".ToCharArray(),
            "Line Strip".ToCharArray(),
            "Points".ToCharArray(),
        };

        static ulong[] s_ptState =
        {
            0,
            (ulong)StateFlags.PtTristrip,
            (ulong)StateFlags.PtLines,
            (ulong)StateFlags.PtLinestrip,
            (ulong)StateFlags.PtPoints,
        };

        [STAThread]
        static void Main(string[] args)
        {
            Run<Program>("Cubes");
        }

        private VertexBufferHandle m_vbh;
        private IndexBufferHandle[] m_ibh;
        private ProgramHandle program;
        private int m_pt = 0;
        private bool m_r = true;
        private bool m_g = true;
        private bool m_b = true;
        private bool m_a = true;

        protected override unsafe void Initialize(bgfx.Init* ini)
        {
            init(ini);

            // Enable debug text.
            set_debug((uint)DebugFlags.Text);

            // Set view 0 clear state.
            set_view_clear(0, (ushort)ClearFlags.Color | (ushort)ClearFlags.Depth, 0x303030ff, 1.0f, 0);

            // Set view 0 default viewport.
            set_view_rect(0, 0, 0, (ushort)width, (ushort)height);

            // Create vertex stream declaration.
            PosColorVertex.init();

            var size = Marshal.SizeOf<PosColorVertex>();

            // Create static vertex buffer.
            fixed (void* data = s_cubeVertices)
            {
                fixed (VertexLayout* layout = &PosColorVertex.ms_layout)
                {
                    var r = make_ref(data, (uint)((uint)size * s_cubeVertices.Length));
                    m_vbh = create_vertex_buffer(r, layout, (ushort)BufferFlags.None);
                }
            }

            m_ibh = new IndexBufferHandle[5];

            // Create static index buffer for triangle list rendering.
            fixed (void* data = s_cubeTriList)
            {
                // Static data can be passed with bgfx::makeRef
                m_ibh[0] = create_index_buffer(make_ref(data, (uint)(sizeof(ushort) * s_cubeTriList.Length)), (ushort)BufferFlags.None);
            }

            // Create static index buffer for line list rendering.
            fixed (void* data = s_cubeTriStrip)
            {
                // Static data can be passed with bgfx::makeRef
                m_ibh[1] = create_index_buffer(make_ref(data, (uint)(sizeof(ushort) * s_cubeTriStrip.Length)), (ushort)BufferFlags.None);
            }

            // Create static index buffer for line strip rendering.
            fixed (void* data = s_cubeLineList)
            {
                // Static data can be passed with bgfx::makeRef
                m_ibh[2] = create_index_buffer(make_ref(data, (uint)(sizeof(ushort) * s_cubeLineList.Length)), (ushort)BufferFlags.None);
            }

            // Create static index buffer for line strip rendering.
            fixed (void* data = s_cubeLineStrip)
            {
                // Static data can be passed with bgfx::makeRef
                m_ibh[3] = create_index_buffer(make_ref(data, (uint)(sizeof(ushort) * s_cubeLineStrip.Length)), (ushort)BufferFlags.None);
            }

            // Create static index buffer for point list rendering.
            fixed (void* data = s_cubePoints)
            {
                // Static data can be passed with bgfx::makeRef
                m_ibh[4] = create_index_buffer(make_ref(data, (uint)(sizeof(ushort) * s_cubePoints.Length)), (ushort)BufferFlags.None);
            }

            // Create program from shaders.
            program = load_program("vs_cubes", "fs_cubes");
        }

        protected unsafe override bool OnUpdate()
        {
            Vector3 at = new Vector3(0, 0, 0);
            Vector3 eye = new Vector3(0, 0, -35);

            {
                Matrix4x4 view = Matrix4x4.CreateLookAt(eye, at, Vector3.UnitY);
                Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView((float)(180f / (Math.PI * 60f)), width / height, 0.1f, 100);
                set_view_transform(0, &view, &proj);

                // Set view 0 default viewport.
                set_view_rect(0, 0, 0, (ushort)width, (ushort)height);
            }

            // This dummy draw call is here to make sure that view 0 is cleared
            // if no other draw calls are submitted to view 0.
            var encoder = encoder_begin(true);
            encoder_touch(encoder, 0);
            encoder_end(encoder);

            IndexBufferHandle ibh = m_ibh[m_pt];
            ulong state = 0
                | (ulong)(m_r ? StateFlags.WriteR : 0)
                | (ulong)(m_g ? StateFlags.WriteG : 0)
                | (ulong)(m_b ? StateFlags.WriteB : 0)
                | (ulong)(m_a ? StateFlags.WriteA : 0)
                | (ulong)StateFlags.WriteZ
                | (ulong)StateFlags.DepthTestLess
                | (ulong)StateFlags.CullCw
                | (ulong)StateFlags.Msaa
                | s_ptState[m_pt]
                ;

            for (uint yy = 0; yy < 11; ++yy)
            {
                for (uint xx = 0; xx < 11; ++xx)
                {
                    Matrix4x4 mtx = Matrix4x4.CreateFromYawPitchRoll(delta + xx * 0.21f, delta + yy * 0.37f, 0);
                    mtx.M41 = 15.0f - (float)xx * 3.0f;
                    mtx.M42 = -15.0f + (float)yy * 3.0f;
                    mtx.M43 = 0;

                    set_transform(&mtx, 1);

                    set_vertex_buffer(0, m_vbh, 0, uint.MaxValue);
                    set_index_buffer(ibh, 0, uint.MaxValue);
                    set_state(state, 0);
                    submit(0, program, 0, (byte)DiscardFlags.All);
                }
            }

            // Advance to next frame. Rendering thread will be kicked to
            // process submitted rendering primitives.
            frame(false);
            return false;
        }
    }
}
