using UnityEditor;
using UnityEngine;

namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        [MenuItem("Mach/Refine Truck Interior")]
        public static void RefineTruckInterior()
        {
            var contents=PrefabUtility.LoadPrefabContents(TruckPrefab);
            try
            {
                contents.GetComponent<UtilityTruck>().steeringLockSeconds=.5f;
                BuildTruckInterior(contents);
                PrefabUtility.SaveAsPrefabAsset(contents,TruckPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);meshFolder=Folder+"/Meshes";meshPrefix="Part_";}
            AssetDatabase.SaveAssets();VerifyTruck();TabletAuthoring.VerifyAndBuild();
        }
        static void BuildTruckInterior(GameObject contents)
        {
            var old=contents.transform.Find("Interior detail assembly");if(old)Object.DestroyImmediate(old.gameObject);
            string previousFolder=meshFolder,previousPrefix=meshPrefix;int previousIndex=meshIndex;
            meshFolder=TruckFolder+"/Meshes";meshPrefix="Interior_";meshIndex=0;
            truckCream=LoadTruckMaterial("Ivory enamel");truckDark=LoadTruckMaterial("Graphite armor");truckRubber=LoadTruckMaterial("Tire rubber");truckMetal=LoadTruckMaterial("Brushed fittings");truckAmber=LoadTruckMaterial("Amber marker");truckRed=LoadTruckMaterial("Tail lamp");truckLight=LoadTruckMaterial("Headlamp lens");
            try
            {
                var interior=Anchor(contents.transform,"Interior detail assembly",Vector3.zero);
                TB("Dashboard face fascia",interior,new Vector3(.04f,1.51f,.48f),new Vector3(1.75f,.40f,.16f),truckCream,false,.045f);
                // All switches are authored visual details, editable before Play Mode.
                var center=Anchor(interior,"Center control stack",new Vector3(.22f,1.48f,.338f));
                TB("Center recessed surround",center,Vector3.zero,new Vector3(.47f,.45f,.055f),truckDark,false,.035f);
                TB("Radio metal bezel",center,new Vector3(0,.115f,-.037f),new Vector3(.385f,.135f,.022f),truckMetal,false,.013f);
                TB("Radio display",center,new Vector3(-.035f,.127f,-.052f),new Vector3(.236f,.068f,.012f),truckRubber,false,.007f);
                InteriorLabel(center,"Radio frequency","FM  104.8",new Vector3(-.035f,.128f,-.060f),.068f,truckAmber);
                Disc("Radio tuning dial",center,new Vector3(.143f,.126f,-.056f),.029f,.024f,12,truckDark);
                TB("Tuning dial index",center,new Vector3(.143f,.141f,-.073f),new Vector3(.005f,.012f,.006f),truckLight,false,.001f);
                for(int i=0;i<5;i++)TB("Radio preset "+(i+1),center,new Vector3(-.14f+i*.069f,.060f,-.055f),new Vector3(.045f,.022f,.015f),truckRubber,false,.004f);
                string[] legends={"LIGHT","FAN","AUX","HAZ"};
                for(int i=0;i<4;i++)
                {
                    float x=(i-1.5f)*.094f;
                    TB(legends[i]+" switch recess",center,new Vector3(x,-.029f,-.038f),new Vector3(.077f,.071f,.019f),truckRubber,false,.008f);
                    TB(legends[i]+" rocker button",center,new Vector3(x,-.031f,-.056f),new Vector3(.055f,.047f,.022f),i==3?truckRed:truckDark,false,.008f);
                    TB(legends[i]+" indicator",center,new Vector3(x,-.017f,-.071f),new Vector3(.023f,.006f,.004f),i==0?truckAmber:truckLight,false,.001f);
                    InteriorLabel(center,legends[i]+" legend",legends[i],new Vector3(x,-.083f,-.041f),.041f,truckLight);
                }
                for(int i=0;i<3;i++)
                {
                    float x=(i-1)*.12f;Disc("Climate dial bezel",center,new Vector3(x,-.156f,-.036f),.040f,.012f,12,truckMetal);
                    Disc("Climate dial",center,new Vector3(x,-.156f,-.050f),.032f,.021f,12,truckRubber);
                    TB("Climate dial index",center,new Vector3(x,-.139f,-.065f),new Vector3(.005f,.014f,.004f),truckLight,false,.001f);
                }
                foreach(float x in new[]{-.79f,.65f})
                {
                    TB("Vent recess",interior,new Vector3(x,1.68f,.34f),new Vector3(.24f,.12f,.038f),truckRubber,false,.014f);
                    for(int i=0;i<4;i++)TB("Vent louver",interior,new Vector3(x,1.64f+i*.025f,.314f),new Vector3(.205f,.01f,.018f),truckMetal,false,.003f);
                }
                TB("Passenger glovebox",interior,new Vector3(.66f,1.43f,.379f),new Vector3(.35f,.27f,.022f),truckDark,false,.025f);
                TB("Glovebox inset face",interior,new Vector3(.66f,1.43f,.36f),new Vector3(.315f,.235f,.016f),truckCream,false,.018f);
                TB("Glovebox latch",interior,new Vector3(.66f,1.49f,.344f),new Vector3(.09f,.023f,.02f),truckMetal,false,.005f);
                foreach(float x in new[]{-.68f,-.47f,-.26f})
                {
                    for(int i=0;i<9;i++){float a=(-120+i*30)*Mathf.Deg2Rad;var tick=TB("Instrument tick",interior,new Vector3(x+Mathf.Sin(a)*.055f,1.615f+Mathf.Cos(a)*.055f,.325f),new Vector3(.004f,.009f,.004f),truckLight,false,.001f);tick.transform.localRotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);}
                    Disc("Auxiliary instrument bezel",interior,new Vector3(x,1.615f,.348f),.068f,.02f,16,truckMetal);Disc("Auxiliary instrument face",interior,new Vector3(x,1.615f,.332f),.059f,.008f,16,truckRubber);
                    TruckBar("Auxiliary needle",interior,new Vector3(x-.018f,1.595f,.322f),new Vector3(x+.025f,1.646f,.322f),.005f,truckAmber);
                }
                InteriorLabel(interior,"Instrument legend","OIL       TEMP       FUEL",new Vector3(-.47f,1.51f,.326f),.057f,truckDark);
                // Lower console between the seats with short shifter and handbrake.
                TB("Center tunnel",interior,new Vector3(.02f,1.14f,-.11f),new Vector3(.27f,.22f,.76f),truckDark,false,.04f);
                TB("Gear lever boot",interior,new Vector3(.02f,1.265f,.04f),new Vector3(.17f,.07f,.18f),truckRubber,false,.026f);
                for(int i=0;i<3;i++)TB("Boot bellows",interior,new Vector3(.02f,1.28f+i*.016f,.04f),new Vector3(.14f-i*.025f,.009f,.15f-i*.025f),truckDark,false,.009f);
                TruckBar("Gear stick",interior,new Vector3(.02f,1.30f,.04f),new Vector3(.02f,1.47f,-.025f),.022f,truckMetal);
                TB("Gear knob",interior,new Vector3(.02f,1.48f,-.025f),new Vector3(.068f,.066f,.069f),truckRubber,false,.019f);
                TB("Gear knob inlay",interior,new Vector3(.02f,1.513f,-.025f),new Vector3(.035f,.003f,.036f),truckMetal,false,.008f);
                TB("Handbrake pivot",interior,new Vector3(.10f,1.29f,-.3f),new Vector3(.046f,.07f,.075f),truckMetal,false,.01f);
                TruckBar("Handbrake grip",interior,new Vector3(.10f,1.32f,-.30f),new Vector3(.10f,1.37f,-.13f),.033f,truckRubber);
                TB("Driver floor mat",interior,new Vector3(-.47f,1.126f,.12f),new Vector3(.62f,.014f,.59f),truckRubber,false,.018f);
                TB("Passenger floor mat",interior,new Vector3(.52f,1.126f,.12f),new Vector3(.60f,.014f,.59f),truckRubber,false,.018f);
                for(int i=0;i<3;i++){var pedal=TB("Pedal "+i,interior,new Vector3(-.65f+i*.14f,1.19f,.42f),new Vector3(.08f,.024f,.12f),truckRubber,false,.01f);pedal.transform.localRotation=Quaternion.Euler(-25,0,0);}
                foreach(float side in new[]{-1f,1f})
                {
                    var door=side<0?contents.GetComponent<UtilityTruck>().driverDoor:contents.GetComponent<UtilityTruck>().passengerDoor;
                    // Keep these under the existing animated door, but grouped for repeatable authoring.
                    var existing=door.transform.Find("Interior door details");if(existing)Object.DestroyImmediate(existing.gameObject);
                    var panel=Anchor(door.transform,"Interior door details",Vector3.zero);
                    TB("Door card",panel,new Vector3(-side*.056f,.35f,-.72f),new Vector3(.018f,.49f,1.10f),truckDark,false,.025f);
                    TB("Door storage pocket",panel,new Vector3(-side*.081f,.18f,-.75f),new Vector3(.06f,.11f,.63f),truckRubber,false,.018f);
                    TruckBar("Interior door release",panel,new Vector3(-side*.08f,.59f,-.24f),new Vector3(-side*.08f,.59f,-.37f),.023f,truckMetal);
                }
            }
            finally{meshFolder=previousFolder;meshPrefix=previousPrefix;meshIndex=previousIndex;}
        }
        static void InteriorLabel(Transform parent,string name,string value,Vector3 position,float size,Material material)
        {Text(name,parent,value,position,size,material);}
    }
}
