using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Mach
{
    [DefaultExecutionOrder(-20)]
    public sealed class CeaseFireRemote : MonoBehaviour
    {
        public BehaviorProgram program;public RectTransform panel,stopButton;public Text state;public bool IsRaised {get;private set;}
        public Camera uiCamera;
        [Min(.1f)] public float transitionSeconds=.5f;
        public Vector3 stowedOffset=new Vector3(.06f,-.55f,.12f);
        Vector3 raisedPosition,buttonPosition;Quaternion raisedRotation;
        float blend,velocity,pressTime;bool initialized;
        // Retain the component type and serialized references in the user's existing scene.
        void InitializePose(){if(initialized)return;raisedPosition=panel.localPosition;raisedRotation=panel.localRotation;buttonPosition=stopButton.localPosition;initialized=true;}
        public void PressEngage()
        {
            pressTime=.22f;
            var defense=FindAnyObjectByType<GhostDefense>();
            if(!defense){state.text="NO DEFENSE LINK";return;}
            if(defense.Won){state.text="VICTORY";return;}
            if(!defense.home||defense.home.Destroyed){state.text="BASE UNAVAILABLE";return;}
            if(!defense.Ready){state.text="PREPARING DEFENSE";return;}
            defense.BeginWave();state.text="ENGAGEMENT ACTIVE";
        }
        public void SetRaised(bool value)
        {
            InitializePose();IsRaised=value;
            if(value){var launcher=GetComponent<PointLauncher>();if(launcher)launcher.SetRaised(false);panel.gameObject.SetActive(true);program.tablet.SetRaised(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            else{Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(program.pickup.prompt)program.pickup.prompt.text="";
            AdvancePose(0);
        }
        public void AdvancePose(float dt)
        {
            InitializePose();blend=Mathf.SmoothDamp(blend,IsRaised?1:0,ref velocity,transitionSeconds*.32f,Mathf.Infinity,Mathf.Max(.00001f,dt));
            panel.localPosition=raisedPosition+stowedOffset*(1-blend)+Vector3.up*(Mathf.Sin(blend*Mathf.PI)*.045f);
            panel.localRotation=raisedRotation*Quaternion.Euler(22*(1-blend),8*(1-blend),-12*(1-blend));
            pressTime=Mathf.Max(0,pressTime-dt);stopButton.localPosition=buttonPosition+Vector3.forward*(Mathf.Sin(pressTime/.22f*Mathf.PI)*5);
            if(!IsRaised&&blend<.001f){blend=velocity=0;panel.gameObject.SetActive(false);}
        }
        void LateUpdate()=>AdvancePose(Mathf.Min(Time.unscaledDeltaTime,.05f));
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            var k=Keyboard.current;var m=Mouse.current;if(k==null||m==null)return;
            if(program.GetComponent<TruckDriver>().CurrentTruck){if(IsRaised)SetRaised(false);return;}
            if(program.pickup.placement&&program.pickup.placement.IsPlacing)return;
            if(k.rKey.wasPressedThisFrame)SetRaised(!IsRaised);if(IsRaised&&k.escapeKey.wasPressedThisFrame)SetRaised(false);
            if(IsRaised&&blend>.95f&&m.leftButton.wasPressedThisFrame&&RectTransformUtility.RectangleContainsScreenPoint(stopButton,m.position.ReadValue(),uiCamera))PressEngage();
        }
        void OnApplicationFocus(bool focus){if(!focus&&IsRaised)SetRaised(false);}
    }
}
