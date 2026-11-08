from pathlib import Path
import re,math,uuid,shutil
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity')
raw=scene.read_text(encoding='utf-8-sig')
# Rebuild only the explicitly requested resort hierarchy, preserving the rest of the scene.
original=raw
oldparts=re.split(r'(?=^--- !u!)',raw,flags=re.M)
oldids={re.match(r'--- !u!\d+ &(-?\d+)',v)[1]:v for v in oldparts if v.startswith('--- !u!')}
oldroot=next(v for v in oldparts if '  m_Name: Resort Whitebox Area\n' in v)
rootgo=re.match(r'--- !u!1 &(\d+)',oldroot)[1]
transforms={k:v for k,v in oldids.items() if v.startswith('--- !u!4 ') and 'm_Father:' in v}
roottr=next(k for k,v in transforms.items() if f'm_GameObject: {{fileID: {rootgo}}}' in v)
owned={roottr}
while True:
 added={k for k,v in transforms.items() if re.search(r'm_Father: \{fileID: (\d+)\}',v)[1] in owned}
 if added<=owned:break
 owned|=added
objects={re.search(r'm_GameObject: \{fileID: (\d+)\}',transforms[k])[1] for k in owned}
remove=set(owned)|objects
for go in objects:remove.update(re.findall(r'component: \{fileID: (\d+)\}',oldids[go]))
raw=''.join(v for v in oldparts if not (v.startswith('--- !u!') and re.match(r'--- !u!\d+ &(-?\d+)',v)[1] in remove))
raw=raw.replace(f'  - {{fileID: {roottr}}}\n','')
parts=re.split(r'(?=^--- !u!)',raw,flags=re.M)
byid={re.match(r'--- !u!\d+ &(-?\d+)',p)[1]:p for p in parts if p.startswith('--- !u!')}
yard=next(p for p in parts if '  m_Name: Foot loading yard\n' in p)
templates={}
for fid in re.findall(r'component: \{fileID: (\d+)\}',yard):
 p=byid[fid];templates[int(re.match(r'--- !u!(\d+)',p)[1])]=p
folder=Path('Assets/Mach/ResortWhitebox');folder.mkdir(exist_ok=True)
if not Path(str(folder)+'.meta').exists():Path(str(folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\n')
mat=folder/'Resort chalk white.mat';mg=re.search(r'guid: (\w+)',Path(str(mat)+'.meta').read_text())[1] if Path(str(mat)+'.meta').exists() else uuid.uuid4().hex
m=Path('Assets/Mach/WhiteCourse/Chalk white.mat').read_text().replace('m_Name: Chalk white','m_Name: Resort chalk white')
m=re.sub(r'(- _(?:BaseColor|Color): )\{[^}]+\}',r'\g<1>{r: 0.88, g: 0.89, b: 0.88, a: 1}',m)
mat.write_text(m);Path(str(mat)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {mg}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
nextid=7400300000
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
root=obj('Resort Whitebox Area',pos=(260,0,-140))
site=obj('01 - Site and approach road',root)
box('New flat area 300 x 190m',site,(0,-1,15),(300,2,190))
# Continuous route from the existing yard edge to the new site; both surfaces at Y=0.
a=(-216,-.25,120);b=(-147,-.25,-35)
dx,dz=b[0]-a[0],b[2]-a[2];length=math.hypot(dx,dz)
box('12m access road from mountain foot yard',site,((a[0]+b[0])/2,-.25,(a[2]+b[2])/2),(12,.5,length+3),math.degrees(math.atan2(dx,dz)),True)
box('Arrival apron',site,(0,-.15,-44),(260,.3,32))
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
def ramp_tower(parent,name,pos,floors):
 tower=obj(name,parent,pos=pos)
 box('Ground slab',tower,(0,-.15,0),(16,.3,12))
 for side in (-1,1):
  for f in range(floors):
   box('Stair window sill',tower,(side*8,f*4+.5,0),(.25,1,12))
   box('Stair window head',tower,(side*8,f*4+3.4,0),(.25,.6,12))
   for z in (-5.9,0,5.9):box('Stair window mullion',tower,(side*8,f*4+2.05,z),(.25,2.1,.18))
 box('Stair rear wall',tower,(0,floors*2,6),(16,floors*4,.24))
 for f in range(floors):box('Open stair landing',tower,(0,f*4-.15,-5),(16,.3,2))
 pitch=math.degrees(math.atan2(2,8));run=math.hypot(8,2);offset=.15/math.cos(math.radians(pitch))
 for f in range(floors-1):
  y=f*4
  box('Ramp UP',tower,(-3,y+1-offset,0),(3.6,.3,run+.08),-pitch)
  box('Turning landing',tower,(0,y+1.85,5),(10,.3,2))
  box('Ramp RETURN',tower,(3,y+3-offset,0),(3.6,.3,run+.08),pitch)
  for x,rot,mid in [(-4.9,-pitch,1),(-1.1,-pitch,1),(1.1,pitch,3),(4.9,pitch,3)]:box('Ramp guardrail',tower,(x,y+mid+.5,0),(.1,.9,run),rot)
 box('Stair roof',tower,(0,floors*4-.15,0),(16.5,.3,12.5))
 return tower

def sun_gallery(name,pos,length,turn=False,entrance=False):
 g=obj(name,root,pos=pos,angle=90 if turn else 0,yaw=turn)
 box('Gallery walk surface',g,(0,-.15,0),(length,.3,4.5))
 # No opaque long walls: white glazing frames, broad daylight openings and skylights.
 for side in (-1,1):
  box('Continuous roof edge',g,(0,3.45,side*2.25),(length,.25,.45))
  for j in range(round(length/4)):
   x=-length/2+(j+.5)*4
   if not (entrance and abs(x)<4) and not (turn and abs(x)>length/2-4):box('Low window plinth',g,(x,.3,side*2.25),(4,.6,.18))
  count=math.ceil(length/4)
  for j in range(count+1):
   x=-length/2+length*j/count
   if (entrance and abs(x)<3.5) or (turn and abs(x)>length/2-4):continue
   box('Tall glazing mullion',g,(x,1.9,side*2.25),(.14,3.2,.18))
  box('Window transom',g,(0,2.8,side*2.25),(length,.10,.14))
 for j in range(math.ceil(length/4)+1):
  x=-length/2+length*j/math.ceil(length/4)
  box('Skylight roof rib',g,(x,3.45,0),(.16,.2,4.5))
 return g

for label,wx in [('West',-84),('East',84)]:
 wing=obj('02 - '+label+' accommodation building',root,pos=(wx,0,-8),angle=-90 if wx<0 else 90,yaw=True)
 wing['scale']=(1.2,1,1.5)
 inner=1 if wx<0 else -1
 for floor in range(3):
  y=floor*4;level=obj(f'Floor {floor+1} - 4m hall and room suites',wing)
  box('Floor slab',level,(0,y-.15,0),(20,.3,48))
  door_z('Front entrance' if floor==0 else 'End daylight window',level,0,-24,y,20,4)
  # All three inner-end doors join the front junction building.
  door_z('Stair vestibule entrance',level,0,24,y,20,8)
  for side in (-1,1):
   for bay in range(6):
    z=-20+bay*8
    junction=False
    if junction:
     vestibule=obj('Sun gallery entrance vestibule',level)
     box('Vestibule divider',vestibule,(side*6,y+1.85,z+(4 if bay==0 else -4)),(8,3.7,.22))
     box('Waiting bench seat',vestibule,(side*7,.5,z+2),(3,.2,.6))
     box('Waiting bench back',vestibule,(side*7,.95,z+2.3),(3,.9,.12))
     continue
    room=obj(f'Room {floor+1}{bay+1+(0 if side<0 else 6):02d} - entry and bathroom',level)
    door_x('Recessed room entrance',room,side*2,z,y,8,1.6)
    window_x('Large bedroom window',room,side*10,z,y)
    if bay>0:box('Party wall',room,(side*6,y+1.85,z-4),(8,3.7,.22))
    # Small enclosed bathroom at one side of the entry; main room remains open.
    box('Bathroom side wall',room,(side*5,y+1.5,z-2.5),(.18,3,3))
    door_z('Bathroom door',room,side*3.5,z-1,y,3,1.15,3)
    box('Shower tray blockout',room,(side*3.6,y+.08,z-3.25),(1.8,.16,1.2))
    box('Wardrobe blockout',room,(side*3,y+1.1,z+3.3),(1.2,2.2,.8))
    # Door surrounds make room boundaries readable without decoration.
    for dz in (-.9,.9):box('Door frame',room,(side*2.02,y+1.35,z+dz),(.32,2.7,.1))
  if floor==0:
   box('Reception desk',level,(-6,.6,-21),(3,1.2,1))
   box('Lift shaft blockout',level,(6,1.85,21),(3.5,3.7,3))
  # Upper floors are solid here; all vertical circulation is in the separate ramp void.
  box('Ceiling',level,(0,y+3.85,0),(20,.3,48))
 box('Flat roof coping',wing,(0,12.06,0),(20.6,.12,48.6))
 ramp_tower(wing,'North ramp stair tower',(0,0,30),3)
# The map's front row is west wing -- west junction -- sun gallery -- east junction -- east wing.
def facade_z(parent,name,z,y,xmin,xmax,openings):
 cursor=xmin
 for x,width in sorted(openings):
  left=x-width/2;right=x+width/2
  if left>cursor:box(name+' pier',parent,((cursor+left)/2,y+1.85,z),(left-cursor,3.7,.24))
  box(name+' lintel',parent,(x,y+3.2,z),(width,1,.24));cursor=right
 if cursor<xmax:box(name+' pier',parent,((cursor+xmax)/2,y+1.85,z),(xmax-cursor,3.7,.24))
for side in (-1,1):
 joint=obj(('West' if side<0 else 'East')+' front junction and lounge',root,pos=(side*33,0,-2))
 for f in range(3):
  y=f*4;l=obj('Junction floor '+str(f+1),joint)
  box('Junction floor slab',l,(0,y-.15,0),(30,.3,24))
  door_z('Front entrance / window',l,0,-12,y,30,4)
  if f>0:box('Front sill',l,(0,y+.5,-12),(4,1,.24))
  facade_z(l,'North gallery door',12,y,-15,15,[(-side*12,4)])
  if f>0:box('North window sill',l,(-side*12,y+.5,12),(4,1,.24))
  door_x('Accommodation wing connection',l,side*15,-6,y,12,4)
  box('Outer wall north segment',l,(side*15,y+1.85,6),(.24,3.7,12))
  door_x('Central sun gallery connection',l,-side*15,0,y,24,4.5)
  if f==2:box('Central window sill',l,(-side*15,y+.5,0),(.24,1,4.5))
  box('Junction ceiling',l,(0,y+3.85,0),(30,.3,24))
  box('Lounge reception counter',l,(side*8,y+.6,5),(4,1.2,1))
  box('Lounge bench',l,(side*7,y+.45,-9),(5,.9,.6))
bridge=sun_gallery('04 - SECOND FLOOR SUN BRIDGE - east to west',(0,4,-2),36)
box('Enclosed bridge roof',bridge,(0,3.6,0),(36,.25,4.8))
for side in (-1,1):
 box('Bridge fascia',bridge,(0,3.55,side*2.25),(36,.5,.3))
 for x in (-12,-4,4,12):box('Ground-level support column',bridge,(x,-2.15,side*2),(.45,3.7,.45))
for side in (-1,1):
 sun_gallery(('West' if side<0 else 'East')+' north-south sun corridor',(side*21,0,26),32,turn=True)

admin=obj('03 - NORTH ADMINISTRATION - independent building',root,pos=(0,0,54))
admin['scale']=(1.5,1,1.2)
for floor in range(2):
 y=floor*4;level=obj(f'Administration level {floor+1}',admin)
 box('Admin floor',level,(0,y-.15,0),(34,.3,20))
 # Front facade has a main entrance and two separate gallery doorways.
 for x,w in [(-16,2),(-8.5,7),(8.5,7),(16,2)]:box('Front facade pier',level,(x,y+1.85,-10),(w,3.7,.24))
 for x,w in [(0,10),(-13.5,3),(13.5,3)]:box('Entrance lintel',level,(x,y+3.2,-10),(w,1,.24))
 if floor==1:
  for x,w in [(0,10),(-13.5,3),(13.5,3)]:box('Front window sill',level,(x,y+.5,-10),(w,1,.24))
 door_z('Admin stairs doorway',level,0,10,y,34,8)
 for side in (-1,1):
  box('Outer side wall',level,(side*17,y+1.85,0),(.25,3.7,20))
  # Off-axis doors and cross partitions form individual offices off the main lobby.
  for z in (-4,4):
   office=obj('Office / treatment room',level)
   door_x('Office entry',office,side*5,z,y,8,1.7)
   box('Office dividing wall',office,(side*11,y+1.85,z+4),(12,3.7,.22))
   box('Simple office counter',office,(side*12,y+.4,z+2),(4,.8,1.2))
 if floor==0:
  box('Lobby reception island',level,(0,.6,1),(4,1.2,1.2))
  for x in (-3,3):box('Lobby structural column',level,(x,1.85,6),(.45,3.7,.45))
 box('Administration ceiling',level,(0,y+3.85,0),(34,.3,20))
ramp_tower(admin,'Admin ramp stair tower',(0,0,16),2)
# Central open-air courtyard remains visibly open between three separate roof masses.
court=obj('05 - Open courtyard and arrival plaza',root)
for x in (-8,8):
 box('Courtyard planter low wall',court,(x,.25,25),(5,.5,12))
 for z in (16,34):box('Courtyard bench',court,(x,.45,z),(4,.9,.55))
box('Front plaza whitebox pad',court,(0,-.1,-45),(72,.2,30))
box('Landing pad simple square',court,(0,.015,-52),(20,.03,20))
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
 t=re.sub(r'm_LocalScale: \{[^}]+\}','m_LocalScale: '+vec(n['size'] or n.get('scale',(1,1,1))),t)
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
backup=Path('Logs/Before-resort-whitebox-v3.unity');backup.parent.mkdir(exist_ok=True)
if not backup.exists():shutil.copy2(scene,backup)
roots=roots.replace('  m_Roots:\n',f"  m_Roots:\n  - {{fileID: {root['tr']}}}\n",1)
scene.write_text(before+''.join(out)+marker+roots)
(folder/'README.txt').write_text('Resort Whitebox Area\nWalkingPickup scene, root at (260, 0, -140).\nMap-oriented footprint: west and east run east-west in the SOUTH/front row. Admin is NORTH/rear. Two north-south sun galleries border the open courtyard. Front sun gallery joins the two junction halls. Approx 276m overall building width on 300 x 190m site.\nSeparate west/east accommodation wings and north administration building. Low sun galleries with open glazing frames and skylight beams. Room vestibules, bathroom partitions, reception and office layouts. Three switchback ramp stairs, approx 14 degrees.\nFloor heights: 0 / 4 / 8 metres. All pieces use editable cube meshes and BoxColliders.\n12m access road begins at the east edge of the original mountain-foot loading yard.\nThis is a simplified spatial reference to Shoreline resort, not an exact map reproduction.\nNo runtime generator; edit individual objects under Resort Whitebox Area.\n')
print(f'Saved {len(nodes)} editable objects, including {sum(bool(n["size"]) for n in nodes)} collider primitives, in existing scene.')

