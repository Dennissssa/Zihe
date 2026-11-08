from pathlib import Path
import re,uuid,json
main=Path('Assets/Mach/Scenes/WalkingPickup.unity');s=main.read_text();b={int(m[1]):m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)[^\n]*\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
def guid(name):
 p=Path('Assets/Mach/Scripts/'+name+'.cs.meta');
 if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
 return re.search(r'guid: (\w+)',p.read_text())[1]
pause,save=guid('MissionPauseMenu'),guid('MissionCheckpoint')
for scene in ['Tutorial','WalkingPickup']:
 p=Path('Assets/Mach/Scenes/'+scene+'.unity');raw=p.read_bytes();s=raw.decode('utf-8-sig').replace('\r\n','\n');seq=iter(range(9401010000,9401010900));out={};children={};ids=[]
 def alloc():
  v=next(seq);ids.append(v);return v
 def clone(i,cid,go):
  t=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m[1]+str(cid),b[i]);return re.sub(r'm_GameObject: \{[^}]*\}',f'm_GameObject: {{fileID: {go}}}',t)
 def elem(name,parent,kind,xy,size,text='',fontSize=25):
  go,tr=alloc(),alloc();idsComp={k:alloc() for k in kind};g=clone(57430118,go,go);g=re.sub(r'  m_Component:.*?  m_Layer: .*','',g,count=0) if False else g
  g=re.sub(r'  m_Component:.*?  m_Layer: \d+','  m_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for i in [tr]+list(idsComp.values()))+'  m_Layer: 5',g,flags=re.S);out[go]=re.sub(r'm_Name: .*','m_Name: '+name,g)
  r=clone(57430119,tr,go);r=re.sub(r'm_Father: \{[^}]*\}',f'm_Father: {{fileID: {parent}}}',r);r=re.sub(r'm_AnchoredPosition: \{[^}]*\}','m_AnchoredPosition: {x: %g, y: %g}'%xy,r);r=re.sub(r'm_SizeDelta: \{[^}]*\}','m_SizeDelta: {x: %g, y: %g}'%size,r);out[tr]=r;children[tr]=[]
  if parent:children[parent].append(tr)
  for k,i in idsComp.items():
   if k in ('canvas','scaler','renderer','image','text'):
    v=clone({'canvas':8600590002,'scaler':8600590003,'renderer':57430121,'image':8600590013,'text':57430120}[k],i,go)
    if k=='canvas':v=v.replace('m_SortingOrder: 200','m_SortingOrder: 885')
    if k=='image':v=v.replace('m_Color: {r: 0, g: 0, b: 0, a: 0}','m_Color: {r: 0.035, g: 0.055, b: 0.07, a: 0.97}')
    if k=='text':
     v=v.replace('m_BestFit: 1','m_BestFit: 0');v=re.sub(r'm_FontSize: \d+','m_FontSize: '+str(fontSize),v);v=re.sub(r'm_Text:.*\Z',lambda m:'m_Text: '+json.dumps(text)+'\n',v,flags=re.S)
    out[i]=v
  return go,tr,idsComp
 root,rt,c=elem('Pause menu canvas',0,['canvas','scaler','pause','save'],(0,0),(0,0));panel,pt,_=elem('Pause panel',rt,['renderer','image'],(0,0),(560,510));out[panel]=out[panel].replace('m_IsActive: 1','m_IsActive: 0')
 _,_,title=elem('Pause title',pt,['renderer','text'],(0,175),(490,60),'PAUSED',38);choices=[]
 for j,v in enumerate(['Settings','Save','Load','Exit to main menu']):
  _,_,row=elem(v,pt,['renderer','text'],(0,91-j*56),(450,48),v.upper(),25);choices.append(row['text'])
 _,_,status=elem('Save status',pt,['renderer','text'],(0,-168),(490,48),'',19)
 for k,g in [('pause',pause),('save',save)]:
  v=clone(9000800394,c[k],root).split('  panel:')[0];v=re.sub(r'(m_Script: \{fileID: 11500000, guid: )\w+',lambda m:m[1]+g,v)
  if k=='pause':v+=f'  panel: {{fileID: {panel}}}\n  heading: {{fileID: {title["text"]}}}\n  status: {{fileID: {status["text"]}}}\n  options:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in choices)+f'  checkpoint: {{fileID: {c["save"]}}}\n  englishFont: {{fileID: 12800000, guid: 511fdf12618875c41a15c474a6d52434, type: 3}}\n  chineseFont: {{fileID: 12800000, guid: 0800462cfab0f4a4996328202757c415, type: 3}}\n'
  out[c[k]]=v
 for tr,ch in children.items():
  if ch:out[tr]=out[tr].replace('  m_Children: []\n','  m_Children:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in ch))
 marker='--- !u!1660057539 &9223372036854775807';a,z=s.split(marker);z=z.replace('  m_Roots:\n',f'  m_Roots:\n  - {{fileID: {rt}}}\n');s=a+''.join(out.values())+marker+z;p.write_bytes(s.replace('\n','\r\n' if b'\r\n' in raw[:500] else '\n').encode());print(scene,'authored pause menu')
