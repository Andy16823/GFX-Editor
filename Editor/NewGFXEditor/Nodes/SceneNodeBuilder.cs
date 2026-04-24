using Assimp;
using LibGFX.Assets;
using LibGFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Nodes
{
    public class SceneNodeBuilder : INodeBuilder
    {
        public TreeNode Build(Scene3D scene, AssetManager assetManager)
        {
            var sceneNode = new TreeNode("Scene");
            sceneNode.Tag = scene;

            if(scene.DirectionalLight != null ) 
            {
                var lightNode = new TreeNode("Directional Light");
                lightNode.Tag = scene.DirectionalLight;
                sceneNode.Nodes.Add(lightNode);
            }

            scene.Elements.ForEach(element =>
            {
                var elementNode = new TreeNode(element.Name);
                elementNode.Tag = element;
                sceneNode.Nodes.Add(elementNode);
            });
            return sceneNode;
        }
    }
}
