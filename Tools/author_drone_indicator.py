from pathlib import Path
import re,uuid,shutil
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=scene.read_text();parts=re.split(r'(?=^--- !u!)',raw,flags=re.M)
ids={re.match(r'--- !u!\d+ &(-?\d+)',v)[1]:i for i,v in enumerate(parts) if v.startswith('--- !u!')}
def get(i):return parts[ids[str(i)]]
def fid(v):return re.match(r'--- !u!\d+ &(-?\d+)',v)[1]
def go(v):return re.search(r'm_GameObject: \{fileID: (\d+)\}',v)[1]
station=next(v for v in parts if 'guid: 23c8f3a245a94e91aeec8bf85cd85a34' in v)
drone=re.search(r'  drone: \{fileID: (\d+)\}',station)[1]
stationtr=next(fid(v) for v in parts if v.startswith('--- !u!4 ') and f'm_GameObject: {{fileID: {go(station)}}}' in v)
ref=next(v for v in parts if '  m_Name: Ivory fuselage\n' in v);templates={}
for f in re.findall(r'component: \{fileID: (\d+)\}',ref):templates[int(re.match(r'--- !u!(\d+)',get(f))[1])]=get(f)
folder=Path('Assets/Mach/Drone');folder.mkdir(exist_ok=True)
if not Path(str(folder)+'.meta').exists():Path(str(folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\n')
def mat(name,source,transparent):
 s=Path(source).read_text();s=re.sub(r'  m_Name:.*','  m_Name: '+name,s,count=1)
 if transparent:
  s=re.sub(r'(- _Smoothness: )[^\n]+',r'\g<1>0',s)
 else:
  s=s.replace('  m_ValidKeywords: []','  m_ValidKeywords:\n  - _EMISSION')
  if '_EMISSION' not in s:s=s.replace('  m_ValidKeywords:\n','  m_ValidKeywords:\n  - _EMISSION\n',1)
  s=re.sub(r'(- _EmissionColor: )\{[^}]+\}',r'\g<1>{r: 2.5, g: 0.08, b: 0.04, a: 1}',s)
 path=folder/(name+'.mat');guid=uuid.uuid4().hex;path.write_text(s);Path(str(path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  mainObjectFileID: 2100000\n');return guid
transparent=mat('Drop prediction transparent','Assets/Mach/Tutorial/Rooms/Glass - pale reflective.mat',True)
signalmat=mat('Drone alternating signal','Assets/Mach/UtilityTruck/Materials/Amber marker.mat',False)
nextid=7800300000;added=[];links={}
def alloc():
 global nextid
 nextid+=1
 if str(nextid) in ids:raise RuntimeError('Already authored')
 return nextid
def vec(v):return '{x: %g, y: %g, z: %g}'%v
def obj(name,par,pos,size=None,material=None,active=True):
 gid=alloc();cs={k:alloc() for k in ([4,33,23] if size else [4])};links.setdefault(str(par),[]).append(cs[4])
 g=re.sub(r'^--- !u!1 &\d+',f'--- !u!1 &{gid}',ref);g=re.sub(r'  m_Name:.*','  m_Name: '+name,g)
 g=re.sub(r'  m_Component:.*?  m_Layer:','  m_Component:\n'+''.join(f'  - component: {{fileID: {f}}}\n' for f in cs.values())+'  m_Layer:',g,flags=re.S)
 if not active:g=g.replace('m_IsActive: 1','m_IsActive: 0')
 added.append(g)
 for k,f in cs.items():
  v=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m[1]+str(f),templates[k]);v=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {gid}}}',v)
  if k==4:
   v=re.sub(r'm_LocalPosition: \{[^}]+\}','m_LocalPosition: '+vec(pos),v);v=re.sub(r'm_LocalScale: \{[^}]+\}','m_LocalScale: '+vec(size or (1,1,1)),v);v=re.sub(r'm_Father: \{fileID: \d+\}',f'm_Father: {{fileID: {par}}}',v)
  if k==23:
   v=re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+material,v);v=v.replace('m_CastShadows: 1','m_CastShadows: 0').replace('m_ReceiveShadows: 1','m_ReceiveShadows: 0')
  added.append(v)
 return cs
indicator=obj('Drone drop prediction',stationtr,(0,0,0),active=False);surfaces=[]
for name,pos,size in [('Fill',(0,0,0),(1.4,.005,1.4)),('North edge',(0,.006,.7),(1.45,.012,.035)),('South edge',(0,.006,-.7),(1.45,.012,.035)),('East edge',(.7,.006,0),(.035,.012,1.45)),('West edge',(-.7,.006,0),(.035,.012,1.45)),('Cross X',(0,.008,0),(.42,.012,.035)),('Cross Z',(0,.008,0),(.035,.012,.42))]:surfaces.append(obj(name,indicator[4],pos,size,transparent)[23])
signal=obj('Top alternating red green beacon',drone,(0,.27,-.06),(.14,.13,.14),signalmat)
for par,ch in links.items():
 def attach(v):return v.replace('  m_Children: []\n','  m_Children:\n'+''.join(f'  - {{fileID: {c}}}\n' for c in ch)) if 'm_Children: []' in v else v.replace('  m_Father:',''.join(f'  - {{fileID: {c}}}\n' for c in ch)+'  m_Father:',1)
 if par in ids:parts[ids[par]]=attach(get(par))
 else:
  i=next(i for i,v in enumerate(added) if v.startswith(f'--- !u!4 &{par}\n'));added[i]=attach(added[i])
v=re.sub(r'  maxRange:.*\n','',station);v+=f'  dropIndicator: {{fileID: {indicator[4]}}}\n  indicatorSurfaces:\n'+''.join(f'  - {{fileID: {r}}}\n' for r in surfaces)+f'  topSignal: {{fileID: {signal[23]}}}\n  signalInterval: 0.6\n';parts[ids[fid(station)]]=v
shutil.copy2(scene,'Logs/Before-drone-indicator.unity');marker='--- !u!1660057539 &9223372036854775807';a,b=''.join(parts).split(marker);scene.write_text(a+''.join(added)+marker+b)
print('Saved translucent drop marker, alternating emissive top signal, and removed maxRange setting.')
