using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Silk.NET.OpenGL;

namespace TIDestroyer9000
{
    public class SurfacePlot3D : OpenGlControlBase
    {
        // ── GL objects ────────────────────────────────────────────────────────
        private GL?  _gl;
        private uint _program;
        private uint _vao, _vbo, _ebo;
        private uint _depthRBO;
        private int  _depthW, _depthH;

        private int _uMVP, _uZMin, _uZMax, _uLightDir;
        private int _indexCount;

        // ── Pending mesh ──────────────────────────────────────────────────────
        private float[]? _pendingVerts;
        private uint[]?  _pendingIdx;
        private bool     _meshDirty;

        // ── View parameters ───────────────────────────────────────────────────
        private float _azimuth   = 45f;
        private float _elevation = 30f;
        private float _zMin, _zMax;
        private float _range = 5f;

        private float _zoom = 1.0f;
        private const float ZoomMin  = 0.2f;
        private const float ZoomMax  = 5.0f;
        private const float ZoomStep = 0.12f;

        // ── Shaders ───────────────────────────────────────────────────────────
        //
        // GLSL 1.50 (#version 150) targets desktop OpenGL 3.2 core profile.
        // This is the lowest common denominator that works across:
        //   - Windows: ANGLE translates GL → Direct3D
        //   - Linux:   Mesa native
        //   - macOS:   Apple's deprecated-but-still-present OpenGL 4.1 stack
        //
        // We previously used #version 300 es (GLES 3.0) which works on Windows
        // (via ANGLE's GLES path) and Linux but NOT macOS — Apple never shipped
        // OpenGL ES on the desktop.
        //
        // Differences vs the GLES variant:
        //   - No "precision highp float;" (GLES-only directive)
        //   - in/out semantics are otherwise identical
        //   - User-defined fragment outputs require #version 130+, we have 150
        //
        // Attribute locations are still bound via glBindAttribLocation before
        // linking (see OnOpenGlInit) which works in 1.50. We avoid layout(location=N)
        // because that requires GLSL 330+.

        private const string VertSrc = @"#version 150

in vec3 aPosition;
in vec3 aNormal;

uniform mat4 uMVP;

out vec3  vNormal;
out float vZ;

void main()
{
    vNormal     = normalize(aNormal);
    vZ          = aPosition.z;
    gl_Position = uMVP * vec4(aPosition, 1.0);
}
";

        private const string FragSrc = @"#version 150

in  vec3  vNormal;
in  float vZ;

uniform float uZMin;
uniform float uZMax;
uniform vec3  uLightDir;

out vec4 fragColor;

void main()
{
    vec3  n     = normalize(vNormal);
    vec3  ld    = normalize(uLightDir);
    float diff  = max(dot(n, ld), 0.0)
                + max(dot(-n, ld), 0.0) * 0.35;
    float light = 0.30 + diff * 0.70;

    float t = clamp((vZ - uZMin) / max(uZMax - uZMin, 0.001), 0.0, 1.0);
    vec3 low  = vec3(0.08, 0.39, 0.63);
    vec3 mid  = vec3(0.20, 0.70, 0.80);
    vec3 high = vec3(0.39, 0.86, 0.98);
    vec3 col  = t < 0.5
        ? mix(low,  mid,  t * 2.0)
        : mix(mid,  high, (t - 0.5) * 2.0);

    fragColor = vec4(col * light, 1.0);
}
";

        // ── Public API ────────────────────────────────────────────────────────

        public void SetViewAngles(float azimuth, float elevation)
        {
            _azimuth   = azimuth;
            _elevation = elevation;
            RequestNextFrameRendering();
        }

        public void AdjustZoom(float delta)
        {
            _zoom = Math.Clamp(_zoom - delta * ZoomStep, ZoomMin, ZoomMax);
            RequestNextFrameRendering();
        }

        public void ResetZoom()
        {
            _zoom = 1.0f;
            RequestNextFrameRendering();
        }

        public void UpdateMesh(float[] vertices, uint[] indices,
                               float zMin, float zMax, float range)
        {
            _pendingVerts = vertices;
            _pendingIdx   = indices;
            _zMin   = zMin;
            _zMax   = zMax;
            _range  = range;
            _zoom   = 1.0f;
            _meshDirty = true;
            RequestNextFrameRendering();
        }

        public void ClearMesh()
        {
            _indexCount   = 0;
            _pendingVerts = null;
            _pendingIdx   = null;
            _meshDirty    = false;
            _zoom         = 1.0f;
            RequestNextFrameRendering();
        }

        // ── OpenGL lifecycle ──────────────────────────────────────────────────

        protected override void OnOpenGlInit(GlInterface gl)
        {
            try
            {
                _gl = GL.GetApi(gl.GetProcAddress);

                uint vert = Compile(_gl, GLEnum.VertexShader,   VertSrc);
                uint frag = Compile(_gl, GLEnum.FragmentShader, FragSrc);

                _program = _gl.CreateProgram();
                _gl.AttachShader(_program, vert);
                _gl.AttachShader(_program, frag);
                _gl.BindAttribLocation(_program, 0, "aPosition");
                _gl.BindAttribLocation(_program, 1, "aNormal");
                _gl.LinkProgram(_program);
                _gl.GetProgram(_program, GLEnum.LinkStatus, out int ok);
                if (ok == 0)
                    throw new Exception($"Link error: {_gl.GetProgramInfoLog(_program)}");

                _gl.DeleteShader(vert);
                _gl.DeleteShader(frag);

                _uMVP      = _gl.GetUniformLocation(_program, "uMVP");
                _uZMin     = _gl.GetUniformLocation(_program, "uZMin");
                _uZMax     = _gl.GetUniformLocation(_program, "uZMax");
                _uLightDir = _gl.GetUniformLocation(_program, "uLightDir");

                Console.Error.WriteLine($"[SurfacePlot3D] Init OK — uniforms uMVP:{_uMVP} uZMin:{_uZMin} uZMax:{_uZMax} uLightDir:{_uLightDir}");

                _vao = _gl.GenVertexArray();
                _vbo = _gl.GenBuffer();
                _ebo = _gl.GenBuffer();

                _gl.BindVertexArray(_vao);
                _gl.BindBuffer(GLEnum.ArrayBuffer,        _vbo);
                _gl.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
                SetupLayout(_gl);
                _gl.BindVertexArray(0);

                _depthRBO = _gl.GenRenderbuffer();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SurfacePlot3D] OnOpenGlInit FAILED: {ex}");
            }
        }

        protected override unsafe void OnOpenGlRender(GlInterface glInterface, int fb)
        {
            if (_gl == null) return;
            var gl = _gl;

            try
            {
                gl.BindFramebuffer(GLEnum.Framebuffer, (uint)fb);

                double scale = VisualRoot?.RenderScaling ?? 1.0;
                int w = Math.Max(1, (int)(Bounds.Width  * scale));
                int h = Math.Max(1, (int)(Bounds.Height * scale));

                if (w != _depthW || h != _depthH)
                {
                    _depthW = w; _depthH = h;
                    gl.BindRenderbuffer(GLEnum.Renderbuffer, _depthRBO);
                    gl.RenderbufferStorage(GLEnum.Renderbuffer,
                        GLEnum.DepthComponent16, (uint)w, (uint)h);
                    gl.FramebufferRenderbuffer(GLEnum.Framebuffer,
                        GLEnum.DepthAttachment,
                        GLEnum.Renderbuffer, _depthRBO);
                    Console.Error.WriteLine($"[SurfacePlot3D] Depth RBO {w}×{h}");
                }

                gl.Viewport(0, 0, (uint)w, (uint)h);
                gl.ClearColor(0.118f, 0.118f, 0.172f, 1.0f);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                gl.Enable(GLEnum.DepthTest);

                if (_meshDirty && _pendingVerts != null && _pendingIdx != null)
                {
                    gl.BindVertexArray(_vao);
                    gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
                    gl.BufferData(GLEnum.ArrayBuffer,
                        (ReadOnlySpan<float>)_pendingVerts, GLEnum.DynamicDraw);
                    gl.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
                    gl.BufferData(GLEnum.ElementArrayBuffer,
                        (ReadOnlySpan<uint>)_pendingIdx, GLEnum.DynamicDraw);
                    SetupLayout(gl);
                    gl.BindVertexArray(0);
                    _indexCount = _pendingIdx.Length;
                    _meshDirty  = false;
                    Console.Error.WriteLine($"[SurfacePlot3D] Mesh uploaded — {_indexCount/3} triangles");
                }

                if (_indexCount == 0) return;

                gl.UseProgram(_program);
                float[] mvp = BuildMVP((float)w / h);
                gl.UniformMatrix4(_uMVP, 1, false, (ReadOnlySpan<float>)mvp);
                gl.Uniform1(_uZMin,     _zMin);
                gl.Uniform1(_uZMax,     _zMax);
                gl.Uniform3(_uLightDir, 1.0f, 1.0f, 2.0f);

                gl.BindVertexArray(_vao);
                gl.DrawElements(GLEnum.Triangles,
                    (uint)_indexCount, GLEnum.UnsignedInt, (void*)0);
                gl.BindVertexArray(0);

                var err = gl.GetError();
                if (err != GLEnum.NoError)
                    Console.Error.WriteLine($"[SurfacePlot3D] GL error: {err}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SurfacePlot3D] OnOpenGlRender FAILED: {ex}");
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            if (_gl == null) return;
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
            _gl.DeleteRenderbuffer(_depthRBO);
            _gl.DeleteProgram(_program);
            _gl.Dispose();
            _gl = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static unsafe void SetupLayout(GL gl)
        {
            const uint stride = 6 * sizeof(float);
            gl.VertexAttribPointer(0, 3, GLEnum.Float, false, stride, (void*)0);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(1, 3, GLEnum.Float, false, stride, (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);
        }

        private static uint Compile(GL gl, GLEnum type, string src)
        {
            uint s = gl.CreateShader(type);
            gl.ShaderSource(s, src);
            gl.CompileShader(s);
            gl.GetShader(s, GLEnum.CompileStatus, out int ok);
            if (ok == 0)
                throw new Exception($"Shader ({type}):\n{gl.GetShaderInfoLog(s)}");
            return s;
        }

        // ── MVP — zoom on camera distance ─────────────────────────────────────

        private float[] BuildMVP(float aspect)
        {
            float az   = _azimuth   * MathF.PI / 180f;
            float el   = _elevation * MathF.PI / 180f;
            float dist = _range * 2.8f * _zoom;

            float midZ = (_zMin + _zMax) * 0.5f;
            float ex   = dist * MathF.Cos(el) * MathF.Cos(az);
            float ey   = dist * MathF.Cos(el) * MathF.Sin(az);
            float ez   = midZ + dist * MathF.Sin(el);

            float[] view = LookAt(ex, ey, ez, 0f, 0f, midZ, 0f, 0f, 1f);
            float[] proj = Perspective(45f * MathF.PI / 180f, aspect, 0.1f, 1000f);
            return Multiply(proj, view);
        }

        private static float[] Perspective(float fovY, float aspect, float near, float far)
        {
            float f  = 1f / MathF.Tan(fovY * 0.5f);
            float nf = 1f / (near - far);
            return new float[]
            {
                f/aspect, 0f, 0f,               0f,
                0f,        f, 0f,               0f,
                0f,       0f, (far+near)*nf,   -1f,
                0f,       0f, 2f*far*near*nf,   0f
            };
        }

        private static float[] LookAt(
            float ex, float ey, float ez,
            float cx, float cy, float cz,
            float wx, float wy, float wz)
        {
            float fx=cx-ex, fy=cy-ey, fz=cz-ez;
            float fl=MathF.Sqrt(fx*fx+fy*fy+fz*fz);
            fx/=fl; fy/=fl; fz/=fl;

            float rx=fy*wz-fz*wy, ry=fz*wx-fx*wz, rz=fx*wy-fy*wx;
            float rl=MathF.Sqrt(rx*rx+ry*ry+rz*rz);
            rx/=rl; ry/=rl; rz/=rl;

            float ux=ry*fz-rz*fy, uy=rz*fx-rx*fz, uz=rx*fy-ry*fx;

            return new float[]
            {
                rx, ux, -fx, 0f,
                ry, uy, -fy, 0f,
                rz, uz, -fz, 0f,
                -(rx*ex+ry*ey+rz*ez), -(ux*ex+uy*ey+uz*ez), fx*ex+fy*ey+fz*ez, 1f
            };
        }

        private static float[] Multiply(float[] a, float[] b)
        {
            var c = new float[16];
            for (int i=0; i<4; i++)
                for (int j=0; j<4; j++)
                    for (int k=0; k<4; k++)
                        c[j*4+i] += a[k*4+i] * b[j*4+k];
            return c;
        }
    }
}
