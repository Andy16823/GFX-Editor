using LibGFX.Assets;
using LibGFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Nodes
{
    public interface INodeBuilder
    {
        public TreeNode Build(Scene3D scene, AssetManager assetManager);
    }
}
