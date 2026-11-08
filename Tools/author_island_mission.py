from pathlib import Path
import re,math,uuid,json,ast
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=scene.read_bytes();s=raw.decode().replace('\r\n','\n')
if 'm_Name: Floating carrier departure' in s:raise SystemExit('Mission already authored')
b={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
folder=Path('Assets/Mach/ResortWhitebox');base=(folder/'Resort chalk white.mat').read_text();out=[];ids=iter(range(9000800000,9000899999));children={};newroots=[]
for node in ast.parse(Path('Tools/author_resort_laser_fence.py').read_text()).body:
 if isinstance(node,ast.FunctionDef) and node.name in ('meta','material'):exec(compile(ast.Module(body=[node],type_ignores=[]),'<helpers>','exec'))
def alloc():
 i=next(ids);assert i not in b;return i
def comp(v,cid,go):
 v=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m[1]+str(cid),v);return re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {go}}}',v)
GO=b[2121361127];T=b[2121361128];parts={int(re.match(r'--- !u!(\d+)',b[int(i)])[1]):b[int(i)] for i in re.findall(r'component: \{fileID: (\d+)',GO)}
def go(name,parent=0,pos=(0,0,0),scale=(1,1,1),quat=(0,0,0,1),kinds=()):
 gid,tr=alloc(),alloc();cs={k:alloc() for k in kinds};g=re.sub(r'^--- !u!1 &\d+',f'--- !u!1 &{gid}',GO)
 g=re.sub(r'  m_Component:.*?  m_Layer:','  m_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for i in [tr]+list(cs.values()))+'  m_Layer:',g,flags=re.S);g=re.sub(r'm_Name: .*','m_Name: '+name,g);out.append(g)
 t=comp(T,tr,gid)
 for key,val in [('m_LocalPosition',pos),('m_LocalScale',scale),('m_LocalEulerAnglesHint',(0,0,0))]:t=re.sub(key+r': \{[^}]*\}',key+': {x: %g, y: %g, z: %g}'%val,t)
 t=re.sub(r'm_LocalRotation: \{[^}]*\}','m_LocalRotation: {x: %g, y: %g, z: %g, w: %g}'%quat,t)
 t=re.sub(r'  m_Children:.*?  m_Father:','  m_Children: []\n  m_Father:',t,flags=re.S);t=re.sub(r'm_Father: \{[^}]*\}',f'm_Father: {{fileID: {parent}}}',t);out.append(t)
 children[tr]=[]
 if parent:children.setdefault(parent,[]).append(tr)
 else:newroots.append(tr)
 return gid,tr,cs
def box(name,parent,pos,size,mat,rot=(0,0,0,1),collide=True):
 gid,tr,cs=go(name,parent,pos,size,rot,(33,23,65) if collide else (33,23))
 for typ,cid in cs.items():
  v=comp(parts[typ],cid,gid)
  if typ==23:v=re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+mat,v)
  if typ==65:v=re.sub(r'm_Size: \{[^}]*\}','m_Size: {x: 1, y: 1, z: 1}',v);v=re.sub(r'm_Center: \{[^}]*\}','m_Center: {x: 0, y: 0, z: 0}',v)
  out.append(v)
 return gid,tr
silver=material('Carrier silver',(.59,.65,.68),.4);dark=material('Carrier hull',(.18,.23,.28),.4);cream=material('Field tent fabric',(.62,.67,.62));light=material('Carrier landing lights',(.2,.7,.9),0,(.3,2,3));orange=material('Mission amber',(.85,.36,.12))
# Stationary floating transport, with a clear central driving exit and a connected ramp.
carrier,ctr,_=go('Floating carrier departure',pos=(164,0,-443))
box('Flight deck',ctr,(0,1.5,0),(38,1,40),silver)
box('Armoured lower hull',ctr,(0,-.2,0),(32,2.4,35),dark)
for sign in [-1,1]:
 box('Side pontoon',ctr,(sign*18,-.3,0),(4,3,32),dark)
 box('Deck side rail',ctr,(sign*18,2.7,0),(.25,1.4,39),silver)
 for z in [-13,0,13]:
  box('Hover pod housing',ctr,(sign*16,-2.2,z),(5,1.4,6),silver)
  box('Hover pod glow',ctr,(sign*16,-2.94,z),(3.5,.08,4.2),light,collide=False)
 box('Front rail beside ramp',ctr,(sign*12,2.7,19.7),(12,1.4,.25),silver)
box('Rear rail',ctr,(0,2.7,-19.7),(36,1.4,.25),silver)
for z in range(-15,18,6):
 for x in [-6.5,6.5]:box('Deck lane marker',ctr,(x,2.015,z),(.14,.025,2.2),orange,collide=False)
angle=math.atan2(2,26);q=(math.sin(angle/2),0,0,math.cos(angle/2))
box('Shore access ramp',0,(164,.76,-410),(12,.48,math.hypot(26,2)+.6),silver,q)
for x in [157.85,170.15]:box('Ramp edge rail',0,(x,1.42,-410),(.2,.8,math.hypot(26,2)),silver,q)
# Small field tent on the existing lower shore pad, away from the airdrop landing spot.
tent,ttr,_=go('Field briefing tent',pos=(151,0,-395))
box('Tent floor',ttr,(0,.08,0),(8,.16,7),dark)
for x in [-3.9,3.9]:
 box('Fabric side wall',ttr,(x,1.25,0),(.13,2.5,7),cream)
 for z in [-3.4,3.4]:box('Silver tent pole',ttr,(x,1.5,z),(.12,3,.12),silver)
for sign in [-1,1]:
 a=math.radians(-sign*29);rot=(0,0,math.sin(a/2),math.cos(a/2))
 box('Pitched canvas roof',ttr,(sign*2,3.58,0),(4.7,.12,7.4),cream,rot)
 box('Front opening panel',ttr,(sign*3,1.25,-3.5),(2,2.5,.12),cream)
box('Tent back wall',ttr,(0,1.25,3.5),(8,2.5,.12),cream)
box('Briefing desk',ttr,(0,1,1.8),(5,.18,1.4),silver)
for x in [-2.2,2.2]:box('Desk leg',ttr,(x,.5,1.8),(.15,1,.15),dark)
# Change only root placement overrides, retaining all custom truck / drone modelling.
for instance,source,pg,pos in [(1471253228781627325,8073535930104866452,'3d5cbd6bad0471747abdfe1e97b24433',(158,2.15,-444)),(1507330637,3022473931635180496,'03af61817eca5af488be92f1d0c9fafa',(165,3.55,-440))]:
 old=b[instance];v=old
 for axis,value in zip('xyz',pos):
  pat=r'(    - target: \{fileID: '+str(source)+r', guid: '+pg+r', type: 3\}\n      propertyPath: m_LocalPosition\.'+axis+r'\n      value: )[^\n]*';v,n=re.subn(pat,lambda m:m[1]+str(value),v);assert n==1
 s=s.replace(old,v)
# Copy authored objective hierarchies; retain prefab enemy references, materials and settings.
def clone_tree(roottr,newname,pos):
 owned={roottr}
 while True:
  added={i for i,v in b.items() if v.startswith(('--- !u!4 ','--- !u!224 ')) and (m:=re.search(r'm_Father: \{fileID: (\d+)',v)) and int(m[1]) in owned}
  if added<=owned:break
  owned|=added
 gos={int(re.search(r'm_GameObject: \{fileID: (\d+)',b[i])[1]) for i in owned};allids=set(owned)|gos
 for gid in gos:allids.update(int(x) for x in re.findall(r'component: \{fileID: (\d+)',b[gid]))
 mapping={i:alloc() for i in sorted(allids)};rootgo=int(re.search(r'm_GameObject: \{fileID: (\d+)',b[roottr])[1])
 for i in sorted(allids):
  v=b[i];v=re.sub(r'(?<=&)\d+|(?<=fileID: )\d+',lambda m:str(mapping.get(int(m[0]),int(m[0]))),v)
  if i==roottr:v=re.sub(r'm_LocalPosition: \{[^}]*\}','m_LocalPosition: {x: %g, y: %g, z: %g}'%pos,v)
  if i==rootgo:v=re.sub(r'm_Name: .*','m_Name: '+newname,v)
  out.append(v)
 children.setdefault(828669459,[]).append(mapping[roottr]);return mapping
basecopy=clone_tree(1937008872,'DEFEND - Base B',(295,72.2,-189))
spawncopy=clone_tree(1377393672,'DESTROY - Spawner B',(300,72.3,-146))
for gid,newname in [(1937008869,'DEFEND - Base A'),(1377393671,'DESTROY - Spawner A')]:s=s.replace(b[gid],re.sub(r'm_Name: .*','m_Name: '+newname,b[gid]))
for tr,pos in [(1937008872,(225,72.2,-189)),(1377393672,(220,72.3,-146))]:s=s.replace(b[tr],re.sub(r'm_LocalPosition: \{[^}]*\}','m_LocalPosition: {x: %g, y: %g, z: %g}'%pos,b[tr]))
for sid in [1377393673,spawncopy[1377393673]]:
 if sid==1377393673:v=b[sid]
 else:v=next(v for v in out if v.startswith(f'--- !u!114 &{sid}\n'))
 nv=v+'  destructible: 1\n  maxHealth: 20\n  currentHealth: 20\n'
 if sid!=1377393673:nv=nv.replace('target: {fileID: 1937008870}',f'target: {{fileID: {basecopy[1937008870]}}}')
 if sid==1377393673:s=s.replace(v,nv)
 else:out[out.index(v)]=nv
# Ensure robot shots can hit a solid cage body, not only the bars.
for gid in [1377393671,spawncopy[1377393671]]:
 cid=alloc();v=b[1377393671] if gid==1377393671 else next(v for v in out if v.startswith(f'--- !u!1 &{gid}\n'))
 if gid==1377393671:v=next(m[0] for m in re.finditer(r'^--- !u!1 &1377393671\n.*?(?=^--- !u!)',s,re.M|re.S))
 nv=v.replace('  m_Component:\n',f'  m_Component:\n  - component: {{fileID: {cid}}}\n')
 if gid==1377393671:s=s.replace(v,nv)
 else:out[out.index(v)]=nv
 col=comp(parts[65],cid,gid);col=re.sub(r'm_Size: \{[^}]*\}','m_Size: {x: 2, y: 2.5, z: 2}',col);col=re.sub(r'm_Center: \{[^}]*\}','m_Center: {x: 0, y: 1.25, z: 0}',col);out.append(col)
v=b[828669458];s=s.replace(v,v+f'  protectedBases:\n  - {{fileID: 1937008870}}\n  - {{fileID: {basecopy[1937008870]}}}\n  objectiveSpawners:\n  - {{fileID: 1377393673}}\n  - {{fileID: {spawncopy[1377393673]}}}\n')
# Editable mission / airspace components live in this scene only.
airgo,airtr,ac=go('Main island drone airspace',kinds=(114,));ag=meta(Path('Assets/Mach/Scripts/IslandAirspace.cs'))
def behaviour(cid,gid,guid,fields):return f'''--- !u!114 &{cid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
'''+fields
out.append(behaviour(ac[114],airgo,ag,'  center: {x: 300, y: 0, z: -80}\n  radii: {x: 390, y: 510}\n  ceiling: 191.36\n'))
# UI is authored into the scene, not built during Start.
ug,ut,uc=go('Operation briefing UI',kinds=(223,114));newroots.remove(ut)
# Convert plain Transform to RectTransform, and keep the root registered.
old=next(v for v in out if v.startswith(f'--- !u!4 &{ut}\n'));rect=comp(b[8600590001],ut,ug);rect=rect.replace('  m_Children:\n  - {fileID: 8600590011}','  m_Children: []');out[out.index(old)]=rect;newroots.append(ut)
canvas=comp(b[8600590002],uc[223],ug).replace('m_SortingOrder: 200','m_SortingOrder: 150');out.append(canvas)
scalerid=alloc();rootblock=next(v for v in out if v.startswith(f'--- !u!1 &{ug}\n'));out[out.index(rootblock)]=rootblock.replace('  m_Component:\n',f'  m_Component:\n  - component: {{fileID: {scalerid}}}\n');out.append(comp(b[8600590003],scalerid,ug))
font=re.search(r'guid: (\w+)',Path('Assets/Mach/Fonts/college.ttf.meta').read_text())[1];cnfont=re.search(r'guid: (\w+)',Path('Assets/Mach/Resources/ChineseUIHeavy.ttf.meta').read_text())[1]
texttemplate=b[57430120];crt=b[57430121]
def ui(name,parent,xy,size,text=None,fontsize=24,color=(1,1,1,1)):
 gid,tr,cs=go(name,parent,kinds=(222,114));old=next(v for v in out if v.startswith(f'--- !u!4 &{tr}\n'))
 v=comp(b[57430119],tr,gid);v=re.sub(r'm_Father: \{[^}]*\}',f'm_Father: {{fileID: {parent}}}',v);v=re.sub(r'm_AnchoredPosition: \{[^}]*\}','m_AnchoredPosition: {x: %g, y: %g}'%xy,v);v=re.sub(r'm_SizeDelta: \{[^}]*\}','m_SizeDelta: {x: %g, y: %g}'%size,v);out[out.index(old)]=v
 out.append(comp(crt,cs[222],gid));v=comp(texttemplate if text is not None else b[8600590013],cs[114],gid)
 v=re.sub(r'm_Color: \{[^}]*\}','m_Color: {r: %g, g: %g, b: %g, a: %g}'%color,v)
 if text is not None:
  v=re.sub(r'm_Font: \{[^}]*\}','m_Font: {fileID: 12800000, guid: '+font+', type: 3}',v)
  for key,value in [('m_FontSize',fontsize),('m_MinSize',fontsize),('m_MaxSize',fontsize),('m_BestFit',0),('m_Alignment',0)]:v=re.sub(key+r': \d+',key+': '+str(value),v)
  v=re.sub(r'm_Text:.*',lambda m:'m_Text: '+json.dumps(text),v)
 out.append(v);return gid,tr,cs[114]
panel,pt,_=ui('Mission brief panel',ut,(0,0),(1120,650),color=(.035,.055,.075,.96))
_,_,heading=ui('Mission heading',pt,(0,255),(1000,65),'MISSION BRIEF / SANATORIUM',36,color=(.65,.83,.9,1))
body='01  Drive off the floating carrier. Organize equipment at the field tent.\n\n02  Protect BOTH bases. Destroy TWO enemy spawners and clear enemies.\n\n03  Personnel and vehicles cannot cross the red fence. Use drones and robots.\n\n04  CHECK your circuit, dock the tablet and use the ENGAGE remote.\n\nDrones are restricted to the main island, below the highest summit.\n\n[ENTER] BEGIN OPERATION'
_,_,desc=ui('Mission instructions',pt,(0,-20),(1000,470),body,24)
_,_,restricted=ui('Restricted Area warning',ut,(0,135),(880,100),'Restricted Area\nNO PERSONNEL OR VEHICLES',28,color=(1,.18,.10,1))
mg=meta(Path('Assets/Mach/Scripts/MissionBriefing.cs'))
out.append(behaviour(uc[114],ug,mg,f'  panel: {{fileID: {panel}}}\n  heading: {{fileID: {heading}}}\n  description: {{fileID: {desc}}}\n  restricted: {{fileID: {restricted}}}\n  defense: {{fileID: 828669458}}\n  englishFont: {{fileID: 12800000, guid: {font}, type: 3}}\n  chineseFont: {{fileID: 12800000, guid: {cnfont}, type: 3}}\n  showOnStart: 1\n'))
v=b[8900700002];s=s.replace(v,v+f'  vehicle: {{fileID: 1521236734}}\n  briefing: {{fileID: {uc[114]}}}\n  vehicleHalfSize: {{x: 1.7, y: 3.8}}\n')
# Reduce remote haze while keeping the valley itself opaque.
v=b[8800600100];s=s.replace(v,v.replace('distanceDensity: 0.00055','distanceDensity: 0.00032'))
# Record children for all newly authored groups and extend existing parents.
for tr,ch in children.items():
 if not ch:continue
 candidates=[v for v in out if v.startswith((f'--- !u!4 &{tr}\n',f'--- !u!224 &{tr}\n'))]
 if candidates:
  v=candidates[0];nv=re.sub(r'  m_Children:.*?  m_Father:','  m_Children:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in ch)+'  m_Father:',v,flags=re.S);out[out.index(v)]=nv
 else:
  v=b[tr];s=s.replace(v,v.replace('  m_Children:\n','  m_Children:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in ch)))
marker='--- !u!1660057539 &9223372036854775807';s=s.replace(marker,''.join(out)+marker);before,roots=s.split(marker);roots=roots.replace('  m_Roots:\n','  m_Roots:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in newroots));s=before+marker+roots
scene.write_bytes(s.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
print('Saved carrier, shore ramp, field tent, authored briefing, 2 bases / 2 destructible spawners, airspace and fence vehicle references.')
