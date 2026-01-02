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
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct HitResult
    {
        [FieldOffset(0)]
        public int TriangleIndex;
        [FieldOffset(16)]
        public Vector4 Normal;
        [FieldOffset(32)]
        public Vector4 Position;
    }

    public class ComputeRaycast
    {
        public static HitResult PerformRaycast(Vector3 rayrayOrigin, Vector3 rayDirection, Transform transform, Mesh mesh, IRenderDevice renderer, ComputeShader shader)
        {
            Vector3[] positions = new Vector3[mesh.Vertices.Count];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = new Vector3(mesh.Vertices[i].Position);

            int positionsBuffer = renderer.CreateBuffer();
            renderer.SetBufferData<Vector3>(positionsBuffer, positions, RenderFlags.GFXBufferTarget.ShaderStorageBuffer, RenderFlags.GFXBufferUsageHint.StaticDraw);

            Matrix4 modelMatrix = mesh.GetTransform() * transform.GetMatrix();

            int hitBuffer = renderer.CreateBuffer();
            renderer.SetBufferData<HitResult>(hitBuffer, new HitResult[1], RenderFlags.GFXBufferTarget.ShaderStorageBuffer, RenderFlags.GFXBufferUsageHint.DynamicRead);

            renderer.BindShaderProgram(shader);
            renderer.PrepareShader("rayOrigin", rayrayOrigin);
            renderer.PrepareShader("rayDir", rayDirection.Normalized());
            renderer.PrepareShader("modelMatrix", true, modelMatrix);

            renderer.BindShaderStorageBuffer(0, positionsBuffer);
            renderer.BindShaderStorageBuffer(1, mesh.RenderData.IndexBuffer);
            renderer.BindShaderStorageBuffer(2, hitBuffer);

            renderer.DispatchCompute(1, 1, 1);
            renderer.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            HitResult hit = renderer.GetBufferData<HitResult>(hitBuffer, 1, RenderFlags.GFXBufferTarget.ShaderStorageBuffer)[0];
            if(hit.TriangleIndex == -1)
            {
                Debug.WriteLine("No hit detected.");
            }
            else
            {
                Debug.WriteLine($"Hit detected: Triangle {hit.TriangleIndex}, Normal {hit.Normal}, Position {hit.Position}");
            }
            renderer.DisposeBuffer(hitBuffer);
            renderer.DisposeBuffer(positionsBuffer);

            return hit;
        }
    }
}
