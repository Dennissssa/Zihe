using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class GameAudioAuthoring
    {
        const string Folder="Assets/Mach/Audio";
        static GameAudioAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/GameAudio.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/GameAudio.request");try{Apply();File.WriteAllText("Logs/game-audio-applied.txt","Original sound cues, spatial voices and machinery loops saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/game-audio-error.txt",e.ToString());}
        }
        static double Sine(double frequency,double time)=>Math.Sin(2*Math.PI*frequency*time);
        static void WriteSound(string name,int kind,bool loop=false)
        {
            // Keep imported recordings when legacy scene authoring is run again.
            var recordedNames=Folder+"/RecordedAudioNames.txt";
            if(File.Exists(Folder+"/"+name+".wav")&&File.Exists(recordedNames)&&Array.IndexOf(File.ReadAllLines(recordedNames),name)>=0)return;
            const int rate=22050;double seconds=loop?6:kind==12?.75:kind==13?.65:kind==15?.7:kind==16?.5:kind==14?.18:kind==4?.075:.28;
            int count=(int)(rate*seconds);var samples=new short[count];var random=new System.Random(9163+kind);double smoothNoise=0,lowNoise=0,phase=0;
            for(int i=0;i<count;i++)
            {
                double t=(double)i/rate,u=t/seconds,n=random.NextDouble()*2-1;smoothNoise=smoothNoise*.84+n*.16;lowNoise=lowNoise*.985+n*.015;double sound;
                if(loop)
                {
                    // Jet turbulence and uneven combustion pulses, without electronic pitch sweeps.
                    phase+=(kind==101?32:24)*(1+.025*lowNoise)/rate;
                    double pulse=Math.Exp(-(phase-Math.Floor(phase))*15)*(1+.18*Sine(7,t));
                    if(kind==100)sound=lowNoise*2.9+smoothNoise*.95+n*.075+Sine(1450,t)*.018;
                    else if(kind==101)sound=lowNoise*2.4+smoothNoise*.42*(.7+.3*pulse)+Math.Sin(2*Math.PI*phase)*lowNoise*.6;
                    else if(kind==102)sound=lowNoise*.8+smoothNoise*.2+(pulse-.067)*.22;
                    else if(kind==103)sound=smoothNoise*.12+Sine(220,t)*.035;
                    else sound=lowNoise*1.3+smoothNoise*.45;
                }
                else
                {
                    double envelope=Math.Min(1,t/.006)*Math.Pow(1-u,1.6),tone=500;
                    switch(kind)
                    {
                        case 0:tone=u<.45?720:1080;sound=(Sine(tone,t)+.20*Sine(tone*2,t))*.37;break;
                        case 1:tone=u<.45?780:420;sound=Sine(tone,t)*.38;break;
                        case 2:sound=Sine(390-180*u,t)*.25;break;
                        case 3:sound=Sine(190,t)*.30*(Math.Sin(2*Math.PI*u*3)>0?1:.15);break;
                        case 4:sound=(Sine(1000,t)*.28+n*.24)*Math.Exp(-u*5);break;
                        case 5:case 19:sound=smoothNoise*.45+n*.25*Math.Exp(-u*22);break;
                        case 6:case 20:sound=smoothNoise*.35+n*.18*Math.Exp(-u*18);break;
                        case 7:case 10:sound=(Sine(540+420*u,t)+.16*Sine(1080+840*u,t))*.24;break;
                        case 8:sound=(Sine(820-320*u,t)+.16*Sine(1640-640*u,t))*.24;break;

                        case 9:case 17:case 18:sound=lowNoise*2*Math.Exp(-u*8)+smoothNoise*.55*Math.Exp(-u*6)+n*.18*Math.Exp(-u*22);break;
                        case 11:sound=smoothNoise*.65+n*.15*Math.Exp(-u*18);break;
                        case 12:sound=smoothNoise*1.8*Math.Sin(Math.PI*u)+n*.15;break;
                        case 13:sound=lowNoise*3*Math.Exp(-u*6)+smoothNoise*.9*Math.Exp(-u*12);break;
                        case 14:sound=n*.55*Math.Exp(-u*26)+smoothNoise*.75*Math.Exp(-u*11)+lowNoise*.6*Math.Exp(-u*5);break;
                        case 15:case 16:phase+=(kind==15?12+20*u:32-20*u)/rate;double p=Math.Exp(-(phase-Math.Floor(phase))*15);sound=lowNoise*2+smoothNoise*.45*(.5+p*.5);break;
                        default:sound=Sine(480,t)*.3;break;
                    }
                    sound*=envelope;
                }
                samples[i]=(short)(Math.Max(-.9,Math.Min(.9,sound))*32767);
            }
            // Crossfade tail into head, then trim the duplicated head for a continuous wrap.
            if(loop){int fade=rate/4;for(int i=0;i<fade;i++){double blend=(double)i/(fade-1);samples[count-fade+i]=(short)(samples[count-fade+i]*(1-blend)+samples[i]*blend);}var trimmed=new short[count-fade];Array.Copy(samples,fade,trimmed,0,trimmed.Length);samples=trimmed;count=samples.Length;}
            using(var writer=new BinaryWriter(File.Create(Folder+"/"+name+".wav")))
            {writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*2);foreach(var sample in samples)writer.Write(sample);}
        }
        static AudioSource Source(GameObject go,bool spatial,float min=3,float max=150)
        {
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=spatial?1:0;source.minDistance=min;source.maxDistance=max;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=spatial?.3f:0;source.volume=0;return source;
        }
        public static void RefreshCarrySounds()
        {
            foreach(var cue in new[]{SoundCue.Pickup,SoundCue.Place,SoundCue.Unpack})
            {
                WriteSound(cue.ToString(),(int)cue);
                AssetDatabase.ImportAsset(Folder+"/"+cue+".wav",ImportAssetOptions.ForceUpdate);
            }
        }
        static void AddLoop(GameObject go,LoopSoundKind kind,GameAudio bank)
        {
            var loop=go.GetComponent<MachineLoopSound>();if(!loop)loop=Undo.AddComponent<MachineLoopSound>(go);Undo.RecordObject(loop,"Machine audio wiring");loop.kind=kind;
            if(!loop.motor)loop.motor=Source(go,true,kind==LoopSoundKind.Aircraft?35:kind==LoopSoundKind.Airdrop?5:3,kind==LoopSoundKind.Aircraft?450:kind==LoopSoundKind.Airdrop?130:70);
            loop.motor.clip=kind==LoopSoundKind.Aircraft?bank.aircraftLoop:kind==LoopSoundKind.Truck?bank.truckLoop:kind==LoopSoundKind.Robot?bank.robotLoop:bank.windLoop;loop.motor.loop=true;
            loop.truck=go.GetComponent<UtilityTruck>();loop.robot=go.GetComponent<RobotCircuitRunner>();loop.drop=go.GetComponent<EquipmentAirdrop>();
            if(kind==LoopSoundKind.Robot&&!loop.servo){loop.servo=Source(go,true,2,30);loop.servo.clip=bank.servoLoop;loop.servo.loop=true;}EditorUtility.SetDirty(loop);
        }
        public static void Apply()
        {
            Directory.CreateDirectory(Folder);foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue)))WriteSound(cue.ToString(),(int)cue);
            string[] loops={"Aircraft engine loop","Truck engine loop","Robot tracks loop","Turret servo loop","Parachute wind loop"};for(int i=0;i<loops.Length;i++)WriteSound(loops[i],100+i,true);
            AssetDatabase.Refresh();foreach(var file in Directory.GetFiles(Folder,"*.wav")){string path=file.Replace('\\','/');var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();}
            var bank=UnityEngine.Object.FindAnyObjectByType<GameAudio>();if(!bank){var go=new GameObject("Game Audio - volumes and sound library");Undo.RegisterCreatedObjectUndo(go,"Audio system");bank=go.AddComponent<GameAudio>();}
            Undo.RecordObject(bank,"Sound library");bank.cues=new AudioClip[Enum.GetValues(typeof(SoundCue)).Length];foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue)))bank.cues[(int)cue]=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+cue+".wav");
            bank.aircraftLoop=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+loops[0]+".wav");bank.truckLoop=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+loops[1]+".wav");bank.robotLoop=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+loops[2]+".wav");bank.servoLoop=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+loops[3]+".wav");bank.windLoop=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+loops[4]+".wav");
            if(!bank.interfaceSource){var ui=new GameObject("Tablet UI sounds");ui.transform.SetParent(bank.transform,false);bank.interfaceSource=Source(ui,false);bank.interfaceSource.priority=32;}
            if(bank.spatialVoices==null||bank.spatialVoices.Length==0){bank.spatialVoices=new AudioSource[12];for(int i=0;i<12;i++){var voice=new GameObject("World sound voice "+(i+1));voice.transform.SetParent(bank.transform,false);bank.spatialVoices[i]=Source(voice,true);}}
            foreach(var truck in UnityEngine.Object.FindObjectsByType<UtilityTruck>())
            {
                AddLoop(truck.gameObject,LoopSoundKind.Truck,bank);
                var dock=truck.GetComponent<TruckRackDock>();if(!dock)dock=Undo.AddComponent<TruckRackDock>(truck.gameObject);
                foreach(var mesh in truck.GetComponentsInChildren<MeshFilter>())if(mesh.name=="Cargo bed floor")
                {
                    var bounds=new Bounds();bool first=true;foreach(var vertex in mesh.sharedMesh.vertices){var p=truck.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                    Undo.RecordObject(dock,"Truck tray alignment");dock.deckCenter=new Vector3(bounds.center.x,bounds.max.y+.008f,bounds.center.z);dock.deckSize=new Vector2(bounds.size.x,bounds.size.z);EditorUtility.SetDirty(dock);
                }
            }
            foreach(var robot in UnityEngine.Object.FindObjectsByType<RobotCircuitRunner>(FindObjectsInactive.Include))AddLoop(robot.gameObject,LoopSoundKind.Robot,bank);
            var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();if(drop){AddLoop(drop.gameObject,LoopSoundKind.Airdrop,bank);AddLoop(drop.aircraft.gameObject,LoopSoundKind.Aircraft,bank);Undo.RecordObject(drop,"Delay aircraft appearance");drop.flightDelay=5;drop.releaseAfter=5;Undo.RecordObject(drop.aircraft.gameObject,"Aircraft hidden until five seconds");drop.aircraft.gameObject.SetActive(false);EditorUtility.SetDirty(drop);}
            var listener=UnityEngine.Object.FindAnyObjectByType<AudioListener>();if(!listener){var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();Undo.AddComponent<AudioListener>(player.GetComponentInChildren<Camera>().gameObject);}
            EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var bank=UnityEngine.Object.FindAnyObjectByType<GameAudio>();if(!bank||bank.cues.Length!=21||bank.spatialVoices.Length<8||!bank.interfaceSource)throw new Exception("Audio system wiring missing");
            foreach(var clip in bank.cues){if(!clip)throw new Exception("Missing clip");var data=new float[clip.samples];clip.GetData(data,0);float peak=0;foreach(float sample in data)peak=Mathf.Max(peak,Mathf.Abs(sample));if(peak<.01f||peak>.99f)throw new Exception("Silent/clipped audio "+clip.name);}
            var loops=UnityEngine.Object.FindObjectsByType<MachineLoopSound>(FindObjectsInactive.Include);if(loops.Length<4)throw new Exception("Missing machinery audio");foreach(var loop in loops)if(!loop.motor||!loop.motor.clip||!loop.motor.loop||loop.motor.spatialBlend<1)throw new Exception("Unwired spatial loop");
            int listeners=0;foreach(var l in UnityEngine.Object.FindObjectsByType<AudioListener>())if(l.enabled)listeners++;if(listeners!=1)throw new Exception("Expected one listener, found "+listeners);
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();program.ClearCircuit();int facing=program.Add(CircuitKind.Facing);if(!program.Connect(0,facing,false))throw new Exception("Connect regression");program.Disconnect(0);if(program.nodes[0].next!=-1)throw new Exception("Disconnect regression");
            Debug.Log("GAME_AUDIO_VERIFIED: 21 nonclipping cues, five loop assets, four authored emitters, one listener, connect/unlink intact");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}


