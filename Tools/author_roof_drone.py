from pathlib import Path
import re,math,shutil
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=scene.read_text()
if 'm_Name: Roof Beacon Drone Station' in raw:raise SystemExit('Already authored')
def blocks(s):return {re.match(r'--- !u!\d+ &(-?\d+)',v)[1]:v for v in re.split(r'(?=^--- !u!)',s,flags=re.M) if v.startswith('--- !u!')}
d=blocks(raw);p=blocks(Path('Assets/Mach/Prefabs/FirstPersonPlayer.prefab').read_text())
base=next(v for v in d.values() if '  m_Name: Foot loading yard\n' in v);tm={}
for f in re.findall(r'component: \{fileID: (\d+)\}',base):tm[int(re.match(r'--- !u!(\d+)',d[f])[1])]=d[f]
trucktr='1521236734';truckinstance='1507330637';trucksource='3022473931635180496'
mat={n:re.search(r'guid: (\w+)',Path('Assets/Mach/UtilityTruck/Materials/'+n+'.mat.meta').read_text())[1] for n in ['Ivory enamel','Graphite armor','Amber marker']}
nodes=[];nxt=7700300000

def alloc():
 global nxt
 nxt+=1
 if str(nxt) in d:raise ValueError('ID collision')
 return nxt

def obj(name,parent=None,pos=(0,0,0),size=None,material='Graphite armor',collision=False,angle=0):
 n={'go':alloc(),'tr':alloc(),'name':name,'parent':parent,'pos':pos,'size':size,'mat':material,'angle':angle,'children':[],'extra':[]}
 n['cs']={k:alloc() for k in ([33,23]+([65] if collision else []) if size else [])}
 if isinstance(parent,dict):parent['children'].append(n['tr'])
 nodes.append(n);return n
station=obj('Roof Beacon Drone Station',trucktr,(0,2.86,-.15));script=alloc();station['extra'].append(script)
obj('Launch deck',station,(0,0,0),(1.85,.16,1.5),'Ivory enamel',True)
for x in (-.85,.85):obj('Dock side rail',station,(x,.12,0),(.09,.16,1.5),'Graphite armor',True)
# Side access panel reaches below the roof edge, so E is accessible from ground level.
obj('Drone station access panel',station,(-1.05,-.38,0),(.18,.6,.55),'Graphite armor',True)
obj('Blue-white control key',station,(-1.15,-.32,.05),(.04,.2,.22),'Ivory enamel',True)
for z in (-.52,.52):obj('Landing stripes',station,(0,.086,z),(1.55,.02,.08),'Amber marker')
berth=obj('Drone berth',station,(0,.58,0));drone=obj('Beacon quadcopter',berth)
obj('Ivory fuselage',drone,(0,0,0),(.64,.25,.82),'Ivory enamel')
obj('Battery pack',drone,(0,.16,-.06),(.42,.09,.5),'Graphite armor')
obj('Downward beacon magazine',drone,(0,-.18,-.12),(.3,.12,.32),'Graphite armor')
rotors=[]
for x in (-.62,.62):
 for z in (-.55,.55):
  obj('Diagonal rotor arm',drone,(x*.52,-.02,z*.52),(.07,.08,.85),'Graphite armor',False,math.degrees(math.atan2(x,z)))
  obj('Motor hub',drone,(x,.06,z),(.16,.17,.16),'Graphite armor')
  rotor=obj('Rotor blades',drone,(x,.17,z));rotors.append(rotor['tr'])
  obj('Propeller',rotor,(0,0,0),(.6,.025,.065),'Graphite armor')
  obj('Navigation indicator',drone,(x,.01,z+.09),(.07,.04,.04),'Amber marker')
for x in (-.3,.3):
 obj('Landing leg',drone,(x,-.25,0),(.055,.35,.055),'Graphite armor')
 obj('Landing skid',drone,(x,-.4,0),(.07,.07,.65),'Graphite armor')
mount=obj('Drone gimbal camera',drone,(0,-.26,.48))
obj('Gimbal housing',drone,(0,-.15,.43),(.22,.18,.18),'Graphite armor')
# Standalone authored screen-space canvas: sharp UI independent of world pixel filtering.
hud=obj('Drone HUD - enable to edit');hud['rect']=True;hud['active']=False
canvas=alloc();scaler=alloc();hud['extra'] += [canvas,scaler]
panel=obj('Beacon status panel',hud);panel['rect']=True;panel['rectsize']=(540,125);panel['anchor']=(1,0);panel['ap']=(-290,90)
cr=alloc();im=alloc();panel['extra'] += [cr,im]
label=obj('Selected beacon letter and status',panel);label['rect']=True;label['rectsize']=(510,105)
lcr=alloc();text=alloc();label['extra'] += [lcr,text]
# Also use an authored crosshair in bombing/flight view.
reticle=obj('Drone centre reticle',hud);reticle['rect']=True;reticle['rectsize']=(60,60);rcr=alloc();rt=alloc();reticle['extra'] += [rcr,rt]

def vec(v):return '{x: %.7g, y: %.7g, z: %.7g}'%v

def remap(v,f,go):
 v=re.sub(r'^(--- !u!\d+ &)-?\d+',lambda m:m[1]+str(f),v)
 return re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {go}}}',v)

out=[]
for n in nodes:
 g=re.sub(r'^--- !u!1 &\d+',f"--- !u!1 &{n['go']}",base)
 g=re.sub(r'  m_Name:.*','  m_Name: '+n['name'],g)
 g=re.sub(r'  m_Component:.*?  m_Layer:','  m_Component:\n'+''.join(f'  - component: {{fileID: {f}}}\n' for f in [n['tr']]+list(n['cs'].values())+n['extra'])+'  m_Layer:',g,flags=re.S)
 if n.get('active')==False:g=g.replace('  m_IsActive: 1','  m_IsActive: 0')
 out.append(g)
 v=remap(p['1285599537664336614'] if n.get('rect') else tm[4],n['tr'],n['go'])
 v=re.sub(r'm_LocalPosition: \{[^}]+\}','m_LocalPosition: '+vec(n['pos']),v)
 v=re.sub(r'm_LocalScale: \{[^}]+\}','m_LocalScale: '+vec(n['size'] or (1,1,1)),v)
 a=math.radians(n['angle'])/2;v=re.sub(r'm_LocalRotation: \{[^}]+\}',f'm_LocalRotation: {{x: 0, y: {math.sin(a)}, z: 0, w: {math.cos(a)}}}',v)
 par=n['parent']['tr'] if isinstance(n['parent'],dict) else n['parent'] or 0
 v=re.sub(r'm_Father: \{fileID: \d+\}',f'm_Father: {{fileID: {par}}}',v)
 children='\n'+''.join(f'  - {{fileID: {f}}}\n' for f in n['children']) if n['children'] else ' []\n'
 v=re.sub(r'  m_Children:.*?  m_Father:','  m_Children:'+children+'  m_Father:',v,flags=re.S)
 if n.get('rect'):
  for key,value in [('m_AnchorMin',n.get('anchor',(.5,.5))),('m_AnchorMax',n.get('anchor',(.5,.5))),('m_AnchoredPosition',n.get('ap',(0,0))),('m_SizeDelta',n.get('rectsize',(0,0)))]:v=re.sub(key+r': \{[^}]+\}',f'{key}: {{x: {value[0]}, y: {value[1]}}}',v)
 out.append(v)
 for k,f in n['cs'].items():
  v=remap(tm[k],f,n['go'])
  if k==23:v=re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+mat[n['mat']],v)
  out.append(v)
# Camera/canvas components use existing project component serialization.
out.append(remap(p['2657541087602703475'],canvas,hud['go']).replace('m_SortingOrder: 0','m_SortingOrder: 30'))
out.append(remap(p['3606145178813355299'],scaler,hud['go']))
for f,n in [(cr,panel),(lcr,label),(rcr,reticle)]:out.append(remap(p['2783039882281711419'],f,n['go']))
image=next(v for v in d.values() if 'guid: fe87c0e1cc204ed48ad3b37840f39efc' in v)
image=remap(image,im,panel['go']);image=re.sub(r'  m_Color: \{[^}]+\}','  m_Color: {r: 0.025, g: 0.04, b: 0.055, a: 0.88}',image);image=re.sub(r'  m_Sprite: \{[^}]+\}','  m_Sprite: {fileID: 0}',image);image=re.sub(r'  m_RaycastTarget: \d+','  m_RaycastTarget: 0',image);out.append(image)
for f,n,value in [(text,label,'BEACON DRONE / POINT A'),(rt,reticle,'+')]:
 v=remap(p['3474150314901948735'],f,n['go']);v=re.sub(r'  m_Text:.*','  m_Text: '+value,v)
 if f==text:v=v.replace('m_Alignment: 4','m_Alignment: 3')
 out.append(v)
out.append(f'''--- !u!114 &{script}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {station['go']}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 23c8f3a245a94e91aeec8bf85cd85a34, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::Mach.BeaconDroneStation
  drone: {{fileID: {drone['tr']}}}
  berth: {{fileID: {berth['tr']}}}
  cameraMount: {{fileID: {mount['tr']}}}
  rotors:
'''+''.join(f'  - {{fileID: {r}}}\n' for r in rotors)+f'''  hud: {{fileID: {hud['go']}}}
  beaconLabel: {{fileID: {text}}}
  horizontalSpeed: 9
  verticalSpeed: 5
  acceleration: 14
  collisionRadius: 0.38
  maxRange: 180
  maxHeight: 90
''')
# Register the authored roof attachment as a prefab-instance override.
v=d[truckinstance];entry=f'''    - targetCorrespondingSourceObject: {{fileID: {trucksource}, guid: 03af61817eca5af488be92f1d0c9fafa, type: 3}}
      insertIndex: -1
      addedObject: {{fileID: {station['tr']}}}
'''
v=v.replace('    m_AddedGameObjects:\n','    m_AddedGameObjects:\n'+entry,1);raw=raw.replace(d[truckinstance],v)
shutil.copy2(scene,'Logs/Before-roof-drone.unity')
marker='--- !u!1660057539 &9223372036854775807';a,b=raw.split(marker);b=b.replace('  m_Roots:\n',f"  m_Roots:\n  - {{fileID: {hud['tr']}}}\n",1)
scene.write_text(a+''.join(out)+marker+b)
print('Saved roof station, quadcopter, gimbal, controller and HUD to WalkingPickup.')
