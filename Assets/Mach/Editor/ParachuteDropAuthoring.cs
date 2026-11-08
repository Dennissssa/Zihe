using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class ParachuteDropLive
    {
        static ParachuteDropLive(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/ParachuteDrop.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/ParachuteDrop.request");try{AmmoMachineAuthoring.AuthorParachuteDrop();File.WriteAllText("Logs/parachute-drop-applied.txt","Aircraft, canopy, ropes and foothill flight path saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/parachute-drop-error.txt",e.ToString());}
        }
    }
    public static partial class AmmoMachineAuthoring
    {
        const string AirFolder="Assets/Mach/Airdrop";
        static Material AirMaterial(string name,Color color,bool fade=false)
        {
            string path=AirFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.12f);m.SetFloat("_Cull",0);
            if(fade){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;}
            EditorUtility.SetDirty(m);return m;
        }
        static void AirHull(Transform parent,string name,Vector3 pos,float[] z,float[] radius,float yScale,Material material)
        {
            var b=new Builder();Vector3 P(int ring,int side){float a=side*Mathf.PI/4;return new Vector3(Mathf.Cos(a)*radius[ring],Mathf.Sin(a)*radius[ring]*yScale,z[ring]);}
            for(int r=0;r<z.Length-1;r++)for(int s=0;s<8;s++)b.Quad(P(r,s),P(r,s+1),P(r+1,s+1),P(r+1,s));
            for(int s=0;s<8;s++){b.Tri(new Vector3(0,0,z[0]),P(0,s+1),P(0,s));b.Tri(new Vector3(0,0,z[z.Length-1]),P(z.Length-1,s),P(z.Length-1,s+1));}
            Save(name,parent,pos,b,material);
        }
        static void AirWing(Transform parent,string name,Vector3[] outline,float thickness,Material material)
        {
            var b=new Builder();for(int i=0;i<outline.Length;i++){int j=(i+1)%outline.Length;b.Quad(outline[i],outline[j],outline[j]-Vector3.up*thickness,outline[i]-Vector3.up*thickness);if(i>0&&i<outline.Length-1){b.Tri(outline[0],outline[i],outline[j]);b.Tri(outline[0]-Vector3.up*thickness,outline[j]-Vector3.up*thickness,outline[i]-Vector3.up*thickness);}}Save(name,parent,Vector3.zero,b,material);
        }
        public static void AuthorParachuteDrop()
        {
            var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();var mountain=GameObject.Find("Faceted mountain and summit").GetComponent<MeshCollider>();
            if(!rack||!player||!mountain)throw new Exception("Missing saved scene objects");
            var scene=rack.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Before-parachute-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            Directory.CreateDirectory(AirFolder+"/Meshes");AssetDatabase.Refresh();var savedFolder=meshFolder;var savedPrefix=meshPrefix;int savedIndex=meshIndex;meshFolder=AirFolder+"/Meshes";meshPrefix="Air_";meshIndex=0;while(File.Exists(meshFolder+"/Air_"+meshIndex.ToString("D3")+".asset"))meshIndex++;
            try
            {
                var ivoryMat=AirMaterial("Transport ivory",new Color(.84f,.84f,.77f));var dark=AirMaterial("Transport graphite",new Color(.075f,.09f,.11f));var blue=AirMaterial("Transport cobalt",new Color(.0f,.278f,.671f));var glass=AirMaterial("Cockpit blue glass",new Color(.045f,.15f,.20f));
                var cloth=AirMaterial("Canopy ivory",new Color(.92f,.89f,.78f),true);var stripe=AirMaterial("Canopy cobalt",new Color(.045f,.31f,.65f),true);var cord=AirMaterial("Suspension ropes",new Color(.12f,.14f,.16f),true);
                var plane=GameObject.Find("Low poly supply aircraft");if(plane)throw new Exception("Aircraft already authored; edit existing flight fields instead of regenerating");
                plane=new GameObject("Low poly supply aircraft");Undo.RegisterCreatedObjectUndo(plane,"Supply aircraft");var pt=plane.transform;
                AirHull(pt,"Octagonal cargo fuselage",Vector3.zero,new[]{-7f,-4.5f,3.8f,6f,7f},new[]{.35f,1.22f,1.22f,.85f,.12f},.85f,ivoryMat);
                AirHull(pt,"Dark nose cap",new Vector3(0,0,6),new[]{0f,.9f,1.05f},new[]{.85f,.22f,.05f},.85f,dark);
                Box("Cockpit windscreen",pt,new Vector3(0,.64f,5.3f),new Vector3(1.48f,.38f,.72f),glass).transform.localRotation=Quaternion.Euler(-18,0,0);
                foreach(float side in new[]{-1f,1f})
                {
                    AirWing(pt,"Main swept wing",new[]{new Vector3(side*.7f,.5f,2.3f),new Vector3(side*9.4f,.7f,.6f),new Vector3(side*9.0f,.7f,-.8f),new Vector3(side*.7f,.5f,-1.6f)},.19f,ivoryMat);
                    AirWing(pt,"Tail stabilizer",new[]{new Vector3(side*.2f,.55f,-4.7f),new Vector3(side*3.6f,.8f,-5.6f),new Vector3(side*3.5f,.8f,-6.4f),new Vector3(side*.2f,.55f,-6.4f)},.14f,dark);
                    AirHull(pt,"Engine nacelle",new Vector3(side*3.6f,.1f,1.2f),new[]{-1.5f,-.6f,1.6f,1.9f},new[]{.23f,.55f,.55f,.30f},1,dark);
                    Box("Wingtip cobalt band",pt,new Vector3(side*8.6f,.74f,.05f),new Vector3(.7f,.06f,1.25f),blue);
                    Box("Fuselage category stripe",pt,new Vector3(side*1.17f,.10f,.0f),new Vector3(.065f,.32f,6.4f),blue);
                    Box("Rear cargo hatch",pt,new Vector3(side*.45f,-.74f,-4.5f),new Vector3(.8f,.12f,2),dark);
                }
                var tail=new GameObject("Vertical tail fin").transform;tail.SetParent(pt,false);tail.localRotation=Quaternion.Euler(0,0,90);
                AirWing(tail,"Swept tail",new[]{new Vector3(.5f,0,-4.1f),new Vector3(3.1f,0,-5.5f),new Vector3(3.1f,0,-6.5f),new Vector3(.5f,0,-6.5f)},.20f,blue);
                var drop=rack.GetComponent<EquipmentAirdrop>();Undo.RecordObject(drop,"Parachute delivery sequence");drop.propellers=new Transform[2];
                for(int i=0;i<2;i++){var prop=new GameObject("Four blade propeller").transform;prop.SetParent(pt,false);prop.localPosition=new Vector3((i==0?-1:1)*3.6f,.1f,3.18f);Disc("Propeller hub",prop,Vector3.zero,.25f,.25f,8,ivoryMat);for(int k=0;k<2;k++)Box("Propeller blade",prop,Vector3.zero,new Vector3(.13f,2.5f,.09f),dark).transform.localRotation=Quaternion.Euler(0,0,k*90);drop.propellers[i]=prop;}
                var canopy=new GameObject("Deployable parachute and suspension lines").transform;canopy.SetParent(rack.transform,false);Undo.RegisterCreatedObjectUndo(canopy.gameObject,"Authored parachute");
                Vector3 Canopy(float x,float z)=>new Vector3(x,5.25f+1.15f*(1-x*x/9f)+.18f*(1-z*z/4f),z);
                for(int strip=0;strip<8;strip++)
                {
                    float x0=-3+strip*.75f,x1=x0+.75f;var b=new Builder();for(int j=0;j<4;j++){float z0=-2+j,z1=z0+1;b.Quad(Canopy(x0,z0),Canopy(x1,z0),Canopy(x1,z1),Canopy(x0,z1));b.Quad(Canopy(x0,z0)-Vector3.up*.13f,Canopy(x0,z1)-Vector3.up*.13f,Canopy(x1,z1)-Vector3.up*.13f,Canopy(x1,z0)-Vector3.up*.13f);}
                    b.Quad(Canopy(x0,-2),Canopy(x0,-2)-Vector3.up*.13f,Canopy(x1,-2)-Vector3.up*.13f,Canopy(x1,-2));b.Quad(Canopy(x0,2),Canopy(x1,2),Canopy(x1,2)-Vector3.up*.13f,Canopy(x0,2)-Vector3.up*.13f);
                    Save("Faceted canopy cell "+strip,canopy,Vector3.zero,b,strip==1||strip==6?stripe:cloth);
                }
                foreach(float x in new[]{-3f,-1.5f,1.5f,3f})foreach(float z in new[]{-1.9f,1.9f})
                {
                    var a=new Vector3(Mathf.Sign(x)*.78f,.40f,Mathf.Sign(z)*.99f);var b=Canopy(x,z);var line=Box("Suspension line",canopy,(a+b)*.5f,new Vector3(.017f,(b-a).magnitude,.017f),cord);line.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
                }
                canopy.gameObject.SetActive(false);drop.parachute=canopy;drop.aircraft=pt;
                var groundProbe=mountain.transform.TransformPoint(new Vector3(-8,36,25));RaycastHit ground=default;bool found=false;
                foreach(var hit in Physics.RaycastAll(groundProbe+Vector3.up*150,Vector3.down,250,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(rack.transform)&&Vector3.Dot(hit.normal,Vector3.up)>.85f&&(!found||hit.point.y>ground.point.y)){ground=hit;found=true;}
                if(!found)throw new Exception("No clear foothill landing ground");
                drop.landingNormal=ground.normal;drop.landingPosition=ground.point+ground.normal*.025f;
                var direction=Vector3.ProjectOnPlane(drop.landingPosition-player.transform.position,Vector3.up).normalized;drop.flightDirection=direction;drop.flightSpeed=60;drop.flightDelay=1;drop.descentSeconds=10;drop.parachuteFadeSeconds=2.5f;
                var overhead=player.transform.position+Vector3.up*50;drop.planeStart=overhead-direction*90;
                var planeRelease=drop.landingPosition-direction*20;planeRelease.y=overhead.y;
                drop.releaseAfter=drop.flightDelay+Vector3.Dot(planeRelease-drop.planeStart,direction)/drop.flightSpeed;
                pt.SetPositionAndRotation(drop.planeStart,Quaternion.LookRotation(direction));
                Undo.RecordObject(rack.transform,"Stowed equipment in aircraft");rack.transform.SetParent(pt,true);rack.transform.localPosition=new Vector3(0,-1.65f,-2.5f);rack.transform.rotation=Quaternion.identity;
                drop.releasePosition=planeRelease+pt.rotation*new Vector3(0,-1.65f,-2.5f);
                var body=rack.GetComponent<Rigidbody>();Undo.RecordObject(body,"Controlled parachute glide");body.isKinematic=true;body.useGravity=false;
                Undo.RecordObject(player.transform,"Face approaching supply route");player.transform.rotation=Quaternion.LookRotation(direction);PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                EditorUtility.SetDirty(drop);EditorUtility.SetDirty(body);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/parachute-flight-layout.txt",$"Overhead 2.5 s\nRelease {drop.releaseAfter:F2} s\nDescent {drop.descentSeconds} s\nLanding {drop.landingPosition}\nRelease position {drop.releasePosition}\nGround {ground.collider.name}");
            }
            finally{meshFolder=savedFolder;meshPrefix=savedPrefix;meshIndex=savedIndex;}
        }
        public static void VerifyParachuteDrop()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();var rack=drop.GetComponent<EquipmentRack>();var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();
            if(!drop.aircraft||!drop.parachute||!drop.GetComponent<Rigidbody>().isKinematic)throw new Exception("Missing authored delivery");
            drop.Advance(2.5f);if(Vector3.ProjectOnPlane(drop.aircraft.position-player.transform.position,Vector3.up).magnitude>.1f||drop.Released)throw new Exception("Flyover timing mismatch");
            drop.Advance(drop.releaseAfter-2.5f+.01f);if(!drop.Released||!drop.parachute.gameObject.activeSelf||drop.transform.parent)throw new Exception("Release failed");
            float previous=drop.transform.position.y;for(int i=0;i<99;i++){drop.Advance(.1f);if(drop.transform.position.y>previous+.01f)throw new Exception("Descent reversed");previous=drop.transform.position.y;}
            if(drop.Landed)throw new Exception("Landed before ten second glide");drop.Advance(.10f);
            if(!drop.Landed||Vector3.Distance(drop.transform.position,drop.landingPosition)>.01f||drop.transform.position.y>player.transform.position.y-15)throw new Exception("Not landed at foothill");
            drop.Advance(1.25f);if(!drop.parachute.gameObject.activeSelf)throw new Exception("Parachute vanished instead of fading");var r=drop.parachute.GetComponentInChildren<Renderer>();var mp=new MaterialPropertyBlock();r.GetPropertyBlock(mp);if(mp.GetColor("_BaseColor").a>=.9f)throw new Exception("No transparency fade");drop.Advance(1.3f);if(drop.parachute.gameObject.activeSelf)throw new Exception("Parachute did not disappear");
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();foreach(var s in rack.slots){if(!s.Packed||s.machine.gameObject.activeSelf)throw new Exception("Cargo unpacked early");if(!placement.Begin(s.machine))throw new Exception("Cannot retrieve "+s.name);placement.Cancel();}
            var planePreview=UnityEngine.Object.Instantiate(drop.aircraft.gameObject);planePreview.SetActive(true);planePreview.transform.localScale=Vector3.one*.11f;RenderPreview(planePreview,"Logs/supply-aircraft-preview.png");UnityEngine.Object.DestroyImmediate(planePreview);
            var preview=UnityEngine.Object.Instantiate(rack.gameObject);preview.GetComponent<EquipmentAirdrop>().enabled=false;preview.transform.localScale=Vector3.one*.24f;var canopy=preview.transform.Find("Deployable parachute and suspension lines");canopy.gameObject.SetActive(true);canopy.localScale=Vector3.one;foreach(var item in canopy.GetComponentsInChildren<Renderer>())item.SetPropertyBlock(null);RenderPreview(preview,"Logs/parachute-preview.png");UnityEngine.Object.DestroyImmediate(preview);
            Debug.Log("PARACHUTE_DROP_VERIFIED: 2.5s overhead, 10s glide, foothill landing, gradual fade, cargo retrieval");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}
