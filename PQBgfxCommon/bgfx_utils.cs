using System;
using System.IO;
using System.Text;
using static Bgfx.bgfx;
using static System.Diagnostics.Debug;

namespace BgfxEx
{

    public static partial class bgfxEx
    {

        public unsafe static byte[][] s_attribTypeSizeD3D9 = new byte[(int)AttribType.Count][]
        {
            new byte[] {  4,  4,  4,  4 }, // Uint8
		    new byte[] {  4,  4,  4,  4 }, // Uint10
		    new byte[] {  4,  4,  8,  8 }, // Int16
		    new byte[] {  4,  4,  8,  8 }, // Half
		    new byte[] {  4,  8, 12, 16 }, // Float
	    };

        public static byte[][] s_attribTypeSizeD3D1x = new byte[(int)AttribType.Count][]
        {
                new byte[] {  1,  2,  4,  4 }, // Uint8
		        new byte[]{  4,  4,  4,  4 }, // Uint10
		        new byte[]{  2,  4,  8,  8 }, // Int16
		        new byte[]{  2,  4,  8,  8 }, // Half
		        new byte[]{  4,  8, 12, 16 }, // Float
        };

        public static byte[][] s_attribTypeSizeGl = new byte[(int)AttribType.Count][]
        {
             new byte[]{  1,  2,  4,  4 }, // Uint8
		     new byte[]{  4,  4,  4,  4 }, // Uint10
		     new byte[]{  2,  4,  6,  8 }, // Int16
		     new byte[]{  2,  4,  6,  8 }, // Half
		     new byte[]{  4,  8, 12, 16 }, // Float
	    };

        public unsafe static byte[][][] s_attribTypeSize = new byte[][][]
        {
            s_attribTypeSizeD3D9,  // Noop
	        s_attribTypeSizeD3D9,  // Direct3D9
	        s_attribTypeSizeD3D1x, // Direct3D11
	        s_attribTypeSizeD3D1x, // Direct3D12
	        s_attribTypeSizeD3D1x, // Gnm
	        s_attribTypeSizeGl,    // Metal
	        s_attribTypeSizeGl,    // Nvn
	        s_attribTypeSizeGl,    // OpenGLES
	        s_attribTypeSizeGl,    // OpenGL
	        s_attribTypeSizeD3D1x, // Vulkan
	        s_attribTypeSizeD3D1x, // WebGPU
	        s_attribTypeSizeD3D9,  // Count
        };

        public unsafe static VertexLayout begin(this VertexLayout layout, RendererType renderer)
        {
            layout.hash = (uint)renderer;
            layout.stride = 0;
            Span<ushort> attributes = new Span<ushort>(layout.attributes, (int)Attrib.Count);
            attributes.Fill(65535);

            Span<ushort> offsets = new Span<ushort>(layout.offset, (int)Attrib.Count);
            offsets.Fill(0);

            return layout;
        }

        public unsafe static VertexLayout add(this VertexLayout layout, Attrib attrib, byte num, AttribType type, bool normalized = false, bool asInt = false)
        {
            ushort encodedNorm = (ushort)(((normalized ? 1 : 0) & 1) << 7);
            ushort encodedType = (ushort)(((ushort)type & 7) << 3);
            ushort encodedNum = (ushort)(((ushort)num - 1) & 3);

            char[] ch = { '\x1', '\x1', '\x1', '\x0', '\x0' };
            fixed (char* c = &ch[(int)type])
            {
                ushort encodeAsInt = (ushort)((asInt & (*(bool*)c) ? 1 : 0) << 8);
                layout.attributes[(int)attrib] = (ushort)(encodedNorm | encodedType | encodedNum | encodeAsInt);
            }
            layout.offset[(int)attrib] = layout.stride;
            layout.stride += s_attribTypeSize[layout.hash][(int)type][num - 1];
            return layout;
        }

        public unsafe static VertexLayout skip(this VertexLayout layout, byte num)
        {
            layout.stride += num;
            return layout;
        }

        public unsafe static VertexLayout end(this VertexLayout layout)
        {
            var hash = new bgfx_hash.HashMurmur2();
            var size = (uint)Attrib.Count;
            hash.exp_ushort(size * 2);
            hash.exp_ushort(size * 2);
            hash.exp_ushort(sizeof(ushort));
            hash.exp_end();

            hash.add(layout.attributes, size);
            hash.add(layout.offset, size);
            hash.add(&layout.stride);
            layout.hash = hash.end();
            return layout;
        }

        public static StringBuilder sb = new StringBuilder();

        public static ProgramHandle load_program(string vsName, string fsName)
        {
            ShaderHandle vsh = load_shader(vsName);
            ShaderHandle fsh;
            fsh.idx = UInt16.MaxValue;
            if (fsName != null && fsName.Length != 0)
            {
                fsh = load_shader(fsName);
            }

            return create_program(vsh, fsh, true /* destroy shaders when program is destroyed */);
        }

        public unsafe static ShaderHandle load_shader(string name)
        {
            sb.Clear();
            var type = get_renderer_type();
            switch (type)
            {
                case RendererType.Noop:
                case RendererType.Direct3D9: sb.Append("shaders/dx9/"); break;
                case RendererType.Direct3D11:
                case RendererType.Direct3D12: sb.Append("shaders/dx11/"); break;
                case RendererType.Gnm: sb.Append("shaders/pssl/"); break;
                case RendererType.Metal: sb.Append("shaders/metal/"); break;
                case RendererType.Nvn: sb.Append("shaders/nvn/"); break;
                case RendererType.OpenGL: sb.Append("shaders/glsl/"); break;
                case RendererType.OpenGLES: sb.Append("shaders/essl/"); break;
                case RendererType.Vulkan: sb.Append("shaders/spirv/"); break;
                case RendererType.WebGPU: sb.Append("shaders/spirv/"); break;

                case RendererType.Count:
                    Assert(false, "You should not be here!");
                    break;
            }

            sb.Append(name);
            sb.Append(".bin");

            using (FileStream reader = new FileStream(sb.ToString(), FileMode.Open))
            {
                ShaderHandle shader = create_shader(load_mem(reader));
                set_shader_name(shader, name, name.Length);
                return shader;
            }
        }

        public unsafe static Memory* load_mem(FileStream reader)
        {
            var size = reader.Length;
            Memory* mem = alloc((uint)size);
            Span<byte> span = new Span<byte>(mem->data, (int)mem->size);
            reader.Read(span);
            mem->data[mem->size - 1] = (byte)'\0';
            return mem;
        }
    }
}
