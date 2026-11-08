from pathlib import Path
import ast
import math
import random
import re
import struct
import uuid

folder=Path('Assets/Mach/SnowMountain')
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity')
raw=scene.read_bytes()
scene_text=raw.decode('utf-8').replace('\r\n','\n')
template=Path('Assets/Mach/FireRateConsole/Meshes/Rate_000.asset').read_text(encoding='utf-8-sig')
meshes={}
for node in ast.parse(Path('Tools/author_resort_snow_mountain.py').read_text(encoding='utf-8')).body:
    if isinstance(node,ast.FunctionDef) and node.name in ('tri','quad','vec'):
        exec(compile(ast.Module(body=[node],type_ignores=[]),'<mesh helpers>','exec'))
random.seed(93)

def read_vertices(name):
    data=(folder/(name+'.asset')).read_text()
    binary=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',data)[1])
    return [struct.unpack_from('<6f',binary,i)[:3] for i in range(0,len(binary),24)]

road_vertices=read_vertices('Continuous snow road')
edges=[(road_vertices[i],road_vertices[i+5]) for i in range(0,len(road_vertices),6)]
edges.append((road_vertices[-5],road_vertices[-4]))
road=[tuple((a+b)/2 for a,b in zip(left,right)) for left,right in edges]

def segment_distance(x,z,a,b):
    dx,dz=b[0]-a[0],b[2]-a[2]
    t=max(0,min(1,((x-a[0])*dx+(z-a[2])*dz)/max(.0001,dx*dx+dz*dz)))
    return math.hypot(x-a[0]-t*dx,z-a[2]-t*dz),a[1]+(b[1]-a[1])*t

# Uneven, convex shoulder footprint enclosing the existing three buildings and forecourt.
outline=[(-145,-70),(130,-74),(151,-40),(142,60),(100,105),(38,127),(-50,118),(-125,82),(-152,10)]
plateau=[(260+x,70,-140+z) for x,z in outline]
def plateau_distance(x,z):
    inside=True
    distance=1e9
    for a,b in zip(plateau,plateau[1:]+plateau[:1]):
        if (b[0]-a[0])*(z-a[2])-(b[2]-a[2])*(x-a[0])<0:inside=False
        distance=min(distance,segment_distance(x,z,a,b)[0])
    return -distance if inside else distance

def base_height(x,z):
    # Intersecting ridge masses and erosion channels, rather than a rectangular tower.
    southern=90*max(0,1-abs(x-282)/310-abs(z+190)/410)
    rear=210*max(0,1-abs(x-290)/320-abs(z-100)/340)
    eastern=152*max(0,1-abs(x-467)/250-abs(z+80)/320)
    western=110*max(0,1-abs(x-130)/220-abs(z-35)/260)
    h=max(southern,rear,eastern,western)-14
    channels=abs(math.sin(x*.047+z*.014))*4+abs(math.sin(x*.019-z*.036))*3
    h-=channels
    h+=1.5*math.sin(x*.081+z*.043)
    if x<110 and z>-200:h=min(h,-12+(x-110)*.3)
    # The outermost foothills disappear into the deep mist.
    edge=min((x+80)/110,(730-x)/150,(z+560)/105,(390-z)/130)
    h=-100+(h+100)*max(0,min(1,edge))
    dist=plateau_distance(x,z)
    if dist<=0:return 70
    weight=max(0,1-dist/35)
    h=h*(1-weight)+70*weight
    return h

segments=list(zip(road,road[1:]))
def height(x,z):
    h=base_height(x,z)
    distance,road_y=min((segment_distance(x,z,a,b) for a,b in segments),key=lambda p:p[0])
    if distance<30:
        weight=max(0,min(1,(30-distance)/18))
        weight=weight*weight*(3-2*weight)
        h=h*(1-weight)+(road_y-.18)*weight
    # A low, level departure shelf directly at the new road entrance.
    pad_distance=max(abs(x-164)-22,abs(z+380)-21)
    if pad_distance<18:
        weight=max(0,min(1,1-pad_distance/18))
        h=h*(1-weight)-.2*weight
    if distance<8:h=road_y-.18
    return h

points={}
def add_point(x,y,z):points[(round(x,4),round(z,4))]=(x,y,z)
for x in range(-80,731,24):
    for z in range(-560,391,24):
        px=x+random.uniform(-3,3);pz=z+random.uniform(-3,3)
        add_point(px,height(px,pz),pz)
for x,z in ((290,100),(467,-80),(130,35)):
    add_point(x,height(x,z),z)
for left,right in edges:
    for p in (left,right):add_point(p[0],p[1]-.18,p[2])
    dx,dz=right[0]-left[0],right[2]-left[2]
    for side in (-.25,1.25):
        x,z=left[0]+dx*side,left[2]+dz*side
        add_point(x,height(x,z),z)
for a,b in zip(plateau,plateau[1:]+plateau[:1]):
    steps=max(1,int(math.dist(a,b)/12))
    for i in range(steps):
        t=i/steps;x=a[0]+(b[0]-a[0])*t;z=a[2]+(b[2]-a[2])*t
        add_point(x,height(x,z),z)

# Delaunay mesh includes the road-edge samples, so the slope reaches the road continuously.
vertices=list(points.values())
coords=[(p[0],p[2]) for p in vertices]+[(-10000,-10000),(10000,-10000),(0,10000)]
def circle(a,b,c):
    ax,ay=coords[a];bx,by=coords[b];cx,cy=coords[c]
    divisor=2*(ax*(by-cy)+bx*(cy-ay)+cx*(ay-by))
    if abs(divisor)<1e-9:return None
    aa=ax*ax+ay*ay;bb=bx*bx+by*by;cc=cx*cx+cy*cy
    ux=(aa*(by-cy)+bb*(cy-ay)+cc*(ay-by))/divisor
    uy=(aa*(cx-bx)+bb*(ax-cx)+cc*(bx-ax))/divisor
    return ux,uy,(ux-ax)**2+(uy-ay)**2
count=len(vertices)
triangles=[((count,count+1,count+2),circle(count,count+1,count+2))]
for index,(x,z) in enumerate(coords[:count]):
    boundary={};keep=[]
    for indices,circum in triangles:
        cx,cz,radius=circum
        if (x-cx)**2+(z-cz)**2<=radius+1e-7:
            for a,b in zip(indices,indices[1:]+indices[:1]):
                edge=tuple(sorted((a,b)))
                if edge in boundary:del boundary[edge]
                else:boundary[edge]=(a,b)
        else:keep.append((indices,circum))
    for a,b in boundary.values():
        circum=circle(a,b,index)
        if circum:keep.append(((a,b,index),circum))
    triangles=keep
for indices,_ in triangles:
    if max(indices)>=count:continue
    a,b,c=[vertices[i] for i in indices]
    normal_y=(b[2]-a[2])*(c[0]-a[0])-(b[0]-a[0])*(c[2]-a[2])
    if normal_y<0:b,c=c,b
    slope=(max(a[1],b[1],c[1])-min(a[1],b[1],c[1]))/max(1,math.dist(a,b),math.dist(a,c))
    name='Snow facets' if slope<.33 else 'Slate facets'
    tri(name,a,b,c)

# Irregular foundation replaces only the large rectangular base, preserving all rooms and buildings.
for i in range(len(outline)):
    j=(i+1)%len(outline)
    a=(outline[i][0],0,outline[i][1]);b=(outline[j][0],0,outline[j][1])
    tri('Resort shoulder foundation',(0,0,15),b,a)
    quad('Resort shoulder foundation',a,b,(b[0],-2,b[2]),(a[0],-2,a[2]))
    tri('Resort shoulder foundation',(0,-2,15),(a[0],-2,a[2]),(b[0],-2,b[2]))

# Six connected ridge groups: broad, staggered two-to-five-peak profiles over kilometres.
groups=[(-700,850,1500,650,35,[240,360,315,420,250]),(700,1300,1750,750,-20,[300,480,390,350]),(1650,300,1500,720,72,[330,270,440,370]),(-1150,-600,1350,600,-40,[300,410,330]),(1250,-1450,1600,680,20,[240,350,295,390]),(-150,1950,1900,800,5,[380,450,370,510])]
groups += [(-850,50,920,430,66,[260,325,240]),(500,810,780,420,-25,[285,355]),
           (1080,-550,1150,510,58,[245,330,280,365]),(-320,-1080,730,390,-48,[225,310]),
           (1880,1090,1240,570,12,[330,450,350]),(-1500,1220,870,440,75,[280,370,300])]
# Additional thin, varied silhouettes beyond the doubled primary ridge groups.
groups += [(-1950,170,480,270,30,[310,390]),(2200,-900,690,310,82,[290,420,335]),
           (500,2460,1050,370,-10,[390,470,410,445]),(-850,-1950,570,290,-42,[310,370])]
for gx,gz,length,width,angle,peaks in groups:
    angle=math.radians(angle)
    samples=sorted(set([i/32 for i in range(33)]+[(k+.5)/len(peaks) for k in range(len(peaks))]))
    nx=len(samples)-1;nz=12;grid={}
    for i in range(nx+1):
        u=samples[i];along=(u-.5)*length
        # Interpolate broad summits with saddles between them.
        profile=0
        for k,peak in enumerate(peaks):
            center=(k+.5)/len(peaks)
            profile=max(profile,peak*max(0,1-abs(u-center)*len(peaks)/1.4))
        for j in range(nz+1):
            v=j/nz*2-1;across=v*width*.5
            shift=math.sin(u*8)*width*.07
            shoulder=max(0,1-abs(v))
            erosion=abs(math.sin(v*12+u*23))*22*abs(v)
            y=-110+(profile+110)*shoulder-erosion
            if i==0 or i==nx:y=-110
            x=gx+along*math.cos(angle)-(across+shift)*math.sin(angle)
            z=gz+along*math.sin(angle)+(across+shift)*math.cos(angle)
            grid[i,j]=(x,y,z)
    for i in range(nx):
        for j in range(nz):
            a,b,c,d=grid[i,j],grid[i,j+1],grid[i+1,j+1],grid[i+1,j]
            name='Layered distant snow' if abs((j+.5)/nz*2-1)<.42 else 'Layered distant rock'
            quad(name,a,b,c,d)

guids={}
for name,mesh in meshes.items():
    num=len(mesh);assert num<65536
    meta=folder/(name+'.asset.meta')
    guid=re.search(r'guid: (\w+)',meta.read_text())[1] if meta.exists() else uuid.uuid4().hex
    guids[name]=guid
    meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 4300000\n')
    lo=[min(v[0][i] for v in mesh) for i in range(3)];hi=[max(v[0][i] for v in mesh) for i in range(3)]
    s=template.replace('Rate_000',name)
    s=re.sub(r'(indexCount:|vertexCount:|m_VertexCount:) \d+',lambda m:m[1]+' '+str(num),s)
    s=re.sub(r'm_IndexBuffer: .*','m_IndexBuffer: '+struct.pack('<'+'H'*num,*range(num)).hex(),s)
    s=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(num*24),s)
    s=re.sub(r'_typelessdata: .*','_typelessdata: '+b''.join(struct.pack('<6f',*(p+n)) for p,n in mesh).hex(),s)
    s=re.sub(r'm_Center: \{[^}]*\}','m_Center: '+vec((a+b)/2 for a,b in zip(lo,hi)),s)
    s=re.sub(r'm_Extent: \{[^}]*\}','m_Extent: '+vec((b-a)/2 for a,b in zip(lo,hi)),s)
    (folder/(name+'.asset')).write_text(s)

blocks={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',scene_text,re.M|re.S)}
for b in blocks.values():
    if b.startswith('--- !u!1 ') and 'm_Name: Road cut rock\n' in b:
        scene_text=scene_text.replace(b,b.replace('m_IsActive: 1','m_IsActive: 0'))
old=blocks[7400300006]
new=re.sub(r'm_LocalPosition: \{[^}]*\}','m_LocalPosition: {x: 0, y: 0, z: 0}',old)
new=re.sub(r'm_LocalScale: \{[^}]*\}','m_LocalScale: {x: 1, y: 1, z: 1}',new)
scene_text=scene_text.replace(old,new)
ref='{fileID: 4300000, guid: '+guids['Resort shoulder foundation']+', type: 2}'
old=blocks[7400300007];scene_text=scene_text.replace(old,re.sub(r'm_Mesh: \{[^}]*\}','m_Mesh: '+ref,old))
base=blocks[7400300005]
collider_id=next(int(i) for i in re.findall(r'component: \{fileID: (\d+)',base) if blocks[int(i)].startswith(('--- !u!65','--- !u!64')))
collider=blocks[collider_id] if blocks[collider_id].startswith('--- !u!64') else blocks[888655775]
collider=re.sub(r'^--- !u!64 &\d+',f'--- !u!64 &{collider_id}',collider)
collider=re.sub(r'm_GameObject: \{[^}]*\}','m_GameObject: {fileID: 7400300005}',collider)
collider=re.sub(r'm_Mesh: \{[^}]*\}','m_Mesh: '+ref,collider)
scene_text=scene_text.replace(blocks[collider_id],collider)
scene_text=re.sub(r'  m_FogDensity: .*','  m_FogDensity: 0.00065',scene_text,count=1)
instance=blocks[1471253228781627325]
entry='    - target: {fileID: 1690750842022048513, guid: 3d5cbd6bad0471747abdfe1e97b24433, type: 3}\n      propertyPath: far clip plane\n      value: 5000\n      objectReference: {fileID: 0}\n'
if entry not in instance:
    scene_text=scene_text.replace(instance,instance.replace('    m_Modifications:\n','    m_Modifications:\n'+entry,1))
scene.write_bytes(scene_text.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
print('Rebuilt connected ridges, road-conforming slopes and irregular resort foundation.')
