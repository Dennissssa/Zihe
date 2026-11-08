using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mach
{
    public static class CircuitWireRouter
    {
        public const float Step=12;
        // Coordinate-compressed routing keeps narrow edge corridors available even
        // when no cell in the regular grid fits. Soft obstacle costs are a last
        // resort for genuinely overlapping endpoints, never a missing wire.
        public static List<Vector2> EdgeRoute(Vector2 start,Vector2 end,List<Rect> obstacles)
            =>CompressedRoute(start,end,obstacles,new List<List<Vector2>>(),true);
        static List<Vector2> CompressedRoute(Vector2 start,Vector2 end,List<Rect> obstacles,List<List<Vector2>> previous,bool fallback)
        {
            var xs=new SortedSet<float>{start.x,end.x};var ys=new SortedSet<float>{start.y,end.y};
            foreach(var r in obstacles){xs.Add(r.xMin-2);xs.Add(r.xMax+2);ys.Add(r.yMin-2);ys.Add(r.yMax+2);}
            foreach(var path in previous)foreach(var p in path){xs.Add(p.x-10);xs.Add(p.x+10);ys.Add(p.y-10);ys.Add(p.y+10);}
            var xx=new List<float>(xs);var yy=new List<float>(ys);int w=xx.Count,h=yy.Count;
            int s=yy.IndexOf(start.y)*w+xx.IndexOf(start.x),t=yy.IndexOf(end.y)*w+xx.IndexOf(end.x);
            var costs=new float[w*h*3];var parents=new int[costs.Length];for(int i=0;i<costs.Length;i++){costs[i]=float.PositiveInfinity;parents[i]=-1;}
            var heap=new Heap();costs[s*3]=0;heap.Add(s*3,Mathf.Abs(start.x-end.x)+Mathf.Abs(start.y-end.y));int found=-1;
            int[] dx={1,-1,0,0},dy={0,0,1,-1};
            while(heap.Count>0)
            {
                var e=heap.Pop();int cell=e.id/3,axis=e.id%3;
                var a=new Vector2(xx[cell%w],yy[cell/w]);
                if(e.score>costs[e.id]+Mathf.Abs(a.x-end.x)+Mathf.Abs(a.y-end.y)+.01f)continue;
                if(cell==t){found=e.id;break;}
                for(int d=0;d<4;d++)
                {
                    int x=cell%w+dx[d],y=cell/w+dy[d];if(x<0||x>=w||y<0||y>=h)continue;
                    var b=new Vector2(xx[x],yy[y]);float penalty=0;
                    foreach(var r in obstacles)
                    {
                        float overlap=d<2?(a.y>r.yMin&&a.y<r.yMax?Mathf.Min(Mathf.Max(a.x,b.x),r.xMax)-Mathf.Max(Mathf.Min(a.x,b.x),r.xMin):0)
                            :(a.x>r.xMin&&a.x<r.xMax?Mathf.Min(Mathf.Max(a.y,b.y),r.yMax)-Mathf.Max(Mathf.Min(a.y,b.y),r.yMin):0);
                        if(overlap>0)penalty+=fallback?100000+overlap*10000:float.PositiveInfinity;
                    }
                    // Sharing a lane costs more than a nearby parallel lane; crossings
                    // are inexpensive and rendered with a bridge gap by the graphic.
                    foreach(var path in previous)for(int j=1;j<path.Count;j++)
                    {
                        var c=path[j-1];var f=path[j];
                        bool horizontal=d<2;
                        bool parallel=horizontal?Mathf.Abs(c.y-f.y)<.01f:Mathf.Abs(c.x-f.x)<.01f;
                        if(!parallel)continue;
                        float distance=horizontal?Mathf.Abs(a.y-c.y):Mathf.Abs(a.x-c.x);
                        float overlap=horizontal?Mathf.Min(Mathf.Max(a.x,b.x),Mathf.Max(c.x,f.x))-Mathf.Max(Mathf.Min(a.x,b.x),Mathf.Min(c.x,f.x))
                            :Mathf.Min(Mathf.Max(a.y,b.y),Mathf.Max(c.y,f.y))-Mathf.Max(Mathf.Min(a.y,b.y),Mathf.Min(c.y,f.y));
                        if(distance<6 && overlap>0)penalty+=overlap*8;
                    }
                    int na=d<2?1:2,n=(y*w+x)*3+na;float cost=costs[e.id]+Vector2.Distance(a,b)+penalty+(axis!=0&&axis!=na?48:0);
                    if(cost>=costs[n])continue;costs[n]=cost;parents[n]=e.id;heap.Add(n,cost+Mathf.Abs(b.x-end.x)+Mathf.Abs(b.y-end.y));
                }
            }
            if(found<0)return null;var route=new List<Vector2>();for(int v=found;v>=0;v=parents[v])route.Add(new Vector2(xx[(v/3)%w],yy[(v/3)/w]));route.Reverse();return route;
        }
        public static List<Vector2> Route(Vector2 start,Vector2 end,List<Rect> obstacles,List<List<Vector2>> previous)
            =>CompressedRoute(start,end,obstacles,previous,false);
        static long Edge(int a,int b)=>((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
        struct Entry{public int id;public float score;public Entry(int i,float s){id=i;score=s;}}
        sealed class Heap
        {
            readonly List<Entry> items=new List<Entry>();public int Count=>items.Count;
            public void Add(int id,float score){items.Add(new Entry(id,score));int i=items.Count-1;while(i>0){int p=(i-1)/2;if(items[p].score<=score)break;items[i]=items[p];i=p;}items[i]=new Entry(id,score);}
            public Entry Pop(){var result=items[0];var last=items[items.Count-1];items.RemoveAt(items.Count-1);if(items.Count==0)return result;int i=0;while(i*2+1<items.Count){int c=i*2+1;if(c+1<items.Count&&items[c+1].score<items[c].score)c++;if(items[c].score>=last.score)break;items[i]=items[c];i=c;}items[i]=last;return result;}
        }
    }
}
