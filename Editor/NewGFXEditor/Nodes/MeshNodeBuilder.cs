using LibGFX.Assets;
using LibGFX.Core;
using LibGFX.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Nodes
{
    public class MeshNodeBuilder : INodeBuilder
    {
        public TreeNode Build(Scene3D scene, AssetManager assetManager)
        {
            var meshesNode = new TreeNode("Meshes");
            assetManager.ForeachAsset<Mesh>((key, asset) =>
            {
                var assetNode = new TreeNode(key);
                assetNode.Tag = asset;
                meshesNode.Nodes.Add(assetNode);
            });
            return meshesNode;
        }
    }
}
