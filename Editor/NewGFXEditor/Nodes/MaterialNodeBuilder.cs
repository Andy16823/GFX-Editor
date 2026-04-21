using LibGFX.Assets;
using LibGFX.Core;
using LibGFX.Graphics.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Nodes
{
    public class MaterialNodeBuilder : INodeBuilder
    {
        public TreeNode Build(Scene3D scene, AssetManager assetManager)
        {
            var materialsNode = new TreeNode("Materials");
            assetManager.ForeachAsset<IMaterial>((key, asset) =>
            {
                var assetNode = new TreeNode(key);
                assetNode.Tag = asset;
                materialsNode.Nodes.Add(assetNode);
            });
            return materialsNode;
        }
    }
}
