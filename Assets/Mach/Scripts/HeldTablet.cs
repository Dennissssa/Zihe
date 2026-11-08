using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mach
{
    // Animates an already-authored camera child. Never creates viewmodel objects.
    [DefaultExecutionOrder(-10)]
    public sealed class HeldTablet : MonoBehaviour
    {
        public Camera tabletCamera;
        public Transform poseRoot;
        public Graphic crosshair;
        public MachinePlacement placement;
        [Header("Resting pose: normalized screen anchor")]
        public Vector2 restingAnchor = new Vector2(.225f, .045f);
        public float restingDistance = .78f;
        public Vector3 restingAngles = new Vector3(8,-10,-8);
        [Header("Reading pose")]
        public Vector2 readingAnchor = new Vector2(.5f,.51f);
        public float readingDistance = .48f;
        public Vector3 readingAngles = new Vector3(0,0,0);
        [Header("Natural lift: two intermediate poses")]
        public Vector3 liftOffset = new Vector3(.035f,.12f,.035f);
        public Vector3 gatherOffset = new Vector3(-.055f,-.075f,.045f);
        public Vector3 liftAngles = new Vector3(17,-14,-12);
        public Vector3 gatherAngles = new Vector3(-4,5,3);
        public Transform forearmPivot;
        [Min(.05f)] public float transitionSeconds = .52f;
        [Range(0,1)] public float secondaryMotion = .65f;
        public bool IsRaised { get; private set; }
        public bool BlocksWorldInteraction => IsRaised || blend > .05f;
        float blend, blendVelocity, motionTime, walkWeight;
        Vector2 lookSway;
        CharacterController walker;
        void Awake() => walker=GetComponent<CharacterController>();

        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            HandleInput(Keyboard.current,Cursor.lockState==CursorLockMode.Locked || IsRaised);
        }
        public void HandleInput(Keyboard keyboard,bool inputEnabled)
        {
            if(keyboard==null)return;
            var program=GetComponent<BehaviorProgram>();if(program&&program.remote&&program.remote.IsRaised)return;
            if(keyboard.escapeKey.wasPressedThisFrame){SetRaised(false);return;}
            if(placement && placement.IsPlacing)return;
            if(inputEnabled && keyboard.tabKey.wasPressedThisFrame)SetRaised(!IsRaised);
        }
        public void SetRaised(bool value) { bool changed=IsRaised!=value;IsRaised=value;if(changed&&GetComponent<BehaviorProgram>()){Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;} }
        void LateUpdate()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            motionTime+=dt;
            var mouse=Mouse.current;
            Vector2 desired=Cursor.lockState==CursorLockMode.Locked && mouse!=null ? Vector2.ClampMagnitude(PlayerLookInput.Read()*.018f,1) : Vector2.zero;
            lookSway=Vector2.Lerp(lookSway,desired,1-Mathf.Exp(-8*dt));
            float moving=walker ? Mathf.Clamp01(new Vector2(walker.velocity.x,walker.velocity.z).magnitude/4) : 0;
            walkWeight=Mathf.Lerp(walkWeight,moving,1-Mathf.Exp(-6*dt));
            AdvancePose(dt);
        }
        public void AdvancePose(float deltaTime)
        {
            // Keep velocity when reversing mid-lift; never restart from an endpoint.
            blend=Mathf.SmoothDamp(blend,IsRaised?1:0,ref blendVelocity,Mathf.Max(.05f,transitionSeconds)*.32f,Mathf.Infinity,Mathf.Max(0,deltaTime));
            if(Mathf.Abs(blend-(IsRaised?1:0))<.0002f && Mathf.Abs(blendVelocity)<.003f){blend=IsRaised?1:0;blendVelocity=0;}
            ApplyPose(blend);
            if(crosshair)crosshair.enabled=!BlocksWorldInteraction;
        }
        public void SnapToRest()
        {
            IsRaised=false;blend=0;blendVelocity=0;lookSway=Vector2.zero;walkWeight=0;motionTime=0;ApplyPose(0);
            if(crosshair)crosshair.enabled=true;
        }
        void ApplyPose(float amount)
        {
            if(!poseRoot || !tabletCamera)return;
            float aspect=Mathf.Max(.5f,tabletCamera.aspect);
            // Keep the tablet framed on narrow windows without enlarging it on ultrawide screens.
            float framing=Mathf.Min(1,aspect/(16f/9f));
            poseRoot.localScale=Vector3.one*framing;
            Vector3 rest=Position(restingAnchor,restingDistance,aspect);
            Vector3 reading=Position(readingAnchor,readingDistance,aspect);
            float t=amount,u=1-t;
            poseRoot.localPosition=u*u*u*rest+3*u*u*t*(rest+liftOffset*framing)+3*u*t*t*(reading+gatherOffset*framing)+t*t*t*reading;
            Quaternion a=Quaternion.Euler(restingAngles),b=Quaternion.Euler(liftAngles),c=Quaternion.Euler(gatherAngles),d=Quaternion.Euler(readingAngles);
            Quaternion ab=Quaternion.Slerp(a,b,t),bc=Quaternion.Slerp(b,c,t),cd=Quaternion.Slerp(c,d,t);
            poseRoot.localRotation=Quaternion.Slerp(Quaternion.Slerp(ab,bc,t),Quaternion.Slerp(bc,cd,t),t);
            if(Application.isPlaying)
            {
                float quiet=Mathf.Lerp(1,.3f,t)*secondaryMotion;
                float breath=Mathf.Sin(motionTime*1.65f)*.0011f;
                float stride=Mathf.Sin(motionTime*8)*.0022f*walkWeight;
                poseRoot.localPosition+=new Vector3(-lookSway.x*.002f,breath+stride-lookSway.y*.0015f,0)*quiet*framing;
                poseRoot.localRotation*=Quaternion.Euler(new Vector3(lookSway.y*.65f,lookSway.x*.65f,Mathf.Sin(motionTime*4)*walkWeight*.35f)*quiet)*Quaternion.Euler(0,0,Mathf.Sin(motionTime*1.65f)*.12f*quiet);
            }
            if(forearmPivot)
                forearmPivot.localRotation=Quaternion.Euler(Vector3.Lerp(new Vector3(5,2,-5),new Vector3(-3,-3,3),t)+new Vector3(-5,2,3)*Mathf.Sin(t*Mathf.PI));
        }
        Vector3 Position(Vector2 anchor,float distance,float aspect)
        {
            float height=2*distance*Mathf.Tan(tabletCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            return new Vector3((anchor.x-.5f)*height*aspect,(anchor.y-.5f)*height,distance);
        }
        void OnApplicationFocus(bool focused){if(!focused)SetRaised(false);}
        void OnDisable(){if(crosshair)crosshair.enabled=true;}
    }
}
