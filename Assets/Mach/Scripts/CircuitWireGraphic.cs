using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Mach
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CircuitWireGraphic : MaskableGraphic
    {
        public CircuitBoardView board;
        readonly List<List<Vector2>> paths=new List<List<Vector2>>();
        readonly List<Color> colors=new List<Color>();
        int signature=int.MinValue;
        struct Link { public int from,to;public bool data; }
        readonly List<Link> links=new List<Link>();
        readonly Dictionary<string,List<Vector2>> cached=new Dictionary<string,List<Vector2>>();
        List<Vector2> deletedPath;
        float flashUntil;
        int hovered=-1;
        public int UnroutableCount{get;private set;}
        public void RefreshRoutes()
        {
            if(!board||!board.program)return;
            int hash=17;unchecked{foreach(var n in board.program.nodes){if(n==null)continue;hash=hash*31+n.active.GetHashCode();if(!n.active)continue;hash=hash*31+n.position.GetHashCode();hash=hash*31+n.next;hash=hash*31+n.pointInput;hash=hash*31+n.ammoInput;hash=hash*31+n.rateInput;hash=hash*31+n.durationInput;}}
            if(hash==signature)return;signature=hash;paths.Clear();colors.Clear();links.Clear();UnroutableCount=0;
            var obstacles=new List<Rect>();for(int i=0;i<board.slots.Length;i++)if(board.program.Valid(i))
            {var size=board.slots[i].rect.rect.size+new Vector2(40,48);obstacles.Add(new Rect(board.program.nodes[i].position-size*.5f,size));}
            var nodes=board.program.nodes;
            for(int i=0;i<nodes.Length;i++)if(board.program.Valid(i))
            {
                var n=nodes[i];if(board.program.Valid(n.next))Connect(i,n.next,false,0,obstacles);
                int lane=0;foreach(int source in new[]{n.pointInput,n.ammoInput,n.rateInput,n.durationInput})if(board.program.Valid(source))Connect(source,i,true,lane++,obstacles);
            }
            SetVerticesDirty();
        }
        Vector2 Port(RectTransform port,Vector2 direction)
        {
            var r=port.rect;var shape=port.GetComponent<CircuitPortGraphic>();
            // The socket recess ends at 48% of its local depth, not its outer edge.
            float depth=shape&&shape.socket?-.02f:.5f;
            var p=r.center+Vector2.Scale(r.size*depth,direction);
            return rectTransform.InverseTransformPoint(port.TransformPoint(p));
        }
        readonly List<Vector2> preview=new List<Vector2>();
        void LateUpdate()
        {
            int lastHovered=hovered;bool hadPreview=preview.Count>0;
            hovered=Mouse.current!=null?HitWire(Mouse.current.position.ReadValue()):-1;
            preview.Clear();
            if(board && board.PendingWire && Mouse.current!=null)
            {
                Vector2 direction=board.PendingDirection;
                var a=Port(board.PendingPort,direction);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,Mouse.current.position.ReadValue(),board.canvas.worldCamera,out var b);
                var stub=a+direction*18;
                preview.Add(a);preview.Add(stub);
                preview.Add(direction.x!=0?new Vector2(stub.x,b.y):new Vector2(b.x,stub.y));preview.Add(b);
            }
            bool flash=deletedPath!=null;
            if(flash&&Time.unscaledTime>=flashUntil)deletedPath=null;
            if(lastHovered!=hovered||hadPreview||preview.Count>0||flash)SetVerticesDirty();
        }
        void Connect(int from,int to,bool data,int lane,List<Rect> obstacles)
        {
            var a=Port(data?board.slots[from].data:board.slots[from].output,data?Vector2.down:Vector2.right);var b=Port(data?board.slots[to].parameter:board.slots[to].input,data?Vector2.up:Vector2.left);
            string key=from+":"+to+":"+data;
            if(cached.TryGetValue(key,out var old))
            {
                // Unrelated blocks must not change this route, including on mouse release.
                var stable=old.Count>=2&&(old[0]-a).sqrMagnitude<.001f&&(old[old.Count-1]-b).sqrMagnitude<.001f
                    ?old:FollowEndpoints(old,a,b);
                if(ClearOfBlocks(stable))
                {
                    cached[key]=stable;links.Add(new Link{from=from,to=to,data=data});paths.Add(stable);
                    colors.Add(data?CircuitBoardView.ParameterPortColor:CircuitBoardView.ExecutionPortColor);return;
                }
            }

            var da=data?Vector2.down:Vector2.right;var db=data?Vector2.up:Vector2.left;
            var start=ChooseStub(a,da,data?34:18,obstacles);var end=ChooseStub(b,db,(data?18:38)+lane*10,obstacles);
            var route=CircuitWireRouter.Route(start,end,obstacles,paths);
            if(route==null)
            {
                var edges=new List<Rect>();
                for(int i=0;i<board.slots.Length;i++)if(board.program.Valid(i))
                {var size=board.slots[i].rect.sizeDelta+new Vector2(34,46);edges.Add(new Rect(board.program.nodes[i].position-size*.5f,size));}
                start=ChooseStub(a,da,24,edges);end=ChooseStub(b,db,24,edges);
                route=CircuitWireRouter.EdgeRoute(start,end,edges);
            }
            if(route==null)route=new List<Vector2>{start,new Vector2(start.x,end.y),end};
            route.Insert(0,data?new Vector2(a.x,start.y):new Vector2(start.x,a.y));route.Insert(0,a);
            route.Add(data?new Vector2(b.x,end.y):new Vector2(end.x,b.y));route.Add(b);
            for(int i=route.Count-1;i>0;i--)if((route[i]-route[i-1]).sqrMagnitude<.01f)route.RemoveAt(i);
            for(int i=route.Count-2;i>0;i--)
            {var before=route[i]-route[i-1];var after=route[i+1]-route[i];if(Mathf.Abs(Cross(before,after))<.001f&&Vector2.Dot(before,after)>0)route.RemoveAt(i);}
            cached[key]=new List<Vector2>(route);
            links.Add(new Link{from=from,to=to,data=data});
            paths.Add(route);colors.Add(data?CircuitBoardView.ParameterPortColor:CircuitBoardView.ExecutionPortColor);
        }
        static List<Vector2> FollowEndpoints(List<Vector2> old,Vector2 a,Vector2 b)
        {
            var result=new List<Vector2>(old);if(result.Count<3)return null;
            bool firstHorizontal=Mathf.Abs(old[0].y-old[1].y)<.01f;
            bool lastHorizontal=Mathf.Abs(old[old.Count-1].y-old[old.Count-2].y)<.01f;
            result[0]=a;result[result.Count-1]=b;
            result[1]=firstHorizontal?new Vector2(result[1].x,a.y):new Vector2(a.x,result[1].y);
            int n=result.Count-2;
            result[n]=lastHorizontal?new Vector2(result[n].x,b.y):new Vector2(b.x,result[n].y);
            return result;
        }
        bool ClearOfBlocks(List<Vector2> route)
        {
            if(route==null)return false;
            for(int j=1;j<route.Count;j++)
            {
                var a=route[j-1];var b=route[j];
                if(Mathf.Abs(a.x-b.x)>.01f&&Mathf.Abs(a.y-b.y)>.01f)return false;
                for(int i=0;i<board.slots.Length;i++)if(board.program.Valid(i))
                {
                    var size=board.slots[i].rect.rect.size+Vector2.one*4;
                    var r=new Rect(board.program.nodes[i].position-size*.5f,size);
                    if(j==1&&r.Contains(a)||j==route.Count-1&&r.Contains(b))continue;
                    bool hit=Mathf.Abs(a.y-b.y)<.01f
                        ?a.y>r.yMin&&a.y<r.yMax&&Mathf.Max(a.x,b.x)>r.xMin&&Mathf.Min(a.x,b.x)<r.xMax
                        :a.x>r.xMin&&a.x<r.xMax&&Mathf.Max(a.y,b.y)>r.yMin&&Mathf.Min(a.y,b.y)<r.yMax;
                    if(hit)return false;
                }
            }
            return true;
        }
        int HitWire(Vector2 screen)
        {
            if(!board||!board.program.Editing||board.PendingWire||board.DraggingBlock)return -1;
            var cam=board.canvas.worldCamera;
            if(!RectTransformUtility.RectangleContainsScreenPoint(board.surface,screen,cam))return -1;
            foreach(var slot in board.slots)if(slot.rect.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(slot.rect,screen,cam))return -1;
            float best=9*9;int found=-1;
            for(int i=0;i<paths.Count;i++)for(int j=1;j<paths[i].Count;j++)
            {
                Vector2 a=RectTransformUtility.WorldToScreenPoint(cam,rectTransform.TransformPoint(paths[i][j-1]));
                Vector2 b=RectTransformUtility.WorldToScreenPoint(cam,rectTransform.TransformPoint(paths[i][j]));
                var d=b-a;float t=d.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(screen-a,d)/d.sqrMagnitude);
                float distance=(screen-a-d*t).sqrMagnitude;
                if(distance<best){best=distance;found=i;}
            }
            return found;
        }
        public void DeleteAt(Vector2 screen)
        {
            int i=HitWire(screen);if(i<0||i>=links.Count)return;
            deletedPath=new List<Vector2>(paths[i]);flashUntil=Time.unscaledTime+.18f;
            var link=links[i];board.program.DisconnectWire(link.from,link.to,link.data);
            hovered=-1;signature=int.MinValue;RefreshRoutes();
        }
        static void Stroke(VertexHelper vh,List<Vector2> path,Color c,float radius)
        {
            for(int j=1;j<path.Count;j++)if((path[j]-path[j-1]).sqrMagnitude>.01f)Line(vh,path[j-1],path[j],c,radius);
            for(int j=1;j<path.Count-1;j++)Join(vh,path[j],c,radius);
        }
        static Vector2 ChooseStub(Vector2 p,Vector2 dir,float preferred,List<Rect> obstacles)
        {
            foreach(float length in new[]{preferred,12f,6f})
            {
                var q=p+dir*length;bool free=true;foreach(var r in obstacles)if(r.Contains(q)){free=false;break;}if(free)return q;
            }
            return p+dir*preferred;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();for(int p=0;p<paths.Count;p++)
            {
                var route=paths[p];if(p==hovered)Stroke(vh,route,Color.white,3.5f);for(int j=1;j<route.Count;j++)
                {
                    var a=route[j-1];var b=route[j];var dir=(b-a).normalized;float length=Vector2.Distance(a,b);if(length<.01f)continue;
                    var gaps=new List<float>();
                    for(int k=0;k<p;k++)for(int s=1;s<paths[k].Count;s++)
                    {
                        Vector2 c=paths[k][s-1],d=paths[k][s];float cross=Cross(b-a,d-c);if(Mathf.Abs(cross)<.01f)continue;
                        float t=Cross(c-a,d-c)/cross,u=Cross(c-a,b-a)/cross;if(t>.02f&&t<.98f&&u>.02f&&u<.98f)gaps.Add(t*length);
                    }
                    gaps.Sort();float cursor=0;foreach(float gap in gaps){if(gap-5>cursor)Line(vh,a+dir*cursor,a+dir*(gap-5),colors[p]);cursor=Mathf.Max(cursor,gap+5);}if(cursor<length)Line(vh,a+dir*cursor,b,colors[p]);
                }
                // Round joins use exactly the stroke radius, avoiding square
                // overlaps and holes at folds while keeping constant thickness.
                for(int j=1;j<route.Count-1;j++)Join(vh,route[j],colors[p]);
            }
            if(deletedPath!=null&&Time.unscaledTime<flashUntil)Stroke(vh,deletedPath,new Color(1,.15f,.12f),3.5f);
            Color previewColor=board&&board.PendingData?CircuitBoardView.ParameterPortColor:CircuitBoardView.ExecutionPortColor;
            for(int j=1;j<preview.Count;j++)if((preview[j]-preview[j-1]).sqrMagnitude>.01f)Line(vh,preview[j-1],preview[j],previewColor);
            for(int j=1;j<preview.Count-1;j++)Join(vh,preview[j],previewColor);
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static void Join(VertexHelper vh,Vector2 p,Color c,float radius=1.7f)
        {
            int first=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);
            const int segments=12;for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);if(i>0)vh.AddTriangle(first,first+i,first+i+1);}
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color c,float radius=1.7f)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*radius;int i=vh.currentVertCount;vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}

