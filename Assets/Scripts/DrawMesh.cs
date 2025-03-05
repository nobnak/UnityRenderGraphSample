using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class DrawMesh : MonoBehaviour {

    [SerializeField] protected Config config = new();
    [SerializeField] protected Dependency dep = new();

    Material material;
    MaterialPropertyBlock properties;

    RenderGraphCallback pass;

    #region unity
    private void OnEnable() {
        material = CoreUtils.CreateEngineMaterial(dep.shader);
        material.enableInstancing = true;

        properties = new MaterialPropertyBlock();

        pass = new RenderGraphCallback() {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
        pass.OnRenderGraph += Pass_OnRenderGraph;
    
        RenderPipelineManager.beginCameraRendering += Draw;
    }

    private void OnDisable() {
        RenderPipelineManager.beginCameraRendering -= Draw;

        if (material != null) {
            CoreUtils.Destroy(material);
            material = null;
        }
    }
    #endregion

    #region drawing
    void Draw(ScriptableRenderContext context, Camera camera) {
        var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null) {
            return;
        }
        var renderer = cameraData.scriptableRenderer;
        if (renderer == null || dep.mesh == null) {
            return;
        }

        renderer.EnqueuePass(pass);
    }
    void Pass_OnRenderGraph(RenderGraph renderGraph, ContextContainer cc) {
        var resourceData = cc.Get<UniversalResourceData>();

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData)) {
            passData.mesh = dep.mesh;
            passData.subMeshIndex = 0;

            passData.material = material;
            passData.shaderPass = 0;

            passData.modelmatrix = transform.localToWorldMatrix;
            passData.properties = properties;

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) => {
                var cmd = ctx.cmd;
                cmd.DrawMesh(
                    data.mesh,
                    data.modelmatrix,
                    data.material,
                    data.subMeshIndex,
                    data.shaderPass,
                    data.properties
                );
            });
        }
    }
    #endregion

    #region declarations
    public static readonly int P_Positions = Shader.PropertyToID("_Positions");
    public static readonly int P_PositionsCount = Shader.PropertyToID("_PositionsCount");

    public class PassData {
        public Mesh mesh;
        public int subMeshIndex;

        public Material material;
        public int shaderPass;

        public float4x4 modelmatrix;
        public MaterialPropertyBlock properties;
    }

    [System.Serializable]
    public class Dependency {
        public Mesh mesh;
        public Shader shader;
    }
    [System.Serializable]
    public class Config {
    }
    #endregion
}