using UnityEngine;
using UnityEngine.UI;
namespace Mach
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CircuitGridGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;
            for(float x=rect.xMin+20;x<rect.xMax;x+=32)
                for(float y=rect.yMin+16;y<rect.yMax;y+=32)
                {
                    int i=vh.currentVertCount;var c=new Color(.22f,.37f,.44f,.4f);
                    vh.AddVert(new Vector3(x-1,y-1),c,Vector2.zero);vh.AddVert(new Vector3(x-1,y+1),c,Vector2.zero);vh.AddVert(new Vector3(x+1,y+1),c,Vector2.zero);vh.AddVert(new Vector3(x+1,y-1),c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
                }
        }
    }
}
