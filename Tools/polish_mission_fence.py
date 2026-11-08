from pathlib import Path
import re,uuid,json
p=Path('Assets/Mach/Scenes/WalkingPickup.unity');raw=p.read_bytes();s=raw.decode().replace('\r\n','\n');b={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
shader=Path('Assets/Mach/ResortWhitebox/FenceGlow.shader');sg=uuid.uuid4().hex;Path(str(shader)+'.meta').write_text('fileFormatVersion: 2\nguid: '+sg+'\n')
mat=Path('Assets/Mach/ResortWhitebox/Fence red halo.mat');mg=uuid.uuid4().hex
mat.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Fence red halo
  m_Shader: {fileID: 4800000, guid: '''+sg+''', type: 3}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats: []
    m_Colors:
    - _Color: {r: 3, g: 0.01, b: 0.015, a: 0.2}
''');Path(str(mat)+'.meta').write_text('fileFormatVersion: 2\nguid: '+mg+'\nNativeFormatImporter:\n  mainObjectFileID: 2100000\n')
seq=iter(range(9100900000,9100999999));out=[];children=[]
for gid,go in b.items():
 if 'm_Name: Red laser strand\n' not in go:continue
 components=[int(x) for x in re.findall(r'component: \{fileID: (\d+)',go)];mapping={i:next(seq) for i in [gid]+components}
 for i in [gid]+components:
  v=b[i];v=re.sub(r'(?<=&)\d+|(?<=fileID: )\d+',lambda m:str(mapping.get(int(m[0]),int(m[0]))),v)
  if i==gid:v=v.replace('Red laser strand','Laser glow envelope')
  if v.startswith('--- !u!4 '):
   children.append(mapping[i]);v=v.replace('m_LocalScale: {x: 0.025, y: 0.025,','m_LocalScale: {x: 0.13, y: 0.13,')
  if v.startswith('--- !u!23 '):
   v=re.sub(r'(m_Materials:\n  - \{fileID: 2100000, guid: )\w+',lambda m:m[1]+mg,v);v=v.replace('m_CastShadows: 1','m_CastShadows: 0')
  out.append(v)
v=b[8900700001];s=s.replace(v,v.replace('  m_Children:\n','  m_Children:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in children)))
# An authored world-space briefing board in the tent, using the existing TextMesh font material.
tentgo=next(v for v in b.values() if 'm_Name: Field briefing tent\n' in v);tenttr=int(re.search(r'component: \{fileID: (\d+)',tentgo)[1])
text=next(v for v in b.values() if v.startswith('--- !u!102 '));sourcego=int(re.search(r'm_GameObject: \{fileID: (\d+)',text)[1]);g=b[sourcego];cids=[int(x) for x in re.findall(r'component: \{fileID: (\d+)',g)]
mapping={i:next(seq) for i in [sourcego]+cids};boardtr=None
for i in [sourcego]+cids:
 v=b[i];v=re.sub(r'(?<=&)\d+|(?<=fileID: )\d+',lambda m:str(mapping.get(int(m[0]),int(m[0]))),v)
 if i==sourcego:v=re.sub(r'm_Name: .*','m_Name: FIELD BRIEF - 2 bases / 2 spawners',v)
 if v.startswith('--- !u!4 '):
  boardtr=mapping[i]
  for key,val in [('m_LocalPosition','{x: -2.8, y: 2.7, z: 2.9}'),('m_LocalScale','{x: 1, y: 1, z: 1}'),('m_LocalRotation','{x: 0, y: 0, z: 0, w: 1}'),('m_LocalEulerAnglesHint','{x: 0, y: 0, z: 0}'),('m_Father','{fileID: '+str(tenttr)+'}')]:v=re.sub(key+r': \{[^}]*\}',key+': '+val,v)
 if v.startswith('--- !u!102 '):
  v=re.sub(r'  m_Text:.*?(?=\n  m_OffsetZ:)',lambda m:'  m_Text: '+json.dumps('FIELD BRIEF\nDEFEND 2 BASES\nDESTROY 2 SPAWNERS\nDRONES + ROBOTS ONLY'),v,flags=re.S)
  v=re.sub(r'm_CharacterSize: .*','m_CharacterSize: 0.12',v);v=re.sub(r'm_FontSize: .*','m_FontSize: 60',v);v=re.sub(r'm_Anchor: .*','m_Anchor: 0',v)
 out.append(v)
v=b[tenttr];s=s.replace(v,v.replace('  m_Children:\n',f'  m_Children:\n  - {{fileID: {boardtr}}}\n'))
s=s.replace('--- !u!1660057539 &9223372036854775807',''.join(out)+'--- !u!1660057539 &9223372036854775807');p.write_bytes(s.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
print('Added',len(children),'thin laser glow envelopes and field briefing board.')
