﻿using System;
using System.IO;

namespace CSharpGL.Demos
{
    internal class PointSpriteRenderer : Renderer
    {
        private Texture spriteTexture;

        public static PointSpriteRenderer Create(int particleCount)
        {
            var shaderCodes = new ShaderCode[2];
            shaderCodes[0] = new ShaderCode(File.ReadAllText(@"shaders\PointSprite.vert"), ShaderType.VertexShader);
            shaderCodes[1] = new ShaderCode(File.ReadAllText(@"shaders\PointSprite.frag"), ShaderType.FragmentShader);
            var map = new AttributeMap();
            map.Add("position", PointSpriteModel.strposition);
            var model = new PointSpriteModel(particleCount);
            var renderer = new PointSpriteRenderer(model, shaderCodes, map, new PointSpriteSwitch());
            renderer.ModelSize = model.Lengths;

            return renderer;
        }

        public PointSpriteRenderer(IBufferable model, ShaderCode[] shaderCodes,
            AttributeMap attributeMap, params GLSwitch[] switches)
            : base(model, shaderCodes, attributeMap, switches)
        {
        }

        protected override void DoInitialize()
        {
            {
                // This is the texture that the compute program will write into
                var bitmap = new System.Drawing.Bitmap(@"Textures\PointSprite.png");
                var texture = new Texture(TextureTarget.Texture2D, bitmap, new SamplerParameters());
                texture.Initialize();
                bitmap.Dispose();
                this.spriteTexture = texture;
            }
            base.DoInitialize();
            this.SetUniform("spriteTexture", this.spriteTexture);
            this.SetUniform("factor", 100.0f);
        }

        protected override void DoRender(RenderEventArgs arg)
        {
            mat4 model = mat4.identity();
            mat4 view = arg.Camera.GetViewMatrix();
            mat4 projection = arg.Camera.GetProjectionMatrix();
            this.SetUniform("mvp", projection * view * model);

            base.DoRender(arg);
        }

        protected override void DisposeUnmanagedResources()
        {
            base.DisposeUnmanagedResources();

            this.spriteTexture.Dispose();
        }

        private class PointSpriteModel : IBufferable
        {
            public PointSpriteModel(int particleCount)
            {
                this.particleCount = particleCount;
            }

            public const string strposition = "position";
            private VertexAttributeBuffer positionBufferPtr = null;
            private IndexBuffer indexBufferPtr;
            private int particleCount;
            private Random random = new Random();
            private float factor = 1;

            public VertexAttributeBuffer GetVertexAttributeBufferPtr(string bufferName, string varNameInShader)
            {
                if (bufferName == strposition)
                {
                    if (this.positionBufferPtr == null)
                    {
                        int length = particleCount;
                        VertexAttributeBuffer bufferPtr = VertexAttributeBuffer.Create(typeof(vec3), length, VertexAttributeConfig.Vec3, BufferUsage.StaticDraw, varNameInShader);
                        unsafe
                        {
                            IntPtr pointer = bufferPtr.MapBuffer(MapBufferAccess.WriteOnly);
                            var array = (vec3*)pointer;
                            for (int i = 0; i < particleCount; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    while (true)
                                    {
                                        var x = (float)(random.NextDouble() * 2 - 1) * factor;
                                        var y = (float)(random.NextDouble() * 2 - 1) * factor;
                                        var z = (float)(random.NextDouble() * 2 - 1) * factor;
                                        if (y < 0 && x * x + y * y + z * z >= factor * factor)
                                        {
                                            array[i] = new vec3(x, y, z);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    double theta = random.NextDouble() * 2 * Math.PI - Math.PI;
                                    double alpha = random.NextDouble() * 2 * Math.PI - Math.PI;
                                    array[i] = new vec3(
                                        (float)(Math.Sin(theta) * Math.Cos(alpha)) * factor,
                                        (float)(Math.Sin(theta) * Math.Sin(alpha)) * factor,
                                        (float)(Math.Cos(theta)) * factor);
                                }
                            }
                            bufferPtr.UnmapBuffer();
                        }

                        this.positionBufferPtr = bufferPtr;
                    }

                    return this.positionBufferPtr;
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            public IndexBuffer GetIndexBufferPtr()
            {
                if (this.indexBufferPtr == null)
                {
                    ZeroIndexBuffer bufferPtr = ZeroIndexBuffer.Create(DrawMode.Points, 0, particleCount);
                    this.indexBufferPtr = bufferPtr;
                }

                return indexBufferPtr;
            }

            /// <summary>
            /// Uses <see cref="ZeroIndexBuffer"/> or <see cref="OneIndexBuffer"/>.
            /// </summary>
            /// <returns></returns>
            public bool UsesZeroIndexBuffer() { return true; }

            public vec3 Lengths { get { return new vec3(2, 2, 2); } }
        }

        internal void UpdateTexture(string filename)
        {
            // This is the texture that the compute program will write into
            var bitmap = new System.Drawing.Bitmap(filename);
            var texture = new Texture(TextureTarget.Texture2D, bitmap, new SamplerParameters());
            texture.Initialize();
            bitmap.Dispose();
            Texture old = this.spriteTexture;
            this.spriteTexture = texture;
            this.SetUniform("sprite_texture", texture);

            old.Dispose();
        }
    }
}