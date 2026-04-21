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
    public class ModelNodeBuilder : INodeBuilder
    {
        public TreeNode Build(Scene3D scene, AssetManager assetManager)
        {
            var modelsNode = new TreeNode("Models");
            assetManager.ForeachAsset<IModel>((key, asset) =>
            {
                var assetNode = new TreeNode(key);
                assetNode.Tag = asset;
                modelsNode.Nodes.Add(assetNode);
            });
            return modelsNode;
        }
    }
}
