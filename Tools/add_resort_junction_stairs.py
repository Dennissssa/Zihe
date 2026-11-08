from pathlib import Path
import re,math,shutil
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=scene.read_text();parts=re.split(r'(?=^--- !u!)',raw,flags=re.M)
ids={re.match(r'--- !u!\d+ &(-?\d+)',v)[1]:i for i,v in enumerate(parts) if v.startswith('--- !u!')}
def get(i):return parts[ids[str(i)]]
def fid(v):return re.match(r'--- !u!\d+ &(-?\d+)',v)[1]
def go(v):return re.search(r'm_GameObject: \{fileID: (\d+)\}',v)[1]
def name(t):return re.search(r'  m_Name: (.*)',get(go(trs[t])))[1]
trs={fid(v):v for v in parts if v.startswith('--- !u!4 ') and 'm_Father:' in v}
parent={i:re.search(r'm_Father: \{fileID: (\d+)\}',v)[1] for i,v in trs.items()}
joints=[i for i in trs if name(i) in ('West front junction and lounge','East front junction and lounge')]
if len(joints)!=2:raise RuntimeError('Expected both junction halls')
if 'm_Name: Internal switchback ramp stairs' in raw:raise RuntimeError('Internal stairs already authored')
ref=next(i for i in trs if name(i)=='Junction floor slab');tgo=get(go(trs[ref]));templates={}
for f in re.findall(r'component: \{fileID: (\d+)\}',tgo):templates[int(re.match(r'--- !u!(\d+)',get(f))[1])]=get(f)
nextid=7600300000;added=[];links={}
def alloc():
 global nextid
 nextid+=1
 if str(nextid) in ids:raise ValueError('ID collision')
 return nextid
def vec(v):return '{x: %.7g, y: %.7g, z: %.7g}'%tuple(v)
def obj(label,p,size,par,angle=0):
 gid=alloc();cs={k:alloc() for k in ([4,33,23,65] if size else [4])};links.setdefault(str(par),[]).append(cs[4])
 g=re.sub(r'^--- !u!1 &\d+',f'--- !u!1 &{gid}',tgo);g=re.sub(r'  m_Name:.*','  m_Name: '+label,g)
 g=re.sub(r'  m_Component:.*?  m_Layer:','  m_Component:\n'+''.join(f'  - component: {{fileID: {j}}}\n' for j in cs.values())+'  m_Layer:',g,flags=re.S);added.append(g)
 for k,f in cs.items():
  v=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m[1]+str(f),templates[k]);v=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {gid}}}',v)
  if k==4:
   v=re.sub(r'm_LocalPosition: \{[^}]+\}','m_LocalPosition: '+vec(p),v);v=re.sub(r'm_LocalScale: \{[^}]+\}','m_LocalScale: '+vec(size or (1,1,1)),v)
   a=math.radians(angle)/2;v=re.sub(r'm_LocalRotation: \{[^}]+\}',f'm_LocalRotation: {{x: {math.sin(a)}, y: 0, z: 0, w: {math.cos(a)}}}',v)
   v=re.sub(r'm_Father: \{fileID: \d+\}',f'm_Father: {{fileID: {par}}}',v)
  added.append(v)
 return cs[4]
def hide(i):
 g=go(trs[i]);parts[ids[g]]=get(g).replace('  m_IsActive: 1','  m_IsActive: 0')
for joint in joints:
 levels={name(i):i for i in trs if parent[i]==joint}
 for floor in range(3):
  level=levels[f'Junction floor {floor+1}']
  for i in trs:
   if parent[i]!=level:continue
   if (name(i)=='Junction ceiling' and floor<2) or (name(i)=='Junction floor slab' and floor>0):hide(i)
  if floor>0:
   y=floor*4-.15
   for label,p,size in [('South floor',(0,y,-6),(30,.3,12)),('North floor',(0,y,11),(30,.3,2)),('West floor',(-10,y,5),(10,.3,10)),('East floor',(10,y,5),(10,.3,10))]:obj(label+' around stair opening',p,size,level)
 stairs=obj('Internal switchback ramp stairs',(0,0,0),None,joint)
 pitch=math.degrees(math.atan2(2,8));length=math.hypot(2,8);off=.15/math.cos(math.radians(pitch))
 for floor in range(2):
  y=floor*4
  obj('Ramp ascent',(-2.5,y+1-off,4),(3.2,.3,length+.04),stairs,-pitch)
  obj('Half-floor turning landing',(0,y+1.85,9),(8.2,.3,2),stairs)
  obj('Ramp return',(2.5,y+3-off,4),(3.2,.3,length+.04),stairs,pitch)
  for x,rot,mid in [(-4.2,-pitch,1),(-.8,-pitch,1),(.8,pitch,3),(4.2,pitch,3)]:obj('Ramp side guard',(x,y+mid+.5,4),(.1,.9,length),stairs,rot)
 for floor in (1,2):
  y=floor*4
  for x in (-5,5):obj('Stair void side guard',(x,y+.5,5),(.12,1,10),stairs)
  obj('Stair void rear guard',(0,y+.5,10),(10,1,.12),stairs)
# Attach new transforms to their existing or newly authored parents.
for par,children in links.items():
 def attach(v):
  insertion=''.join(f'  - {{fileID: {c}}}\n' for c in children)
  if '  m_Children: []' in v:return v.replace('  m_Children: []\n','  m_Children:\n'+insertion)
  return v.replace('  m_Father:',insertion+'  m_Father:',1)
 if par in ids:parts[ids[par]]=attach(get(par))
 else:
  j=next(j for j,v in enumerate(added) if v.startswith(f'--- !u!4 &{par}\n'));added[j]=attach(added[j])
shutil.copy2(scene,'Logs/Before-internal-junction-stairs.unity')
marker='--- !u!1660057539 &9223372036854775807';a,b=''.join(parts).split(marker);scene.write_text(a+''.join(added)+marker+b)
print('Saved two internal ramp staircases and openings through floors 2 and 3; kept existing bridge and exterior stairs.')
