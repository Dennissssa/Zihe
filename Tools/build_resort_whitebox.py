from pathlib import Path
import re,math,uuid,shutil
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity')
raw=scene.read_text(encoding='utf-8-sig')
if 'm_Name: Resort Whitebox Area' in raw:raise SystemExit('Resort already exists; edit the authored objects instead of rebuilding.')
parts=re.split(r'(?=^--- !u!)',raw,flags=re.M)
byid={re.match(r'--- !u!\d+ &(-?\d+)',p)[1]:p for p in parts if p.startswith('--- !u!')}
yard=next(p for p in parts if '  m_Name: Foot loading yard\n' in p)
templates={}
for fid in re.findall(r'component: \{fileID: (\d+)\}',yard):
 p=byid[fid];templates[int(re.match(r'--- !u!(\d+)',p)[1])]=p
folder=Path('Assets/Mach/ResortWhitebox');folder.mkdir(exist_ok=True)
if not Path(str(folder)+'.meta').exists():Path(str(folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\n')
mat=folder/'Resort chalk white.mat';mg=uuid.uuid4().hex
m=Path('Assets/Mach/WhiteCourse/Chalk white.mat').read_text().replace('m_Name: Chalk white','m_Name: Resort chalk white')
m=re.sub(r'(- _(?:BaseColor|Color): )\{[^}]+\}',r'\g<1>{r: 0.88, g: 0.89, b: 0.88, a: 1}',m)
mat.write_text(m);Path(str(mat)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {mg}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
nextid=7200300000
nodes=[]
def alloc():
 global nextid
 nextid+=1
 if str(nextid) in byid:raise ValueError('ID collision')
 return nextid
def obj(name,parent=None,pos=(0,0,0),size=None,angle=0,yaw=False):
 n={'go':alloc(),'tr':alloc(),'name':name,'parent':parent,'pos':pos,'size':size,'angle':angle,'yaw':yaw,'children':[]}
 if size:n['components']={k:alloc() for k in (33,23,65)}
 if parent:parent['children'].append(n)
 nodes.append(n);return n
def box(name,parent,pos,size,angle=0,yaw=False):return obj(name,parent,pos,size,angle,yaw)
root=obj('Resort Whitebox Area',pos=(220,0,-70))
site=obj('01 - Site and approach road',root)
box('New flat area 160 x 140m',site,(0,-1,0),(160,2,140))
# Continuous route from the existing yard edge to the new site; both surfaces at Y=0.
a=(-176,-.25,50);b=(-77,-.25,-35)
dx,dz=b[0]-a[0],b[2]-a[2];length=math.hypot(dx,dz)
box('12m access road from mountain foot yard',site,((a[0]+b[0])/2,-.25,(a[2]+b[2])/2),(12,.5,length+3),math.degrees(math.atan2(dx,dz)),True)
box('Arrival apron',site,(0,-.15,-39),(102,.3,22))
# Door/window wall helpers leave actual collision-free openings.
def door_z(name,parent,x,z,y,width,opening=2,height=3.7):
 side=(width-opening)/2
 for sign in (-1,1):box(name+' jamb',parent,(x+sign*(opening/2+side/2),y+height/2,z),(side,height,.24))
 box(name+' lintel',parent,(x,y+(2.7+height)/2,z),(opening,height-2.7,.24))
def door_x(name,parent,x,z,y,length,opening=2):
 side=(length-opening)/2
 for sign in (-1,1):box(name+' jamb',parent,(x,y+1.85,z+sign*(opening/2+side/2)),(.24,3.7,side))
 box(name+' lintel',parent,(x,y+3.2,z),(.24,1,opening))
def window_x(name,parent,x,z,y):
 box(name+' sill',parent,(x,y+.5,z),(.28,1,8))
 box(name+' lintel',parent,(x,y+3.25,z),(.28,.9,8))
 for sign in (-1,1):box(name+' pier',parent,(x,y+1.9,z+sign*2.9),(.28,1.8,2.2))
for label,wx in [('West',-36),('East',36)]:
 wing=obj('02 - '+label+' wing - three floors',root,pos=(wx,0,0))
 inner=1 if wx<0 else -1
 for floor in range(3):
  y=floor*4;level=obj(f'Floor {floor+1} - corridor and rooms',wing)
  box('Floor slab - walk surface',level,(0,y-.15,0),(16,.3,48))
  door_z('South main entrance' if floor==0 else 'South corridor window',level,0,-24,y,16,4 if floor==0 else 3)
  # Upper front opening gets a sill, preventing a walk-out drop.
  if floor>0:box('South window sill',level,(0,y+.5,-24),(3,1,.24))
  door_z('North ramp stair access',level,0,24,y,16,8)
  for side in (-1,1):
   for bay in range(6):
    z=-20+bay*8
    if side==inner and bay==5:continue # open transverse hall into central connector
    room=obj(f'Room {floor+1}{(bay+1)+(0 if side<0 else 6):02d}',level)
    door_x('Room door 2m clear',room,side*2,z,y,8)
    window_x('Exterior window',room,side*8,z,y)
    if bay>0:box('Room partition',room,(side*5,y+1.85,z-4),(6,3.7,.24))
   if side==inner:
    # The last bay is a cross-hall, not a sealed room.
    box('Cross-hall divider',level,(side*5,y+1.85,16),(6,3.7,.24))
  box('Floor ceiling beam',level,(0,y+3.8,0),(16,.1,48))
  # Beam here is the underside ceiling; stair tower is outside this slab footprint.
 box('Flat roof',wing,(0,11.95,0),(16.6,.3,48.6))
 stairs=obj('Ramp stairwell - open switchback void',wing,pos=(0,0,30))
 box('Stair ground slab',stairs,(0,-.15,0),(16,.3,12))
 for side in (-1,1):box('Stairwell outer side',stairs,(side*8,6,0),(.28,12,12))
 box('Stairwell back',stairs,(0,6,6),(16,12,.28))
 for level in range(3):box('Floor landing - connects to corridor',stairs,(0,level*4-.15,-5),(16,.3,2))
 for floor in range(2):
  y=floor*4;pitch=math.degrees(math.atan2(2,8));run=math.hypot(8,2)
  # Adjust cube centers so their top surfaces meet each landing without a step.
  offset=.15/math.cos(math.radians(pitch))
  box('Ascending ramp flight',stairs,(-3,y+1-offset,0),(3.6,.3,run+.08),-pitch)
  box('Half-floor turning landing',stairs,(0,y+2-.15,5),(10,.3,2))
  box('Returning ramp flight',stairs,(3,y+3-offset,0),(3.6,.3,run+.08),pitch)
  for x,rot,mid in [(-4.9,-pitch,1),(-1.1,-pitch,1),(1.1,pitch,3),(4.9,pitch,3)]:
   box('Ramp low guard',stairs,(x,y+mid+.5,0),(.12,1,run),rot)
 box('Stair roof',stairs,(0,11.95,0),(16.6,.3,12.6))
connector=obj('04 - Central connecting gallery',root)
for floor in range(3):
 y=floor*4;level=obj(f'Gallery floor {floor+1}',connector)
 box('Gallery slab',level,(0,y-.15,20),(56,.3,8))
 door_z('Courtyard gallery entrance',level,0,16,y,56,4)
 if floor>0:box('Gallery window sill',level,(0,y+.5,16),(4,1,.24))
 door_z('Lobby connection',level,0,24,y,56,6)
 box('Gallery ceiling',level,(0,y+3.85,20),(56,.3,8))
admin=obj('05 - Administration and central lobby',root,pos=(0,0,36))
for floor in range(3):
 y=floor*4;level=obj(f'Lobby level {floor+1}',admin)
 box('Lobby slab',level,(0,y-.15,0),(24,.3,24))
 for side in (-1,1):box('Lobby side wall',level,(side*12,y+1.85,0),(.28,3.7,24))
 door_z('Back exit' if floor==0 else 'Rear window',level,0,12,y,24,4)
 if floor>0:box('Rear sill',level,(0,y+.5,12),(4,1,.24))
 for x in (-7,7):
  door_z('Office partition door',level,x,3,y,10,2)
  box('Office side partition',level,(x/abs(x)*2,y+1.85,7.5),(.24,3.7,9))
 box('Lobby ceiling',level,(0,y+3.85,0),(24,.3,24))
box('Entrance canopy',root,(0,3.2,12),(12,.3,8))
for x in (-5,5):box('Entrance canopy pillar',root,(x,1.6,9),(.4,3.2,.4))
# Serialize plain Unity primitives, never runtime spawning.
def vec(v):return '{x: %.7g, y: %.7g, z: %.7g}'%tuple(v)
def component(template,fid,go):
 t=re.sub(r'^(--- !u!\d+ &)-?\d+',lambda m:m[1]+str(fid),template)
 return re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {go}}}',t)
out=[]
for n in nodes:
 comp=[n['tr']]+list(n.get('components',{}).values())
 g=re.sub(r'^--- !u!1 &\d+',f"--- !u!1 &{n['go']}",yard)
 g=re.sub(r'  m_Component:.*?  m_Layer:', '  m_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for i in comp)+'  m_Layer:',g,flags=re.S)
 g=re.sub(r'  m_Name:.*',f"  m_Name: {n['name']}",g);out.append(g)
 t=component(templates[4],n['tr'],n['go']);a=math.radians(n['angle'])/2
 q=(0,math.sin(a),0,math.cos(a)) if n['yaw'] else (math.sin(a),0,0,math.cos(a))
 t=re.sub(r'm_LocalRotation: \{[^}]+\}','m_LocalRotation: {x: %.8g, y: %.8g, z: %.8g, w: %.8g}'%q,t)
 t=re.sub(r'm_LocalPosition: \{[^}]+\}','m_LocalPosition: '+vec(n['pos']),t)
 t=re.sub(r'm_LocalScale: \{[^}]+\}','m_LocalScale: '+vec(n['size'] or (1,1,1)),t)
 children='\n'+''.join(f"  - {{fileID: {c['tr']}}}\n" for c in n['children']) if n['children'] else ' []\n'
 t=re.sub(r'  m_Children:.*?  m_Father:', '  m_Children:'+children+'  m_Father:',t,flags=re.S)
 t=re.sub(r'm_Father: \{fileID: \d+\}',f"m_Father: {{fileID: {n['parent']['tr'] if n['parent'] else 0}}}",t)
 out.append(t)
 for k,fid in n.get('components',{}).items():
  t=component(templates[k],fid,n['go'])
  if k==23:t=re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+mg,t)
  out.append(t)
marker='--- !u!1660057539 &9223372036854775807'
before,roots=raw.split(marker)
backup=Path('Logs/Before-resort-whitebox.unity');backup.parent.mkdir(exist_ok=True);shutil.copy2(scene,backup)
roots=roots.replace('  m_Roots:\n',f"  m_Roots:\n  - {{fileID: {root['tr']}}}\n",1)
scene.write_text(before+''.join(out)+marker+roots)
(folder/'README.txt').write_text('Resort Whitebox Area\nWalkingPickup scene, root at (220, 0, -70).\nThree floors, west/east wings, central gallery, administration lobby.\n66 partitioned rooms with open doorways. Two switchback ramp stairwells, approx 14 degrees.\nFloor heights: 0 / 4 / 8 metres. All pieces use editable cube meshes and BoxColliders.\n12m access road begins at the east edge of the original mountain-foot loading yard.\nThis is a simplified spatial reference to Shoreline resort, not an exact map reproduction.\nNo runtime generator; edit individual objects under Resort Whitebox Area.\n')
print(f'Saved {len(nodes)} editable objects, including {sum(bool(n["size"]) for n in nodes)} collider primitives, in existing scene.')
