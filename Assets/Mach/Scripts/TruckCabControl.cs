using UnityEngine;
namespace Mach
{
    public enum TruckCabAction { Steering, Handbrake, Lights, Tailgate }
    public sealed class TruckCabControl : MonoBehaviour
    {
        public UtilityTruck truck;
        public TruckCabAction action;
        public Renderer stateRenderer;
        public Color activeColor=new Color(.16f,.7f,.38f),inactiveColor=new Color(.12f,.15f,.17f);
        MaterialPropertyBlock stateProperties;
        public void RefreshStateColor()
        {
            if(!truck||!stateRenderer||(action!=TruckCabAction.Lights&&action!=TruckCabAction.Tailgate))return;
            var lighting=truck.GetComponent<TruckLighting>();
            bool active=action==TruckCabAction.Lights?lighting&&lighting.headlightsOn:truck.tailgate&&truck.tailgate.IsOpen;
            if(stateProperties==null)stateProperties=new MaterialPropertyBlock();
            stateRenderer.GetPropertyBlock(stateProperties);
            stateProperties.SetColor("_BaseColor",active?activeColor:inactiveColor);
            stateRenderer.SetPropertyBlock(stateProperties);
        }
        void LateUpdate()=>RefreshStateColor();
        public bool CanUse(TruckDriver occupant)=>occupant&&occupant.CurrentTruck==truck&&occupant.CurrentSeat==0;
        public string Prompt(TruckDriver occupant)
        {
            switch(action)
            {
                case TruckCabAction.Steering:return "";
                case TruckCabAction.Handbrake:return truck.parkingBrake?"[LMB] Release handbrake":"[LMB] Apply handbrake";
                case TruckCabAction.Lights:return "[LMB] Toggle headlights";
                default:return "[LMB] Toggle tailgate";
            }
        }
        public void Activate(TruckDriver occupant)
        {
            if(!CanUse(occupant))return;
            switch(action)
            {
                case TruckCabAction.Steering:break;
                case TruckCabAction.Handbrake:
                    truck.parkingBrake=!truck.parkingBrake;
                    GameAudio.Play(truck.parkingBrake?SoundCue.DoorClose:SoundCue.DoorOpen,transform.position);
                    break;
                case TruckCabAction.Lights:var lights=truck.GetComponent<TruckLighting>();if(lights)lights.ToggleHeadlights();break;
                case TruckCabAction.Tailgate:if(truck.tailgate)truck.tailgate.Toggle();break;
            }
        }
    }
}
