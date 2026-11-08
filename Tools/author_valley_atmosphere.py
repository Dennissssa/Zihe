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


# A staggered outer chain, not individual identical cones.
groups=[(-2450,1500,1550,520,42,[460,580,490,630]),
 (0,3050,2100,650,-8,[520,680,540,610,470]),
 (2350,2200,1750,580,-35,[480,610,550,690]),
 (3250,0,1900,620,80,[560,690,510,600]),
 (2150,-2350,2050,560,36,[480,650,550,720,520]),
 (-250,-3050,1650,490,-16,[520,640,470,590]),
 (-2250,-2100,1750,570,-44,[540,690,570,640]),
 (-3050,-250,1800,590,86,[510,620,490,680])]
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
            name='Outer horizon snow' if abs((j+.5)/nz*2-1)<.42 else 'Outer horizon rock'
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
for block in blocks.values():
    if block.startswith('--- !u!1 ') and ('m_Name: Valley fog at ' in block or 'm_Name: Deep valley floor\n' in block):
        scene_text=scene_text.replace(block,block.replace('m_IsActive: 1','m_IsActive: 0'))
# Geometry haze is now rendered together with sky/valley fog in the depth pass.
scene_text=re.sub(r'  m_Fog: 1','  m_Fog: 0',scene_text,count=1)
append=[]
for old_id,new_id,name in [(8600500000,8800600000,'Outer horizon rock'),(8600500010,8800600010,'Outer horizon snow')]:
    if new_id in blocks:continue
    mapping={old_id+i:new_id+i for i in range(4)}
    for i in range(4):
        block=blocks[old_id+i]
        block=re.sub(r'(?<=&)\d+|(?<=fileID: )\d+',lambda m:str(mapping.get(int(m[0]),int(m[0]))),block)
        if i==0:block=re.sub(r'  m_Name: .*','  m_Name: '+name,block)
        if i==2:block=re.sub(r'm_Mesh: \{[^}]*\}','m_Mesh: {fileID: 4300000, guid: '+guids[name]+', type: 2}',block)
        append.append(block)
    current=re.search(r'^--- !u!4 &8500500001\n.*?(?=^--- !u!)',scene_text,re.M|re.S)[0]
    scene_text=scene_text.replace(current,current.replace('  m_Children:\n','  m_Children:\n  - {fileID: '+str(new_id+1)+'}\n'))
meta=Path('Assets/Mach/Scripts/MountainAtmosphere.cs.meta')
guid=re.search(r'guid: (\w+)',meta.read_text())[1] if meta.exists() else uuid.uuid4().hex
meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n')
if 8800600100 not in blocks:
    root=blocks[8500500000]
    scene_text=scene_text.replace(root,root.replace('  m_Component:\n','  m_Component:\n  - component: {fileID: 8800600100}\n'))
    append.append('--- !u!114 &8800600100\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 8500500000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  distanceDensity: 0.00055\n  valleyDensity: 0.018\n  valleyHeight: -12\n  heightFalloff: 0.07\n  distantColor: {r: 0.49, g: 0.58, b: 0.68, a: 1}\n  valleyColor: {r: 0.25, g: 0.31, b: 0.39, a: 1}\n')
scene_text=scene_text.replace('--- !u!1660057539 &9223372036854775807',''.join(append)+'--- !u!1660057539 &9223372036854775807')
scene.write_bytes(scene_text.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
print('Saved outer ridge layer and scene atmosphere; disabled solid valley surfaces.')
