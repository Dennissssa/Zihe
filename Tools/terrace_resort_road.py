from pathlib import Path
import re,math,struct

folder=Path('Assets/Mach/SnowMountain')
# Flat turning platforms alternate with straight, climbable ramps.
controls=[(180,0,-365,5),(320,20,-365,5),(340,20,-365,7),
          (340,20,-330,7),(320,20,-330,5),(160,42,-330,5),
          (140,42,-330,7),(140,42,-285,7),(160,42,-285,5),
          (255,62,-285,5),(275,62,-285,7),(275,62,-260,7),
          (260,72,-220,5),(260,72,-208,5)]
points=[]
for a,b in zip(controls,controls[1:]):
    steps=max(1,math.ceil(math.dist(a[:3],b[:3])/4))
    for i in range(steps):
        t=i/steps;points.append(tuple(a[k]+(b[k]-a[k])*t for k in range(4)))
points.append(controls[-1])
edges=[]
for i,p in enumerate(points):
    a=points[max(0,i-1)];b=points[min(len(points)-1,i+1)]
    def side(a,b):
        dx,dz=b[0]-a[0],b[2]-a[2];length=math.hypot(dx,dz)
        return (-dz/length,dx/length) if length>.001 else None
    before=side(a,p);after=side(p,b)
    before=before or after;after=after or before
    nx,nz=before[0]+after[0],before[1]+after[1];length=math.hypot(nx,nz);nx/=length;nz/=length
    width=p[3]/max(.45,nx*after[0]+nz*after[1])
    edges.append(((p[0]+nx*width,p[1],p[2]+nz*width),(p[0]-nx*width,p[1],p[2]-nz*width)))
vertices=[]
for (a,d),(b,c) in zip(edges,edges[1:]):
    for triangle in ((a,b,c),(a,c,d)):
        p,q,r=triangle;u=[q[i]-p[i] for i in range(3)];v=[r[i]-p[i] for i in range(3)]
        n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]);length=math.sqrt(sum(x*x for x in n));n=tuple(x/length for x in n)
        vertices.extend((p,n) for p in triangle)
asset=folder/'Continuous snow road.asset';s=asset.read_text();count=len(vertices)
s=re.sub(r'(indexCount:|vertexCount:|m_VertexCount:) \d+',lambda m:m[1]+' '+str(count),s)
s=re.sub(r'm_IndexBuffer: .*','m_IndexBuffer: '+struct.pack('<'+'H'*count,*range(count)).hex(),s)
s=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(count*24),s)
s=re.sub(r'_typelessdata: .*','_typelessdata: '+b''.join(struct.pack('<6f',*(p+n)) for p,n in vertices).hex(),s)
def vec(v):return '{x: %g, y: %g, z: %g}'%tuple(v)
lo=[min(v[0][i] for v in vertices) for i in range(3)];hi=[max(v[0][i] for v in vertices) for i in range(3)]
s=re.sub(r'm_Center: \{[^}]*\}','m_Center: '+vec((a+b)/2 for a,b in zip(lo,hi)),s)
s=re.sub(r'm_Extent: \{[^}]*\}','m_Extent: '+vec((b-a)/2 for a,b in zip(lo,hi)),s)
asset.write_text(s)

# Reposition the already-authored edge markers to the new road; keep component references.
path=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=path.read_bytes();text=raw.decode().replace('\r\n','\n')
blocks={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}
categories={name:[] for name in ('Road edge barrier','Route marker pole','Red route marker')}
for b in blocks.values():
    if b.startswith('--- !u!1 '):
        name=re.search(r'm_Name: (.*)',b)
        if name and name[1] in categories:categories[name[1]].append(b)
for name,objects in categories.items():
    for i,go in enumerate(objects):
        n=i//2 if name=='Road edge barrier' else i
        total=math.ceil(len(objects)/2) if name=='Road edge barrier' else len(objects)
        sample=8+round(n/max(1,total-1)*(len(points)-18))
        point=edges[sample][i%2 if name=='Road edge barrier' else 0]
        pos=list(point);pos[1]+=.4 if name=='Road edge barrier' else 1.7 if name=='Route marker pole' else 2.8
        if name=='Red route marker':pos[0]+=.55
        tr=int(re.search(r'component: \{fileID: (\d+)',go)[1]);b=blocks[tr]
        updated=re.sub(r'm_LocalPosition: \{[^}]*\}','m_LocalPosition: '+vec(pos),b)
        if name=='Road edge barrier':
            a=points[sample-1];c=points[sample+1];yaw=math.atan2(c[0]-a[0],c[2]-a[2])/2
            updated=re.sub(r'm_LocalRotation: \{[^}]*\}',f'm_LocalRotation: {{x: 0, y: {math.sin(yaw)}, z: 0, w: {math.cos(yaw)}}}',updated)
        text=text.replace(b,updated)
path.write_bytes(text.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
print('Saved straight ramps and flat turning terraces, with repositioned barriers.')
