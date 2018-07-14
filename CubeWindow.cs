using System;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace cubeTest_OpenTK
{
    class CubeWindow : GameWindow
    {
        private static int _WIN_W = 1200;
        private static int _WIN_H = 720;
        private static KeyboardState _keyboardState;

        private int _vaoID;
        private int _vboID_V;
        private int _vboID_T;
        private int _iboID;

        private float[] _vertArys;
        private float[] _texArys;
        private uint[] _idxArys;

        private string _vertSrc =
            @"#version 410 core

            layout (location = 0) in vec3 vert;
            layout (location = 1) in vec2 tex;
            out vec2 passTex;

            uniform mat4 mvpMatrix;

            void main(void)
            {
                gl_Position = mvpMatrix * vec4(vert, 1.0);
                passTex = tex;
            }";

        private string _fragSrc =
            @"#version 410 core

            in vec2 passTex;
            out vec4 outColor;

            uniform sampler2D textureSampler;

            void main(void)
            {
                outColor = texture(textureSampler, passTex);
            }";

        private int _programID;
        private int _textureID;

        private Matrix4 _projectionMatrix;
        private Matrix4 _viewMatrix;
        private Matrix4 _modelviewMatrix;
        int _mvpMatrixLocation;

        private int _xBlocks;
        private int _yBlocks;
        private int _zBlocks;

        public CubeWindow(int xBlocks, int yBlocks, int zBlocks) : base(_WIN_W, _WIN_H, GraphicsMode.Default, "CubeWindow", GameWindowFlags.Default, DisplayDevice.Default, 4, 1, GraphicsContextFlags.Default)
        {
            _xBlocks = xBlocks;
            _yBlocks = yBlocks;
            _zBlocks = zBlocks;
            String v = GL.GetString(OpenTK.Graphics.OpenGL4.StringName.Version);
            Console.WriteLine(v);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            CursorVisible = true;

            // compiling vertex and fragment shader
            _programID = this.CreateProgram(_vertSrc, _fragSrc);
            GL.UseProgram(_programID);

            // load texutre
            _textureID = this.LoadTexture("cube.png", TextureUnit.Texture0);

            GL.GenVertexArrays(1, out _vaoID);
            GL.BindVertexArray(_vaoID);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // enable primitive restart
            GL.Enable(EnableCap.PrimitiveRestart);
            GL.Enable(EnableCap.PrimitiveRestartFixedIndex);
            GL.PrimitiveRestartIndex(UInt32.MaxValue);

            // make data
            int xs = (-_xBlocks + 1) / 2;
            int xm = xs + _xBlocks - 1;
            int ys = (-_yBlocks + 1) / 2;
            int ym = ys + _yBlocks - 1;
            int zs = (-_zBlocks + 1) / 2;
            int zm = zs + _zBlocks - 1;

            float distance = 1.5f;
            this.makeData(xs, ys, zs, xm, ym, zm,
                          distance, distance, distance, 
                          ref _vertArys, ref _texArys, ref _idxArys);

            // vertex array
            GL.GenBuffers(1, out _vboID_V);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID_V);
            int size = _vertArys.Length * System.Runtime.InteropServices.Marshal.SizeOf(default(float));
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(size), _vertArys, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 4 * 3, IntPtr.Zero);
            //GL.BindAttribLocation(_programID, 0, "vert");
            
            // texture coord array
            GL.GenBuffers(1, out _vboID_T);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID_T);
            size = _texArys.Length * System.Runtime.InteropServices.Marshal.SizeOf(default(float));
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(size), _texArys, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * 2, IntPtr.Zero);
            //GL.BindAttribLocation(_programID, 1, "tex");
            
            // indices array
            GL.GenBuffers(1, out _iboID);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _iboID);
            size = _idxArys.Length * System.Runtime.InteropServices.Marshal.SizeOf(default(uint));
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(size), _idxArys, BufferUsageHint.StaticDraw);

            // projectoin
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _WIN_W / (float)_WIN_H, 0.5f, 10000.0f);
            // view
            Vector3 cameraPos = new Vector3(0.0f, 0.0f, 3 * (_xBlocks + _yBlocks + _zBlocks) / 3.0f );
            _viewMatrix = Matrix4.CreateTranslation(-cameraPos);
            // model
            _modelviewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f); // Matrix4.CreateRotationX(-MathHelper.PiOver4) * Matrix4.CreateTranslation(0.0f, 0.0f, 3.0f);

            _mvpMatrixLocation = GL.GetUniformLocation(_programID, "mvpMatrix");
        }
        
        private int CreateProgram(string vertSrc, string fragSrc) {
            int vertexShaderID = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShaderID, vertSrc);
            GL.CompileShader(vertexShaderID);

            int fragmentShaderID = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShaderID, fragSrc);
            GL.CompileShader(fragmentShaderID);

            int programID = GL.CreateProgram();
            GL.AttachShader(programID, vertexShaderID);
            GL.AttachShader(programID, fragmentShaderID);
            GL.LinkProgram(programID);

            GL.DetachShader(programID, vertexShaderID);
            GL.DetachShader(programID, fragmentShaderID);
            GL.DeleteShader(vertexShaderID);
            GL.DeleteShader(fragmentShaderID);

            return programID;
        }

        private int LoadTexture(string fileName, TextureUnit tu) {
            int textureID = GL.GenTexture();

            GL.ActiveTexture(tu);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.Repeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.Repeat);

            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(fileName);
            System.Drawing.Imaging.BitmapData bd 
                = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), 
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bd.Width, bd.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bd.Scan0);
            //GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bitmap.Width, bitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bd.Scan0);

            bitmap.UnlockBits(bd);

            return textureID;
        }

        private void makeData(int xs, int ys, int zs, int xm, int ym, int zm, float xd, float yd, float zd, ref float[] vertArys, ref float[] texArys, ref uint[] idxArys) {

            int num = (xm - xs + 1) * (ym - ys + 1) * (zm - zs + 1);
            
            const int VERT_PER_CUBE = 42;
            const int TEX_PER_CUBE = 28;
            const int IDX_PER_CUBE = 15;

            long memorySize = (VERT_PER_CUBE + TEX_PER_CUBE) * num * System.Runtime.InteropServices.Marshal.SizeOf(default(float))
                            + IDX_PER_CUBE * num * System.Runtime.InteropServices.Marshal.SizeOf(default(uint));
            Console.WriteLine("memory usage = " + String.Format("{0:#,0}", memorySize) + " bytes");

            vertArys = new float[VERT_PER_CUBE * num];
            texArys = new float[TEX_PER_CUBE * num];
            idxArys = new uint[IDX_PER_CUBE * num];

            float xr = 0.5f;
            float yr = 0.5f;
            float zr = 0.5f;

            float texBaseX = 0.0f;
            float texBaseY = 0.0f;
            float texUnitX = 16.0f;
            float texUnitY = 16.0f;
            float texUnitZ = 16.0f;

            const float TEX_SIZE = 128.0f;

            texBaseX /= TEX_SIZE;
            texBaseY /= TEX_SIZE;
            texUnitX /= TEX_SIZE;
            texUnitY /= TEX_SIZE;
            texUnitZ /= TEX_SIZE;

            int count = 0;
            int vi = 0;
            int ti = 0;
            int ii = 0;

            for (float x = xs * xd; x <= xm * xd; x+= xd) {
                for (float y = ys * yd; y <= ym * yd; y+= yd) {
                    for (float z = zs * zd; z <= zm * zd; z += zd) {

                        // 0
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y - yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX * 2 + texUnitY * 2;
                        texArys[ti++] = texBaseY + texUnitY;
                        // 1
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y - yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX * 3 + texUnitY * 2;
                        texArys[ti++] = texBaseY + texUnitY;
                        // 2
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX * 2 + texUnitY * 2;
                        texArys[ti++] = texBaseY;
                        // 3
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX * 3 + texUnitY * 2;
                        texArys[ti++] = texBaseY;
                        // 4
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y - yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX + texUnitX * 2 + texUnitY * 2;
                        texArys[ti++] = texBaseY + texUnitY + texUnitZ;
                        // 5
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX * 2 + texUnitY;
                        texArys[ti++] = texBaseY + texUnitY;
                        // 6
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX + texUnitX * 2 + texUnitY;
                        texArys[ti++] = texBaseY + texUnitY + texUnitZ;
                        // 7
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX + texUnitY;
                        texArys[ti++] = texBaseY + texUnitY;
                        // 8
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX + texUnitX + texUnitY;
                        texArys[ti++] = texBaseY + texUnitY + texUnitZ;
                        // 9
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y - yr;
                        vertArys[vi++] = z + zr;
                        texArys[ti++] = texBaseX + texUnitX;
                        texArys[ti++] = texBaseY + texUnitY;
                        // 10
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y - yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX + texUnitX;
                        texArys[ti++] = texBaseY + texUnitY + texUnitZ;
                        // 11
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y - yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX;
                        texArys[ti++] = texBaseY + texUnitY + texUnitZ;
                        // 12
                        vertArys[vi++] = x + xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX + texUnitX;
                        texArys[ti++] = texBaseY + texUnitY * 2 + texUnitZ;
                        // 13
                        vertArys[vi++] = x - xr;
                        vertArys[vi++] = y + yr;
                        vertArys[vi++] = z - zr;
                        texArys[ti++] = texBaseX;
                        texArys[ti++] = texBaseY + texUnitY * 2 + texUnitZ;

                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 3);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 2);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 1);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 0);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 4);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 5);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 6);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 7);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 8);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 9);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 10);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 11);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 12);
                        idxArys[ii++] = (uint)(count * (IDX_PER_CUBE - 1) + 13);
                        idxArys[ii++] = UInt32.MaxValue;   // primitive restart

                        count++;
                    }
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, ClientRectangle.Width, ClientRectangle.Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            _keyboardState = Keyboard.GetState();
            if (_keyboardState.IsKeyDown(Key.Escape))
            {
                Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _modelviewMatrix = Matrix4.CreateRotationY(0.005f) * Matrix4.CreateRotationX(0.01f) * _modelviewMatrix;
            Matrix4 mvpMatrix = _modelviewMatrix * _viewMatrix * _projectionMatrix;
            GL.UniformMatrix4(_mvpMatrixLocation, false, ref mvpMatrix);

            // prepare for background
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(Color4.Green);

            // draw
            GL.DrawElements(BeginMode.TriangleStrip, _idxArys.Length - 1, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();

            // display fps
            if (_fi.Update(e.Time)) {
                Title = _fi.GetFpsInfo();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.PrimitiveRestart);
            GL.Disable(EnableCap.PrimitiveRestartFixedIndex);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.DeleteVertexArray(_vaoID);
            GL.DeleteBuffer(_vboID_V);
            GL.DeleteBuffer(_vboID_T);
            GL.DeleteBuffer(_iboID);

            GL.DeleteTexture(_textureID);
            GL.DeleteProgram(_programID);
        }
        
        struct FpsInfo {
            private double _timeTotal;
            private double _time1Sec;
            private double _time3Sec;
            private long _frameCntTotal;
            private long _frameCnt1Sec;
            private long _frameCnt3Sec;

            private double _preFpsIn1Sec;
            private double _preFpsIn3Sec;

            public string GetFpsInfo() {
                return $"{this._preFpsIn1Sec:F1}(1s) {this._preFpsIn3Sec:F1}(3s) {this._frameCntTotal / this._timeTotal:F1}(total)";
            }

            public bool Update(double time) {
                bool bUpdate = false;
                this._timeTotal += time;
                this._time1Sec += time;
                this._time3Sec += time;

                this._frameCntTotal++;
                this._frameCnt1Sec++;
                this._frameCnt3Sec++;

                if (1.0f <= this._time1Sec) {
                    this._preFpsIn1Sec = this._frameCnt1Sec / this._time1Sec ;
                    this._time1Sec = 0;
                    this._frameCnt1Sec = 0;
                    bUpdate = true;
                }
                if (3.0f <= this._time3Sec) {
                    this._preFpsIn3Sec = this._frameCnt3Sec / this._time3Sec ;
                    this._time3Sec = 0;
                    this._frameCnt3Sec = 0;
                    bUpdate = true;
                }
                return bUpdate;
            }
        }
        FpsInfo _fi;
    }
}