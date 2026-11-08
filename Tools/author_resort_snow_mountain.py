from pathlib import Path
import math, random, re, struct, uuid

scene=Path('Assets/Mach/Scenes/WalkingPickup.unity')
raw=scene.read_bytes()
text=raw.decode('utf-8').replace('\r\n','\n')
assert 'm_Name: Snow mountain approach\n' not in text
blocks={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}
folder=Path('Assets/Mach/SnowMountain');folder.mkdir(exist_ok=True)
Path(str(folder)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\n')
template=Path('Assets/Mach/FireRateConsole/Meshes/Rate_000.asset').read_text()
random.seed(49)
meshes={}
def tri(name,a,b,c):
    u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
    n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
    length=math.sqrt(sum(x*x for x in n))
    if length<1e-8:return
    normal=tuple(x/length for x in n)
    meshes.setdefault(name,[]).extend((p,normal) for p in (a,b,c))
def quad(name,a,b,c,d):
    tri(name,a,b,c);tri(name,a,c,d)
def vec(p):return '{x: %g, y: %g, z: %g}'%tuple(p)

# Rounded switchbacks, with shared road-edge vertices throughout.
controls=[(34,0,-30),(68,0,-90),(68,0,-355),(95,1,-400),(180,8,-400),(360,22,-400),(407,26,-381),(395,30,-355),(280,39,-355),(155,48,-355),(116,51,-334),(135,54,-300),(220,62,-300),(260,66,-280),(260,72,-220),(260,72,-208)]
path=[]
for index in range(len(controls)-1):
    a=controls[max(0,index-1)];b=controls[index];c=controls[index+1];d=controls[min(len(controls)-1,index+2)]
    count=max(8,int(math.dist(b,c)/4))
    for step in range(count):
        t=step/count
        # Linear elevation avoids an overshooting crest at the summit join.
        values=[.5*((2*b[k])+(-a[k]+c[k])*t+(2*a[k]-5*b[k]+4*c[k]-d[k])*t*t+(-a[k]+3*b[k]-3*c[k]+d[k])*t*t*t) for k in (0,2)]
        path.append((values[0],b[1]+(c[1]-b[1])*t,values[1]))
path.append(controls[-1])
edges=[]
for i,p in enumerate(path):
    before=path[max(0,i-1)];after=path[min(len(path)-1,i+1)]
    dx,dz=after[0]-before[0],after[2]-before[2];length=math.hypot(dx,dz)
    nx,nz=-dz/length,dx/length
    edges.append(((p[0]+nx*6,p[1],p[2]+nz*6),(p[0]-nx*6,p[1],p[2]-nz*6)))
for i in range(len(edges)-1):
    left,right=edges[i];nl,nr=edges[i+1]
    quad('Continuous snow road',left,nl,nr,right)
    for a,b in ((left,nl),(nr,right)):
        quad('Road cut rock',a,(a[0],a[1]-5,a[2]),(b[0],b[1]-5,b[2]),b)

# Angular terrain grid: road cuts sit below the road surface, and the summit under the existing foundation.
def height(x,z):
    if 110<=x<=410 and -220<=z<=-30:return 70
    base=max(-8,min(70,(z+430)*.345))
    side=min(1,max(0,min((x-70)/65,(475-x)/65)))
    base=-8+(base+8)*side
    if z>-30:base=max(-8,70-(z+30)*.8)
    base+=random.uniform(-4,4)
    nearby=[p[1]-1.1 for p in path if math.hypot(p[0]-x,p[2]-z)<32]
    return min(base,min(nearby)) if nearby else base
grid={}
for xi,x in enumerate(range(60,481,20)):
    for zi,z in enumerate(range(-440,81,20)):
        grid[xi,zi]=(x,height(x,z),z)
for xi in range(21):
    for zi in range(26):
        a,b,c,d=grid[xi,zi],grid[xi,zi+1],grid[xi+1,zi+1],grid[xi+1,zi]
        name='Snow facets' if random.random()<.28 else 'Slate facets'
        if (xi+zi)%2:quad(name,a,b,c,d)
        else:quad(name,b,c,d,a)

# Distant angular peaks, clear of the road and hospital footprint.
for index,(x,z,r,h) in enumerate([(90,160,140,180),(300,210,160,245),(510,155,140,190),(660,-50,130,170),(690,-300,140,145),(-115,-220,110,120),(-200,10,160,210),(-30,260,150,230)]):
    ring=[]
    for i in range(9):
        angle=i*math.tau/9;radius=r*random.uniform(.78,1.12)
        ring.append((x+math.cos(angle)*radius,-10,z+math.sin(angle)*radius))
    shoulder=[(x+(p[0]-x)*.43,h*.52+random.uniform(-13,13),z+(p[2]-z)*.43) for p in ring]
    peak=(x-r*.13,h,z+r*.08)
    for i in range(9):
        j=(i+1)%9
        quad('Distant blue rock',ring[i],shoulder[i],shoulder[j],ring[j])
        tri('Distant snow',shoulder[i],peak,shoulder[j])

materials={}
palette={'Continuous snow road':(.78,.83,.88),'Road cut rock':(.32,.38,.46),'Snow facets':(.72,.79,.86),'Slate facets':(.43,.50,.59),'Distant blue rock':(.49,.59,.70),'Distant snow':(.70,.80,.90),'Barrier':(.64,.70,.77),'Flag':(.70,.12,.18)}
for name,color in palette.items():
    guid=uuid.uuid4().hex;materials[name]=guid
    material=Path('Assets/Mach/WhiteCourse/Chalk white.mat').read_text()
    material=re.sub(r'm_Name: .*','m_Name: '+name,material)
    material=re.sub(r'(_BaseColor:|_Color:) \{[^}]*\}',lambda m:m[1]+' {r: %g, g: %g, b: %g, a: 1}'%color,material)
    (folder/(name+'.mat')).write_text(material)
    (folder/(name+'.mat.meta')).write_text('fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n')

meshguids={}
for name,vertices in meshes.items():
    count=len(vertices);assert count<65536
    guid=uuid.uuid4().hex;meshguids[name]=guid
    low=[min(v[0][i] for v in vertices) for i in range(3)];high=[max(v[0][i] for v in vertices) for i in range(3)]
    content=template.replace('Rate_000',name)
    content=re.sub(r'(indexCount:|vertexCount:|m_VertexCount:) \d+',lambda m:m[1]+' '+str(count),content)
    content=re.sub(r'm_IndexBuffer: .*','m_IndexBuffer: '+struct.pack('<'+'H'*count,*range(count)).hex(),content)
    content=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(count*24),content)
    content=re.sub(r'_typelessdata: .*','_typelessdata: '+b''.join(struct.pack('<6f',*(p+n)) for p,n in vertices).hex(),content)
    content=re.sub(r'm_Center: \{[^}]*\}','m_Center: '+vec((a+b)/2 for a,b in zip(low,high)),content)
    content=re.sub(r'm_Extent: \{[^}]*\}','m_Extent: '+vec((b-a)/2 for a,b in zip(low,high)),content)
    (folder/(name+'.asset')).write_text(content)
    (folder/(name+'.asset.meta')).write_text('fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 4300000\n')

next_id=8500500000
output=[];children=[]
basego=blocks[2121361127]
templates={int(re.match(r'--- !u!(\d+)',blocks[int(i)])[1]):blocks[int(i)] for i in re.findall(r'component: \{fileID: (\d+)',basego)}
def obj(name,pos=(0,0,0),scale=(1,1,1),material=None,mesh=None,collision=False,yaw=0):
    global next_id
    start=next_id;next_id+=10
    kinds=[4]+([33,23] if material else [])+([64 if mesh else 65] if collision else [])
    ids={kind:start+i+1 for i,kind in enumerate(kinds)}
    go=re.sub(r'^--- !u!1 &\d+',f'--- !u!1 &{start}',basego)
    go=re.sub(r'm_Name: .*','m_Name: '+name,go)
    go=re.sub(r'  m_Component:.*?  m_Layer:', '  m_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for i in ids.values())+'  m_Layer:',go,flags=re.S)
    output.append(go)
    for kind,i in ids.items():
        b=blocks[888655775] if kind==64 else templates[kind]
        b=re.sub(r'^--- !u!\d+ &\d+',f'--- !u!{kind} &{i}',b)
        b=re.sub(r'm_GameObject: \{[^}]*\}',f'm_GameObject: {{fileID: {start}}}',b)
        if kind==4:
            b=re.sub(r'm_LocalPosition: \{[^}]*\}','m_LocalPosition: '+vec(pos),b)
            b=re.sub(r'm_LocalScale: \{[^}]*\}','m_LocalScale: '+vec(scale),b)
            angle=math.radians(yaw)/2
            b=re.sub(r'm_LocalRotation: \{[^}]*\}',f'm_LocalRotation: {{x: 0, y: {math.sin(angle)}, z: 0, w: {math.cos(angle)}}}',b)
            b=re.sub(r'm_Father: \{[^}]*\}',f'm_Father: {{fileID: {0 if start==8500500000 else 8500500001}}}',b)
            if start!=8500500000:children.append(i)
        if kind in (33,64) and mesh:b=re.sub(r'm_Mesh: \{[^}]*\}',f'm_Mesh: {{fileID: 4300000, guid: {meshguids[mesh]}, type: 2}}',b)
        if kind==23:b=re.sub(r'(m_Materials:\n  - )\{[^}]*\}',lambda m:m[1]+f'{{fileID: 2100000, guid: {materials[material]}, type: 2}}',b)
        output.append(b)
obj('Snow mountain approach')
for name in meshes:obj(name,material=name,mesh=name,collision=name=='Continuous snow road')
for i in range(12,len(path)-8,4):
    p=path[i];a=path[i-1];b=path[i+1];yaw=math.degrees(math.atan2(b[0]-a[0],b[2]-a[2]))
    for edge in edges[i]:obj('Road edge barrier',pos=(edge[0],edge[1]+.4,edge[2]),scale=(.55,.8,2.2),material='Barrier',collision=True,yaw=yaw)
    if i%24==12:
        edge=edges[i][0]
        obj('Route marker pole',pos=(edge[0],edge[1]+1.7,edge[2]),scale=(.07,3.4,.07),material='Barrier')
        obj('Red route marker',pos=(edge[0]+.55,edge[1]+2.8,edge[2]),scale=(1.1,.6,.035),material='Flag')
output[1]=output[1].replace('m_Children: []','m_Children:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in children).rstrip())
# Elevate the existing authored hospital as one assembly.
root=blocks[7400300002]
text=text.replace(root,root.replace('x: 260, y: 0, z: -140','x: 260, y: 72, z: -140'))
# The previous flat access strip would float across the new hillside.
old=blocks[7400300010];text=text.replace(old,old.replace('m_IsActive: 1','m_IsActive: 0'))
marker='--- !u!1660057539 &9223372036854775807'
before,after=text.split(marker)
after=after.replace('  m_Roots:\n','  m_Roots:\n  - {fileID: 8500500001}\n',1)
scene.write_bytes((before+''.join(output)+marker+after).replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
print('Authored snow mountain, continuous switchback road, barriers and distant peaks. Resort elevation 72m.')
