using UnityEngine;
namespace Mach
{
    public sealed class TruckDashboard : MonoBehaviour
    {
        public UtilityTruck truck;
        public Transform speedNeedle,rpmNeedle,fuelNeedle,handbrakePivot;
        public TextMesh speedReadout,rpmReadout,fuelReadout;
        public float maximumMph=120,maximumRpm=8000;
        float speed,rpm;
        int lastGear;
        float gearResponse;
        void OnEnable(){SetNeedle(fuelNeedle,1);}
        void LateUpdate()
        {
            if(!truck)return;
            float blend=1-Mathf.Exp(-8*Time.deltaTime);
            if(lastGear!=truck.CurrentGear){lastGear=truck.CurrentGear;gearResponse=.5f;}
            gearResponse=Mathf.Max(0,gearResponse-Time.deltaTime);
            speed=Mathf.Lerp(speed,truck.Speed*2.236936f,blend);
            rpm=Mathf.Lerp(rpm,truck.EngineRPM,1-Mathf.Exp(-(gearResponse>0?18:8)*Time.deltaTime));
            SetNeedle(speedNeedle,speed/maximumMph);SetNeedle(rpmNeedle,rpm/maximumRpm);SetNeedle(fuelNeedle,1);
            if(speedReadout)speedReadout.text=Mathf.RoundToInt(speed).ToString();
            if(rpmReadout)rpmReadout.text=Mathf.RoundToInt(rpm).ToString();
            if(fuelReadout)fuelReadout.text="F";
            if(handbrakePivot)handbrakePivot.localRotation=Quaternion.Slerp(handbrakePivot.localRotation,Quaternion.Euler(truck.parkingBrake?-28:0,0,0),blend);
        }
        static void SetNeedle(Transform needle,float value){if(needle)needle.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(120,-120,Mathf.Clamp01(value)));}
    }
}
