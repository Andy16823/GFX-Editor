using LibGFX.Compute;
using LibGFX.Graphics;
using LibGFX.Math;
using Microsoft.VisualBasic.Devices;
using OpenTK.Graphics.ES11;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mesh = LibGFX.Graphics.Mesh;

namespace NewGFXEditor
{
    public class ComputeRaycast
    {
        public static void PerformRaycast(Vector3 rayrayOrigin, Vector3 rayDirection, Transform transform, Mesh mesh, IRenderDevice renderer, ComputeShader shader)
        {
            Vector3[] positions = new Vector3[mesh.Vertices.Count];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = mesh.Vertices[i].Position;

            int positionsBuffer = renderer.CreateBuffer();
            renderer.SetBufferData<Vector3>(positionsBuffer, positions, RenderFlags.GFXBufferTarget.ShaderStorageBuffer, RenderFlags.GFXBufferUsageHint.StaticDraw);

            Matrix4 modelMatrix = mesh.GetTransform() * transform.GetMatrix();
            Matrix4 invModelMatrix = modelMatrix.Inverted();

            int hitBuffer = renderer.CreateBuffer();
            renderer.SetBufferData<int>(hitBuffer, new int[1], RenderFlags.GFXBufferTarget.ShaderStorageBuffer, RenderFlags.GFXBufferUsageHint.DynamicRead);

            renderer.BindShaderProgram(shader);
            renderer.PrepareShader("rayOrigin", rayrayOrigin);
            renderer.PrepareShader("rayDir", rayDirection.Normalized());
            renderer.PrepareShader("invModelMatrix", false, invModelMatrix);

            renderer.BindShaderStorageBuffer(0, positionsBuffer);
            renderer.BindShaderStorageBuffer(1, mesh.RenderData.IndexBuffer);
            renderer.BindShaderStorageBuffer(2, hitBuffer);

            renderer.DispatchCompute(1, 1, 1);
            renderer.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            int hit = renderer.GetBufferData<int>(hitBuffer, 1, RenderFlags.GFXBufferTarget.ShaderStorageBuffer)[0];
            if(hit == 0)
            {
                Debug.WriteLine("No hit detected.");
            }
            else
            {
                Debug.WriteLine("Hit detected");
            }
            renderer.DisposeBuffer(hitBuffer);
            renderer.DisposeBuffer(positionsBuffer);
        }
    }
}
