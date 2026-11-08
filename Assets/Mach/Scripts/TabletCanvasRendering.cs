using UnityEngine;
using UnityEngine.Rendering;

namespace Mach
{
    // Explicitly submit world-space UI for the viewmodel camera in the URP stack.
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class TabletCanvasRendering : MonoBehaviour
    {
        Camera view;
        void OnEnable() { view=GetComponent<Camera>(); RenderPipelineManager.beginCameraRendering+=Render; }
        void OnDisable() => RenderPipelineManager.beginCameraRendering-=Render;
        void Render(ScriptableRenderContext context,Camera camera)
        {
            if(camera==view) ScriptableRenderContext.EmitGeometryForCamera(camera);
        }
    }
}
