using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class RenderGraphCallback : ScriptableRenderPass {

    public event System.Action<RenderGraph, ContextContainer> OnRenderGraph;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
        OnRenderGraph?.Invoke(renderGraph, frameData);
    }
}
