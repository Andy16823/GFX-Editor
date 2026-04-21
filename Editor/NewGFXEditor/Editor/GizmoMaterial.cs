using Assimp;
using LibGFX.Core;
using LibGFX.Graphics;
using LibGFX.Graphics.Materials;
using LibGFX.Graphics.Shader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGFX.Types;

namespace NewGFXEditor.Editor
{
    public class GizmoMaterial : IMaterial
    {
        public string Name { get; set; }
        public Guid ID { get; private set; } = Guid.NewGuid();
        public Vector4 VertexColor { get; set; }
        public bool Hovered { get; set; } = false;
        public bool IsInitialized { get; private set; } = false;

        public bool IsTransparent => false;

        public RenderShader Shader { get; set; }
        public Dictionary<string, MetaValue> Metadata { get; set; }

        public void Deserialize(JObject obj, SerializationContext serializationContext, Func<JObject, bool> callback = null)
        {
            throw new NotImplementedException();
        }

        public void Dispose(IRenderDevice renderDevice)
        {
            this.IsInitialized = false;
        }

        public void FreeCPUResources()
        {
            
        }

        public void Init(IRenderDevice renderDevice)
        {
            this.IsInitialized = true;
        }

        public void Serialize(JsonWriter writer, SerializationContext serializationContext, Action<JsonWriter> callback = null)
        {
            throw new NotImplementedException();
        }

        public void Use(IRenderDevice renderDevice)
        {
            renderDevice.BindShaderProgram(this.Shader);
            if(this.Hovered)
            {
                renderDevice.PrepareShader("vertexColor", new Vector4(1, 1, 0, 1));
            }
            else
            {
                renderDevice.PrepareShader("vertexColor", this.VertexColor);
            }
        }

        public void Disable(IRenderDevice renderDevice)
        {
            renderDevice.UnbindShaderProgram();
        }

        void IMaterial.LoadMaterial(Material asmat, string directory)
        {
            
        }
    }
}
