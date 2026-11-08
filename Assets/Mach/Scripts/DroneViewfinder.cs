using UnityEngine;
using UnityEngine.UI;

namespace Mach
{
    public sealed class DroneViewfinder : MaskableGraphic
    {
        public bool targeting;
        [Range(1,6)] public float strokeWidth = 3.2f;
        int heading, tilt;
        bool hasImpact;
        Vector2 impact;
        public void SetImpact(bool valid,Vector3 world)
        {
            var feed=BeaconDroneStation.FeedCamera;
            Vector3 uv=valid&&feed?feed.WorldToViewportPoint(world):Vector3.zero;
            valid=valid&&uv.z>0&&uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1;
            Rect r=rectTransform.rect;
            float unit=Mathf.Max(.001f,Mathf.Min(r.width/1280f,r.height/720f));
            var at=new Vector2((uv.x-.5f)*r.width,(uv.y-.5f)*r.height)/unit;
            if(valid==hasImpact&&(!valid||(at-impact).sqrMagnitude<.16f))return;
            hasImpact=valid;impact=at;SetVerticesDirty();
        }
        void LateUpdate()
        {
            var feed=BeaconDroneStation.FeedCamera;
            if(!feed)return;
            var angles=feed.transform.eulerAngles;
            int h=Mathf.RoundToInt(angles.y),p=Mathf.RoundToInt(Mathf.DeltaAngle(0,angles.x));
            if(heading==h&&tilt==p)return;
            heading=h;tilt=p;SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float unit = Mathf.Min(rect.width / 1280f, rect.height / 720f);
            void Line(Vector2 a, Vector2 b)
            {
                Vector2 normal = Vector2.Perpendicular(b-a).normalized * (strokeWidth*.5f);
                int first = mesh.currentVertCount;
                mesh.AddVert(center+(a+normal)*unit,color,Vector2.zero);
                mesh.AddVert(center+(b+normal)*unit,color,Vector2.zero);
                mesh.AddVert(center+(b-normal)*unit,color,Vector2.zero);
                mesh.AddVert(center+(a-normal)*unit,color,Vector2.zero);
                mesh.AddTriangle(first,first+1,first+2);
                mesh.AddTriangle(first,first+2,first+3);
            }
            for(int x=-1;x<=1;x+=2)
            for(int y=-1;y<=1;y+=2)
            {
                var corner=new Vector2(x*605,y*324);
                Line(corner,corner+Vector2.left*x*65);
                Line(corner,corner+Vector2.down*y*38);
            }
            if(targeting)
            {
                // Optical axis stays fixed; the separate designation gate follows the predicted hit.
                for(int side=-1;side<=1;side+=2)
                {
                    Line(new Vector2(side*220,0),new Vector2(side*32,0));
                    Line(new Vector2(0,side*145),new Vector2(0,side*32));
                    for(int tick=1;tick<=5;tick++)
                        Line(new Vector2(side*(32+tick*24),-3),new Vector2(side*(32+tick*24),3));
                }
                Line(Vector2.left*4,Vector2.right*4);
                Line(Vector2.down*4,Vector2.up*4);
                if(hasImpact)
                {
                    for(int x=-1;x<=1;x+=2)
                    for(int y=-1;y<=1;y+=2)
                    {
                        var gate=impact+new Vector2(20*x,14*y);
                        Line(gate,gate+Vector2.left*x*10);
                        Line(gate,gate+Vector2.down*y*7);
                    }
                }
                // Azimuth tape and fixed boresight index at the top of the optical field.
                for(int i=-10;i<=10;i++)
                {
                    float x=i*16-(heading%5)*3.2f;
                    Line(new Vector2(x,270),new Vector2(x,270-((heading/5+i)%6==0?15:6)));
                }
                Line(new Vector2(-6,245),new Vector2(0,252));
                Line(new Vector2(0,252),new Vector2(6,245));
            }
            else
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Line(new Vector2(side*85,0),new Vector2(side*14,0));
                    Line(new Vector2(side*14,0),Vector2.down*8);
                    for(int i=-5;i<=5;i++)
                        Line(new Vector2(side*485,i*14),new Vector2(side*(485+(i%5==0?18:7)),i*14));
                }
                for(int i=-14;i<=14;i++)
                {
                    float x=i*12-(heading%5)*2.4f;
                    bool major=(heading/5+i)%6==0;
                    Line(new Vector2(x,295),new Vector2(x,295-(major?16:6)));
                }
                Line(new Vector2(-5,272),new Vector2(0,279));
                Line(new Vector2(5,272),new Vector2(0,279));
                for(int angle=-90;angle<=90;angle+=10)
                {
                    float y=(angle+tilt)*3;
                    if(Mathf.Abs(y)>88)continue;
                    for(int side=-1;side<=1;side+=2)
                        Line(new Vector2(side*30,y),new Vector2(side*(angle==0?69:47),y));
                }
            }
        }
    }
}
