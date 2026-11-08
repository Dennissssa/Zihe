using UnityEngine;
namespace Mach
{
    public sealed class TruckInteractable : MonoBehaviour
    {
        public UtilityTruck truck;
        public TruckHingedPanel panel;
        public bool driverEntry;
        public int seatIndex=-1;
        // Older driver doors also carry driverEntry. A hinged panel is never a seat.
        public bool IsSeat=>!(panel&&panel.transform==transform)&&(seatIndex>=0||driverEntry);
        public int SeatIndex=>seatIndex>=0?seatIndex:0;
        public string panelName="Door";
        public string Prompt=>IsSeat?(truck.Speed>.8f?"Wait for vehicle to stop":panel&&!panel.IsOpen?"Open door before entering":SeatIndex==0?"[E] Driver seat":"[E] Passenger seat"):(panel&&panel.IsOpen?"[LMB] Close ":"[LMB] Open ")+panelName;
        public void Activate(){if(!IsSeat&&truck&&panel)panel.Toggle();}
    }
}
