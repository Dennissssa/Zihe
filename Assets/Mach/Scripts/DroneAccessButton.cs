using UnityEngine;

namespace Mach
{
    public sealed class DroneAccessButton : MonoBehaviour
    {
        public BeaconDroneStation station;
        public Transform cap;
        Vector3 restingPosition;
        float press;

        void Awake(){if(cap)restingPosition=cap.localPosition;}

        public void Activate(PlayerPickup player)
        {
            if(!station)return;
            station.Begin(player);
            if(station.Controlling)press=1;
        }

        void Update()
        {
            press=Mathf.MoveTowards(press,0,Time.deltaTime*5);
            if(cap)cap.localPosition=restingPosition-Vector3.up*(press*.15f);
        }
    }
}
