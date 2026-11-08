using UnityEngine;
namespace Mach
{
    public sealed class RobotTrackModule : MonoBehaviour
    {
        public Transform[] links;
        public Transform[] wheels;
        public float axleHalfLength=.72f,loopRadius=.36f,loopHeight=.41f;
        [HideInInspector] public float beltTravel;
        public float LoopLength=>4*axleHalfLength+2*Mathf.PI*loopRadius;
        public void Advance(float distance)
        {
            beltTravel=Mathf.Repeat(beltTravel+distance,LoopLength);
            for(int i=0;i<links.Length;i++)
            {
                Sample(i*LoopLength/links.Length+beltTravel,out var position,out var tangent);
                links[i].localPosition=position;links[i].localRotation=Quaternion.LookRotation(tangent,Vector3.Cross(tangent,Vector3.right));
            }
            foreach(var wheel in wheels)wheel.Rotate(Vector3.right,distance/loopRadius*Mathf.Rad2Deg,Space.Self);
        }
        public void Sample(float distance,out Vector3 position,out Vector3 tangent)
        {
            float s=Mathf.Repeat(distance,LoopLength),straight=2*axleHalfLength,arc=Mathf.PI*loopRadius;
            if(s<straight){position=new Vector3(0,loopHeight+loopRadius,-axleHalfLength+s);tangent=Vector3.forward;}
            else if((s-=straight)<arc){float a=s/loopRadius;position=new Vector3(0,loopHeight+Mathf.Cos(a)*loopRadius,axleHalfLength+Mathf.Sin(a)*loopRadius);tangent=new Vector3(0,-Mathf.Sin(a),Mathf.Cos(a));}
            else if((s-=arc)<straight){position=new Vector3(0,loopHeight-loopRadius,axleHalfLength-s);tangent=Vector3.back;}
            else{float a=(s-straight)/loopRadius;position=new Vector3(0,loopHeight-Mathf.Cos(a)*loopRadius,-axleHalfLength-Mathf.Sin(a)*loopRadius);tangent=new Vector3(0,Mathf.Sin(a),-Mathf.Cos(a));}
        }
    }
}
