from pathlib import Path
import re,uuid,json
folder=Path('Assets/Mach/PointLauncher/BeaconColors');palette=['ff9377','76e1ae','99a4ff','e6db8f','db80f1','40c4a4','f09f31','93d8ff'];materials={}
for letter,hexrgb in zip('IJKLMNOP',palette):
 rgb=tuple(int(hexrgb[k:k+2],16)/255 for k in (0,2,4));materials[letter]={}
 for k in range(3):
  src=folder/f'H_{k}.mat';dst=folder/f'{letter}_{k}.mat';v=src.read_text().replace(f'H_{k}',f'{letter}_{k}');v=re.sub(r'(- _(?:BaseColor|Color|EmissionColor): )\{r: ([\d.]+), g: [\d.]+, b: [\d.]+, a: ([\d.]+)\}',lambda m:m[1]+'{r: %g, g: %g, b: %g, a: %s}'%(*(x*float(m[2]) for x in rgb),m[3]),v);dst.write_text(v);srcmeta=Path(str(src)+'.meta').read_text();old=re.search('guid: (\\w+)',srcmeta)[1];new=uuid.uuid4().hex;Path(str(dst)+'.meta').write_text(srcmeta.replace(old,new));materials[letter][old]=new

def blocks(s):return {int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
for name in ['WalkingPickup','Tutorial']:
 p=Path(f'Assets/Mach/Scenes/{name}.unity');raw=p.read_bytes();s=raw.decode('utf-8-sig').replace('\r\n','\n');b=blocks(s);h=next(i for i,v in b.items() if '\n  pointId: H\n' in v);gid=int(re.search(r'm_GameObject: \{fileID: (\d+)',b[h])[1]);tr=next(i for i,v in b.items() if v.startswith('--- !u!4 ') and f'm_GameObject: {{fileID: {gid}}}' in v);owned=set()
 def collect(idx):
  v=b[idx];g=int(re.search(r'm_GameObject: \{fileID: (\d+)',v)[1]);owned.add(g);owned.update(int(i) for i in re.findall(r'component: \{fileID: (\d+)',b[g]));
  for child in re.findall(r'- \{fileID: (\d+)\}',v.split('  m_Children:')[1].split('  m_Father:')[0]):collect(int(child))
 collect(tr);seq=iter(range(9301010000,9301020000));out=[];extra=[];roots=[]
 for letter,hexrgb in zip('IJKLMNOP',palette):
  ids={i:next(seq) for i in sorted(owned)};assert not (set(ids.values())&b.keys());extra.append(ids[h]);roots.append(ids[tr]);rgb=tuple(int(hexrgb[k:k+2],16)/255 for k in (0,2,4))
  for i in sorted(owned):
   v=re.sub(r'(?<=&)\d+|(?<=fileID: )\d+',lambda m:str(ids.get(int(m[0]),int(m[0]))),b[i]);v=v.replace('Circuit target H','Circuit target '+letter).replace('pointId: H','pointId: '+letter);v=re.sub(r'(m_Text: |m_Name: .*?\b)H\b',lambda m:m[1]+letter,v);v=re.sub(r'beaconColor: \{[^}]+\}','beaconColor: {r: %g, g: %g, b: %g, a: 1}'%rgb,v)
   for old,new in materials[letter].items():v=v.replace(old,new)
   out.append(v)
 token='  pointMarkers:\n';s=re.sub(r'(  pointMarkers:\n(?:  - \{fileID: \d+\}\n){8})',lambda m:m[0]+''.join(f'  - {{fileID: {i}}}\n' for i in extra),s,count=1);marker='--- !u!1660057539 &9223372036854775807';a,z=s.split(marker);z=z.replace('  m_Roots:\n','  m_Roots:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in roots));s=a+''.join(out)+marker+z
 if name=='WalkingPickup':
  for i,pos,size in [(9000800397,(0,0),(970,490)),(9000800401,(0,161),(850,72)),(9000800405,(0,-35),(840,283))]:
   v=b[i];nv=re.sub(r'm_AnchoredPosition: \{[^}]*\}','m_AnchoredPosition: {x: %d, y: %d}'%pos,v);nv=re.sub(r'm_SizeDelta: \{[^}]*\}','m_SizeDelta: {x: %d, y: %d}'%size,nv);s=s.replace(v,nv)
  for i in [9000800403,9000800407]:s=s.replace(b[i],b[i].replace('m_Alignment: 0','m_Alignment: 4'))
  en='Protect both bases. Destroy both spawners and clear the remaining ghosts.\n\nDrive to the fence. Scout and mark targets with the drone.\nCHECK your circuit, dock the tablet, then press [R] and ENGAGE.\n\n[ENTER]   BEGIN';cn='守住两个基地，摧毁两处出生点，再清除剩余敌人。\n\n开车到电网外，用无人机侦察并投放信标。\n检查电路，插入平板，按 R 拿出遥控器并点击迎战。\n\n[ENTER]   出发';s=re.sub(r'  briefEnglish: .*','  briefEnglish: '+json.dumps(en).replace('\\','\\\\'),s,count=1);s=re.sub(r'  briefChinese: .*',lambda m:'  briefChinese: '+json.dumps(cn,ensure_ascii=False),s,count=1);s=s.replace('  volume: 0.6\n  fadeSeconds: 0.5','  volume: 0.5\n  combatVolume: 0.8\n  fadeSeconds: 0.5')
 p.write_bytes(s.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode())
