from pathlib import Path
import math
import re
import struct

ROOT = Path('Assets/Mach/Drone')
template = Path('Assets/Mach/FireRateConsole/Meshes/Rate_000.asset').read_text(encoding='utf-8-sig')
meshes = {name: [] for name in ('Silver shell', 'Graphite body', 'Orange spine')}

def subtract(a, b):
    return tuple(x-y for x,y in zip(a,b))

def triangle(name, a, b, c, center):
    u,v = subtract(b,a),subtract(c,a)
    normal = (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
    length = math.sqrt(sum(x*x for x in normal))
    if length < 1e-9:
        return
    if sum(normal[i]*(a[i]-center[i]) for i in range(3)) < 0:
        b,c = c,b
        normal = tuple(-x for x in normal)
    normal = tuple(x/length for x in normal)
    meshes[name].extend((point,normal) for point in (a,b,c))

def quad(name, points, center):
    triangle(name,*points[:3],center)
    triangle(name,points[0],points[2],points[3],center)

def ellipsoid(name, center, scale, segments=24, rings=12):
    def point(ring, segment):
        latitude = math.pi*ring/rings
        longitude = math.tau*segment/segments
        return (center[0]+scale[0]*math.sin(latitude)*math.cos(longitude),
                center[1]+scale[1]*math.cos(latitude),
                center[2]+scale[2]*math.sin(latitude)*math.sin(longitude))
    for ring in range(rings):
        for segment in range(segments):
            quad(name,[point(ring,segment),point(ring,segment+1),point(ring+1,segment+1),point(ring+1,segment)],center)

def wing(name, start, end, width_start, width_end, thickness):
    # Convex chamfered cross sections produce a closed volume, including both end caps.
    dx,dz=end[0]-start[0],end[2]-start[2]
    length=math.hypot(dx,dz)
    side=(-dz/length,0,dx/length)
    shape=[(-.8,-1),(.8,-1),(1,-.4),(1,.4),(.8,1),(-.8,1),(-1,.4),(-1,-.4)]
    loops=[]
    for origin,width in ((start,width_start),(end,width_end)):
        loops.append([(origin[0]+side[0]*x*width,origin[1]+y*thickness,origin[2]+side[2]*x*width) for x,y in shape])
    center=tuple((a+b)/2 for a,b in zip(start,end))
    for i in range(8):
        j=(i+1)%8
        quad(name,[loops[0][i],loops[0][j],loops[1][j],loops[1][i]],center)
    for loop,origin in zip(loops,(start,end)):
        for i in range(8):
            triangle(name,origin,loop[i],loop[(i+1)%8],center)

def cage_ring(center, plane):
    # Closed tubes around a flattened spherical volume, with open space between ribs.
    rings=[]
    centers=[]
    for i in range(32):
        angle=math.tau*i/32
        if plane=='horizontal':
            radial=(math.cos(angle),0,math.sin(angle));axis=(0,1,0)
            offset=(.225*radial[0],0,.225*radial[2])
        else:
            radial=(math.cos(angle),math.sin(angle),0) if plane=='x' else (0,math.sin(angle),math.cos(angle))
            axis=(0,0,1) if plane=='x' else (1,0,0)
            offset=(.225*radial[0],.105*radial[1],.225*radial[2])
        c=tuple(a+b for a,b in zip(center,offset));centers.append(c)
        rings.append([tuple(c[k]+.012*(radial[k]*math.cos(j*math.tau/6)+axis[k]*math.sin(j*math.tau/6)) for k in range(3)) for j in range(6)])
    for i in range(32):
        n=(i+1)%32
        midpoint=tuple((a+b)/2 for a,b in zip(centers[i],centers[n]))
        for j in range(6):
            q=(j+1)%6
            quad('Graphite body',[rings[i][j],rings[i][q],rings[n][q],rings[n][j]],midpoint)

# Reference proposal 3: low central belly, swept shoulders, pointed forward arm.
ellipsoid('Graphite body',(0,-.075,-.02),(.36,.25,.49))
ellipsoid('Silver shell',(0,.148,-.12),(.31,.062,.28))
wing('Graphite body',(0,.12,.10),(0,-.025,.91),.245,.085,.070)
wing('Silver shell',(0,.20,.08),(0,.065,.88),.22,.065,.040)
for sign in (-1,1):
    wing('Graphite body',(sign*.13,.105,-.10),(sign*.88,.045,-.40),.25,.12,.082)
    wing('Silver shell',(sign*.12,.19,-.10),(sign*.87,.135,-.40),.22,.10,.035)
    for z in (-.60,.47):
        x=sign*(.72 if z<0 else .51)
        y=-.04 if z<0 else -.12
        wing('Graphite body',(sign*.18,y-.055,z*.6),(x,y-.055,z),.045,.025,.016)
        for plane in ('horizontal','x','z'):
            cage_ring((x,y,z),plane)
wing('Graphite body',(0,.19,-.18),(0,.36,-.69),.13,.055,.055)
wing('Orange spine',(0,.252,-.17),(0,.385,-.56),.085,.035,.018)
wing('Graphite body',(0,.115,.55),(0,.082,.80),.023,.018,.009)

for name, vertices in meshes.items():
    count=len(vertices)
    assert count<65536
    low=[min(v[0][i] for v in vertices) for i in range(3)]
    high=[max(v[0][i] for v in vertices) for i in range(3)]
    def vector(values):
        return '{x: %g, y: %g, z: %g}'%tuple(values)
    content=template.replace('Rate_000',name)
    content=re.sub(r'(indexCount:|vertexCount:|m_VertexCount:) \d+',lambda m:m[1]+' '+str(count),content)
    content=re.sub(r'm_IndexBuffer: .*','m_IndexBuffer: '+struct.pack('<'+'H'*count,*range(count)).hex(),content)
    content=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(count*24),content)
    content=re.sub(r'_typelessdata: .*','_typelessdata: '+b''.join(struct.pack('<6f',*(point+normal)) for point,normal in vertices).hex(),content)
    content=re.sub(r'm_Center: \{[^}]*\}','m_Center: '+vector((a+b)/2 for a,b in zip(low,high)),content)
    content=re.sub(r'm_Extent: \{[^}]*\}','m_Extent: '+vector((b-a)/2 for a,b in zip(low,high)),content)
    (ROOT/(name+'.asset')).write_text(content,encoding='utf-8')

for scene in ('WalkingPickup','Tutorial'):
    path=Path('Assets/Mach/Scenes/'+scene+'.unity')
    raw=path.read_bytes()
    text=raw.decode('utf-8').replace('\r\n','\n')
    parts=re.split(r'(?=^--- !u!)',text,flags=re.M)
    for i,part in enumerate(parts):
        match=re.match(r'--- !u!1 &(\d+)',part)
        if match and 7700300000<int(match[1])<7700310000:
            if any('m_Name: '+name+'\n' in part for name in ('Motor hub','Navigation indicator','Gimbal housing','Downward beacon magazine')):
                parts[i]=part.replace('m_IsActive: 1','m_IsActive: 0')
    text=''.join(parts)
    path.write_bytes(text.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode('utf-8'))
print('Replaced drone meshes with closed convex shells and recessed spherical pods in both scenes.')
