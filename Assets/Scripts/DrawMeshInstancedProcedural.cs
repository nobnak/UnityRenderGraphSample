using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class DrawMeshInstancedProcedural : MonoBehaviour {

    [SerializeField] protected Config config = new();
    [SerializeField] protected Dependency dep = new();

    Material material;
    MaterialPropertyBlock properties;

    RenderGraphCallback pass;
    GraphicsBuffer positionBuffer;

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
        if (positionBuffer != null) {
            positionBuffer.Dispose();
            positionBuffer = null;
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

        if (positionBuffer == null || positionBuffer.count > config.positions.Count) {
            positionBuffer?.Dispose();
            positionBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, 
                math.max(4, config.positions.Count), 
                Marshal.SizeOf<float3>());
        }
        positionBuffer.SetData(config.positions);
        properties.SetBuffer(P_Positions, positionBuffer);

        if (config.positions.Count > 0) {
            renderer.EnqueuePass(pass);
        }
    }
    void Pass_OnRenderGraph(RenderGraph renderGraph, ContextContainer cc) {
        var resourceData = cc.Get<UniversalResourceData>();

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData)) {
            passData.mesh = dep.mesh;
            passData.subMeshIndex = 0;
            passData.material = material;
            passData.shaderPass = 0;
            passData.properties = properties;

            passData.count = config.positions.Count;
            passData.positionBufferH = renderGraph.ImportBuffer(positionBuffer);

            builder.UseBuffer(passData.positionBufferH);

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) => {
                var cmd = ctx.cmd;
                cmd.DrawMeshInstancedProcedural(
                    data.mesh,
                    0,
                    data.material,
                    data.shaderPass,
                    data.count,
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

        public MaterialPropertyBlock properties;

        public int count;
        public BufferHandle positionBufferH;
    }

    [System.Serializable]
    public class Dependency {
        public Mesh mesh;
        public Shader shader;
    }
    [System.Serializable]
    public class Config {
        public List<float3> positions = new();
    }
    #endregion
}