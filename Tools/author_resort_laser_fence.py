from pathlib import Path
import re,math,uuid,wave,struct,random
scene=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=scene.read_bytes();s=raw.decode().replace('\r\n','\n')
if 'm_Name: Resort laser security perimeter' in s:raise SystemExit('Fence already authored; edit existing objects in place.')
b={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
folder=Path('Assets/Mach/ResortWhitebox');base=(folder/'Resort chalk white.mat').read_text()
def meta(p,kind=None):
 q=Path(str(p)+'.meta');g=re.search(r'guid: (\w+)',q.read_text())[1] if q.exists() else uuid.uuid4().hex
 q.write_text('fileFormatVersion: 2\nguid: '+g+'\n'+('NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n' if kind else ''));return g
def material(name,color,metal=0,emission=None):
 p=folder/(name+'.mat');t=base.replace('Resort chalk white',name)
 t=re.sub(r'(- _(?:BaseColor|Color): )\{[^}]+\}',lambda m:m[1]+'{r: %s, g: %s, b: %s, a: 1}'%color,t)
 t=re.sub(r'- _Metallic: .*','- _Metallic: '+str(metal),t)
 if emission:
  t=t.replace('m_ValidKeywords: []','m_ValidKeywords:\n  - _EMISSION').replace('m_LightmapFlags: 4','m_LightmapFlags: 1')
  t=re.sub(r'- _EmissionColor: .*','- _EmissionColor: {r: %s, g: %s, b: %s, a: 1}'%emission,t)
 p.write_text(t);return meta(p,True)
floor=material('Resort blue grey floors',(.39,.47,.53));silver=material('Fence brushed silver',(.64,.69,.73),.7);red=material('Fence red laser',(.8,.015,.025),0,(3,.025,.045))
# Restrict material replacement to floor meshes belonging to the existing resort.
owned={7400300002}
while True:
 nxt={i for i,v in b.items() if v.startswith('--- !u!4 ') and (m:=re.search(r'm_Father: \{fileID: (\d+)',v)) and int(m[1]) in owned}
 if nxt<=owned:break
 owned|=nxt
count=0
for tr in owned:
 go=int(re.search(r'm_GameObject: \{fileID: (\d+)',b[tr])[1]);g=b[go];name=re.search(r'm_Name: (.*)',g)[1].lower()
 if go!=7400300005 and not any(w in name for w in ['floor slab','admin floor','floor around','landing','ramp flight','gallery slab','ground slab','arrival apron','lobby slab']):continue
 for c in re.findall(r'component: \{fileID: (\d+)',g):
  v=b[int(c)]
  if v.startswith('--- !u!23 '):s=s.replace(v,re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+floor,v));count+=1
# Authored primitives copied only from the departure pad template.
go_template=b[2121361127];templates={int(re.match(r'--- !u!(\d+)',b[int(i)])[1]):b[int(i)] for i in re.findall(r'component: \{fileID: (\d+)',go_template)}
ids=iter(range(8900700000,8900799999));out=[];children=[]
root=next(ids);rt=next(ids);script=next(ids);audio_go=next(ids);audio_tr=next(ids);audio_id=next(ids)
def header(go,tr,name,parent,pos,size,components,yaw=0):
 g=re.sub(r'^--- !u!1 &\d+','--- !u!1 &'+str(go),go_template)
 g=re.sub(r'  m_Component:.*?  m_Layer:','  m_Component:\n'+''.join('  - component: {fileID: %d}\n'%x for x in [tr]+components)+'  m_Layer:',g,flags=re.S)
 g=re.sub(r'm_Name: .*','m_Name: '+name,g);out.append(g)
 t=templates[4];t=re.sub(r'^--- !u!4 &\d+','--- !u!4 &'+str(tr),t);t=re.sub(r'm_GameObject: \{[^}]+\}','m_GameObject: {fileID: %d}'%go,t)
 for field,val in [('m_LocalPosition',pos),('m_LocalScale',size),('m_LocalEulerAnglesHint',(0,yaw,0))]:t=re.sub(field+r': \{[^}]+\}',field+': {x: %g, y: %g, z: %g}'%val,t)
 t=re.sub(r'm_LocalRotation: \{[^}]+\}','m_LocalRotation: {x: 0, y: %g, z: 0, w: %g}'%(math.sin(math.radians(yaw)/2),math.cos(math.radians(yaw)/2)),t)
 t=re.sub(r'  m_Children:.*?  m_Father:','  m_Children: []\n  m_Father:',t,flags=re.S);t=re.sub(r'm_Father: \{[^}]+\}','m_Father: {fileID: %d}'%parent,t);out.append(t)
def box(name,pos,size,mat,yaw=0):
 go,tr,mf,mr=[next(ids) for _ in range(4)];children.append(tr);header(go,tr,name,rt,pos,size,[mf,mr],yaw)
 for typ,cid in [(33,mf),(23,mr)]:
  t=templates[typ];t=re.sub(r'^--- !u!\d+ &\d+',f'--- !u!{typ} &{cid}',t);t=re.sub(r'm_GameObject: \{[^}]+\}','m_GameObject: {fileID: %d}'%go,t)
  if typ==23:t=re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+mat,t)
  out.append(t)
header(root,rt,'Resort laser security perimeter',7400300002,(0,0,0),(1,1,1),[script])
outline=[(-137,-64),(124,-67),(143,-38),(134,56),(95,98),(37,119),(-47,110),(-118,77),(-144,12)]
for a,c in zip(outline,outline[1:]+outline[:1]):
 length=math.dist(a,c);n=math.ceil(length/10);yaw=math.degrees(math.atan2(c[0]-a[0],c[1]-a[1]))
 for i in range(n):
  t=i/n;u=(i+1)/n;x=a[0]+(c[0]-a[0])*t;z=a[1]+(c[1]-a[1])*t
  box('Silver laser pylon',(x,2.25,z),(.19,4.5,.19),silver)
  box('Pylon foot',(x,.09,z),(.52,.18,.52),silver)
  for y in [.45,1.4,2.4,3.4,4.4]:
   box('Red laser strand',(a[0]+(c[0]-a[0])*(t+u)/2,y,a[1]+(c[1]-a[1])*(t+u)/2),(.025,.025,length/n),red,yaw)
# Short electrical discharge asset, generated once offline, never synthesized during gameplay.
random.seed(712);clip=Path('Assets/Mach/Audio/Fence electrical discharge.wav');rate=22050
samples=[]
for i in range(int(rate*.42)):
 t=i/rate;env=(1-t/.42)**2;noise=random.uniform(-1,1);buzz=math.sin(2*math.pi*110*t)+.35*math.sin(2*math.pi*220*t)
 pulse=1 if (i//110)%7==0 else .25
 samples.append(int(max(-1,min(1,env*(noise*.65*pulse+buzz*.25)))*30000))
with wave.open(str(clip),'wb') as w:w.setparams((1,2,rate,0,'NONE','not compressed'));w.writeframes(struct.pack('<'+'h'*len(samples),*samples))
cg=meta(clip);sg=meta(Path('Assets/Mach/Scripts/ResortLaserFence.cs'))
children.append(audio_tr);header(audio_go,audio_tr,'Fence discharge audio',rt,(0,1,0),(1,1,1),[audio_id])
audio=next(v for v in b.values() if v.startswith('--- !u!82 '));audio=re.sub(r'^--- !u!82 &\d+',f'--- !u!82 &{audio_id}',audio);audio=re.sub(r'm_GameObject: \{[^}]+\}','m_GameObject: {fileID: %d}'%audio_go,audio)
for key,val in [('m_audioClip','{fileID: 8300000, guid: '+cg+', type: 3}'),('m_Resource','{fileID: 8300000, guid: '+cg+', type: 3}'),('m_PlayOnAwake','0'),('Loop','0'),('m_Volume','0.7'),('m_Pitch','1'),('Spatialize','0')]:audio=re.sub(r'(?m)^(  '+key+r':) .*',lambda m:m[1]+' '+val,audio)
out.append(audio)
out.append(f'''--- !u!114 &{script}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {sg}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  player: {{fileID: 8600590030}}
  perimeter:
'''+''.join('  - {x: %g, y: 0, z: %g}\n'%v for v in outline)+f'''  fenceHeight: 4.5
  contactWidth: 0.55
  knockbackSpeed: 9
  shockCooldown: 0.65
  shockAudio: {{fileID: {audio_id}}}
''')
out[1]=out[1].replace('  m_Children: []','  m_Children:\n'+'\n'.join('  - {fileID: %d}'%v for v in children))
r=b[7400300002];s=s.replace(r,r.replace('  m_Children:\n','  m_Children:\n  - {fileID: %d}\n'%rt))
s=s.replace('--- !u!1660057539 &9223372036854775807',''.join(out)+'--- !u!1660057539 &9223372036854775807')
scene.write_bytes(s.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode());print('Authored perimeter and recolored',count,'floor meshes')
