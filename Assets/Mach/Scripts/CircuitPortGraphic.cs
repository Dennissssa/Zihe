using UnityEngine;
using UnityEngine.UI;
namespace Mach
{
    // Authored on the existing port objects, including inactive editor slots.
    public sealed class CircuitPortGraphic : Image
    {
        public bool socket;
        public int direction; // 0 right, 1 left, 2 up, 3 down
        public override Texture mainTexture=>Texture2D.whiteTexture;
        // Transparent socket recesses are part of the port's hit area too.
        public override bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
            =>RectTransformUtility.RectangleContainsScreenPoint(rectTransform,screenPoint,eventCamera);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(socket)
            {
                Quad(vh,0,0,.48f,1);
                Quad(vh,.48f,0,1,.27f);
                Quad(vh,.48f,.73f,1,1);
            }
            else
            {
                Quad(vh,0,0,.55f,1);
                Quad(vh,.55f,.27f,1,.73f);
            }
        }
        void Quad(VertexHelper vh,float x0,float y0,float x1,float y1)
        {
            int i=vh.currentVertCount;
            vh.AddVert(Point(x0,y0),color,Vector2.zero);vh.AddVert(Point(x0,y1),color,Vector2.zero);
            vh.AddVert(Point(x1,y1),color,Vector2.zero);vh.AddVert(Point(x1,y0),color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        Vector2 Point(float x,float y)
        {
            Vector2 p=direction==1?new Vector2(1-x,1-y):direction==2?new Vector2(1-y,x):direction==3?new Vector2(y,1-x):new Vector2(x,y);
            var r=rectTransform.rect;return r.min+Vector2.Scale(p,r.size);
        }
    }
}
