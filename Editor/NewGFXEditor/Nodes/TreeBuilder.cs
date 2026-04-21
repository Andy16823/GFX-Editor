using LibGFX.Assets;
using LibGFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Nodes
{
    public class TreeBuilder
    {
        public List<INodeBuilder> Builders { get; set; }

        public TreeBuilder()
        {
            this.Builders = new List<INodeBuilder>();
        }

        public TreeNode Build(Scene3D scene, AssetManager assetManager)
        {
            var rootNode = new TreeNode("Root");
            Builders.ForEach(builder =>
            {
                var node = builder.Build(scene, assetManager);
                rootNode.Nodes.Add(node);
            });
            return rootNode;
        }

        public void AddBuilder(INodeBuilder builder)
        {
            Builders.Add(builder);
        }

        public void RemoveBuilder(INodeBuilder builder)
        {
            Builders.Remove(builder);
        }
    }
}
