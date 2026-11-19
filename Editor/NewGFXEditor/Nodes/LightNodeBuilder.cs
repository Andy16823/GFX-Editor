using System;
using System.Collections.Generic;
using System.Text;
using LibGFX.Assets;
using LibGFX.Core;

namespace NewGFXEditor.Nodes
{
    public class LightNodeBuilder : INodeBuilder
    {
        public TreeNode Build(Scene3D scene, AssetManager assetManager)
        {
            var lightsNode = new TreeNode("Lights");
            if (scene.DirectionalLight != null)
            {
                var directionalLightNode = new TreeNode("Directional Light");
                directionalLightNode.Tag = scene.DirectionalLight;
                lightsNode.Nodes.Add(directionalLightNode);
            }

            var pointLightsNode = new TreeNode("Point Lights");
            scene.PointLights.ForEach(pointLight =>
            {
                var pointLightNode = new TreeNode(pointLight.Name);
                pointLightNode.Tag = pointLight;
                pointLightsNode.Nodes.Add(pointLightNode);
            });
            lightsNode.Nodes.Add(pointLightsNode);

            return lightsNode;
        }
    }
}
