using UnityEngine;
namespace Mach
{
    public sealed class TruckLighting : MonoBehaviour
    {
        public UtilityTruck truck;
        public Renderer[] tailLenses;
        public Light[] tailLights;
        public Renderer[] headLenses;
        public Light[] headLights;
        public bool headlightsOn=true;
        public Color headColor=new Color(1,.88f,.65f);
        public float headEmission=11;
        public void ToggleHeadlights(){headlightsOn=!headlightsOn;UpdateLights(true);}
        public Color tailColor=new Color(1,.003f,.001f);
        public float tailEmission=2.5f,brakeEmission=10f;
        public bool Braking {get;private set;}
        MaterialPropertyBlock properties;
        void OnEnable(){properties=new MaterialPropertyBlock();UpdateLights(true);}
        void LateUpdate()=>UpdateLights(false);
        public void UpdateLights(bool force)
        {
            bool braking=truck&&truck.ServiceBrakeApplied;if(!force&&braking==Braking)return;Braking=braking;
            if(properties==null)properties=new MaterialPropertyBlock();
            if(headLenses!=null)foreach(var lens in headLenses)if(lens){lens.GetPropertyBlock(properties);properties.SetColor("_EmissionColor",headlightsOn?headColor*headEmission:Color.black);lens.SetPropertyBlock(properties);}
            if(headLights!=null)foreach(var lamp in headLights)if(lamp)lamp.enabled=headlightsOn;
            if(tailLenses!=null)foreach(var lens in tailLenses)if(lens){lens.GetPropertyBlock(properties);properties.SetColor("_EmissionColor",tailColor*(braking?brakeEmission:headlightsOn?tailEmission:0));lens.SetPropertyBlock(properties);}
            if(tailLights!=null)foreach(var lamp in tailLights)if(lamp)lamp.intensity=braking?1.1f:headlightsOn?.25f:0;
        }
    }
}
