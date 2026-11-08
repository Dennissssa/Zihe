using UnityEngine;
using UnityEngine.InputSystem;

namespace Mach
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonWalker : MonoBehaviour
    {
        public Transform view;
        [Min(0)] public float walkSpeed = 4f;
        [Min(0)] public float sprintSpeed = 7f;
        [Min(0)] public float jumpHeight = 1.2f;
        [Min(0)] public float mouseSensitivity = 0.1f;
        public float gravity = -20f;
        CharacterController controller;
        float pitch, verticalSpeed;
        Vector3 externalImpulse;
        public void ApplyExternalImpulse(Vector3 velocity){externalImpulse=velocity;}
        void LateUpdate(){
            if(!controller||!controller.enabled)return;
            if(externalImpulse.sqrMagnitude>.001f){
                controller.Move(externalImpulse*Time.deltaTime);
                externalImpulse=Vector3.MoveTowards(externalImpulse,Vector3.zero,24f*Time.deltaTime);
            }
        }
        public static int CursorCapturedFrame { get; private set; }=-1;

        void Awake() => controller = GetComponent<CharacterController>();
        public void ResetMotionAndLook(){pitch=0;verticalSpeed=0;if(view)view.localRotation=Quaternion.identity;}
        void Start() => SetCursor(true);

        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            var circuit=GetComponent<BehaviorProgram>();if(circuit && (circuit.Editing || (circuit.remote && circuit.remote.IsRaised))){PlayerLookInput.Read();PlayerLookInput.Discard();return;}
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || view == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) SetCursor(false);
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                PlayerLookInput.Read();PlayerLookInput.Discard();
                if (mouse.leftButton.wasPressedThisFrame) SetCursor(true);
                return;
            }

            Vector2 look = GamePreferences.Look(PlayerLookInput.Read(),mouseSensitivity);
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -85, 85);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 move = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            move = Vector2.ClampMagnitude(move, 1);
            float speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? sprintSpeed : walkSpeed;
            if (controller.isGrounded)
            {
                if (verticalSpeed < 0) verticalSpeed = -2f;
                if (keyboard.spaceKey.wasPressedThisFrame && gravity < 0)
                    verticalSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
            verticalSpeed += gravity * Time.deltaTime;
            CollisionFlags collisions = controller.Move((transform.right * move.x * speed + transform.forward * move.y * speed
                + Vector3.up * verticalSpeed) * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        }

        void OnApplicationFocus(bool focused) { if (!focused) SetCursor(false); }
        void OnDisable() => SetCursor(false);
        static void SetCursor(bool locked)
        {
            PlayerLookInput.Discard();
            if(locked && Cursor.lockState!=CursorLockMode.Locked)CursorCapturedFrame=Time.frameCount;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
