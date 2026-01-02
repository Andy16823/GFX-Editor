//using LibGFX.Compute;
//using LibGFX.Graphics;
//using LibGFX.Math;
//using LibGFX.Physics;
//using Microsoft.VisualBasic.Devices;
//using OpenTK.Graphics.ES11;
//using OpenTK.Graphics.OpenGL4;
//using OpenTK.Mathematics;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Mesh = LibGFX.Graphics.Mesh;

//namespace NewGFXEditor
//{
//    [StructLayout(LayoutKind.Explicit, Size = 48)]
//    public struct HitResult
//    {
//        [FieldOffset(0)]
//        public int TriangleIndex;
//        [FieldOffset(16)]
//        public Vector4 Normal;
//        [FieldOffset(32)]
//        public Vector4 Position;
//    }

//    public class ComputeRaycast
//    {
//        private int _resultBuffer;
//        private ComputeShader _raycastShader;
//        private IRenderDevice? _renderer;

//        public ComputeRaycast(ComputeShader shader)
//        {
//            _raycastShader = shader;
//        }

//        public void Init(IRenderDevice renderer)
//        {
//            _renderer = renderer;
//            _resultBuffer = renderer.CreateBuffer();
//            _renderer.SetBufferData<HitResult>(_resultBuffer, new HitResult[1], RenderFlags.GFXBufferTarget.ShaderStorageBuffer, RenderFlags.GFXBufferUsageHint.DynamicRead);
//        }

//        public void Dispose(IRenderDevice renderer)
//        {
//            renderer.DisposeBuffer(_resultBuffer);
//        }

//        public HitResult PerformRaycast(MeshRay ray, Transform transform, Mesh mesh)
//        {
//            if (_renderer == null)
//                throw new InvalidOperationException("ComputeRaycast not initialized with a render device.");

//            Matrix4 modelMatrix = mesh.GetTransform() * transform.GetMatrix();

//            _renderer.BindShaderProgram(_raycastShader);
//            _renderer.PrepareShader("rayOrigin", ray.Origin);
//            _renderer.PrepareShader("rayDir", ray.Direction);
//            _renderer.PrepareShader("modelMatrix", true, modelMatrix);

//            _renderer.BindShaderStorageBuffer(0, mesh.RenderData.PositionBuffer);
//            _renderer.BindShaderStorageBuffer(1, mesh.RenderData.IndexBuffer);
//            _renderer.BindShaderStorageBuffer(2, _resultBuffer);

//            _renderer.DispatchCompute(1, 1, 1);
//            _renderer.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

//            HitResult hit = _renderer.GetBufferData<HitResult>(_resultBuffer, 1, RenderFlags.GFXBufferTarget.ShaderStorageBuffer)[0];
//            if (hit.TriangleIndex == -1)
//            {
//                Debug.WriteLine("No hit detected.");
//            }
//            else
//            {
//                Debug.WriteLine($"Hit detected: Triangle {hit.TriangleIndex}, Normal {hit.Normal}, Position {hit.Position}");
//            }
//            return hit;
//        }

//        public static HitResult PerformRaycast(MeshRay ray, Transform transform, Mesh mesh, IRenderDevice renderer, ComputeShader shader)
//        {
//            Matrix4 modelMatrix = mesh.GetTransform() * transform.GetMatrix();

//            int hitBuffer = renderer.CreateBuffer();
//            renderer.SetBufferData<HitResult>(hitBuffer, new HitResult[1], RenderFlags.GFXBufferTarget.ShaderStorageBuffer, RenderFlags.GFXBufferUsageHint.DynamicRead);

//            renderer.BindShaderProgram(shader);
//            renderer.PrepareShader("rayOrigin", ray.Origin);
//            renderer.PrepareShader("rayDir", ray.Direction);
//            renderer.PrepareShader("modelMatrix", true, modelMatrix);

//            renderer.BindShaderStorageBuffer(0, mesh.RenderData.PositionBuffer);
//            renderer.BindShaderStorageBuffer(1, mesh.RenderData.IndexBuffer);
//            renderer.BindShaderStorageBuffer(2, hitBuffer);

//            renderer.DispatchCompute(1, 1, 1);
//            renderer.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

//            HitResult hit = renderer.GetBufferData<HitResult>(hitBuffer, 1, RenderFlags.GFXBufferTarget.ShaderStorageBuffer)[0];
//            if (hit.TriangleIndex == -1)
//            {
//                Debug.WriteLine("No hit detected.");
//            }
//            else
//            {
//                Debug.WriteLine($"Hit detected: Triangle {hit.TriangleIndex}, Normal {hit.Normal}, Position {hit.Position}");
//            }
//            renderer.DisposeBuffer(hitBuffer);

//            return hit;
//        }
//    }
//}
