﻿using System;
using System.Collections.Generic;

namespace CSharpGL
{
    public partial class HighlightRenderer
    {
        /// <summary>
        /// <see cref="OneIndexBuffer"/>实际存在多少个元素。
        /// </summary>
        protected int maxElementCount = 0;

        /// <summary>
        ///
        /// </summary>
        protected override void DoInitialize()
        {
            // init shader program.
            ShaderProgram program = this.shaderCodes.CreateProgram();

            // init property buffer objects.
            VertexAttributeBuffer positionBufferPtr = null;
            IBufferable model = this.Model;
            VertexAttributeBuffer[] vertexAttributeBufferPtrs;
            {
                var list = new List<VertexAttributeBuffer>();
                foreach (AttributeMap.NamePair item in this.attributeMap)
                {
                    VertexAttributeBuffer bufferPtr = model.GetVertexAttributeBufferPtr(
                               item.NameInIBufferable, item.VarNameInShader);
                    if (bufferPtr == null) { throw new Exception(string.Format("[{0}] returns null buffer pointer!", model)); }
                    if (item.NameInIBufferable == positionNameInIBufferable)
                    {
                        positionBufferPtr = new VertexAttributeBuffer(
                            "in_Position",// in_Postion same with in the PickingShader.vert shader
                            bufferPtr.BufferId,
                            bufferPtr.Config,
                            bufferPtr.Length,
                            bufferPtr.ByteLength);
                    }
                    list.Add(bufferPtr);
                }
                vertexAttributeBufferPtrs = list.ToArray();
            }

            // init index buffer
            OneIndexBuffer indexBufferPtr;
            {
                var mode = DrawMode.Points;//any mode is OK as we'll update it later in other place.
                indexBufferPtr = OneIndexBuffer.Create(BufferUsage.StaticDraw, mode, IndexElementType.UInt, positionBufferPtr.ByteLength / (positionBufferPtr.DataSize * positionBufferPtr.DataTypeByteLength));
                this.maxElementCount = indexBufferPtr.ElementCount;
                indexBufferPtr.ElementCount = 0;// 高亮0个图元
                // RULE: Renderer takes uint.MaxValue, ushort.MaxValue or byte.MaxValue as PrimitiveRestartIndex. So take care this rule when designing a model's index buffer.
                GLSwitch glSwitch = new PrimitiveRestartSwitch(indexBufferPtr.Type);
                this.switchList.Add(glSwitch);
            }

            // init VAO.
            var vertexArrayObject = new VertexArrayObject(indexBufferPtr, vertexAttributeBufferPtrs);
            vertexArrayObject.Initialize(program);

            // sets fields.
            this.Program = program;
            this.vertexAttributeBufferPtrs = vertexAttributeBufferPtrs;
            this.indexBufferPtr = indexBufferPtr;
            this.vertexArrayObject = vertexArrayObject;

            this.positionBufferPtr = positionBufferPtr;
        }
    }
}