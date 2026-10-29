using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace NorthBlockout
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BlockoutWalker : MonoBehaviour
    {
        public Camera view;
        public Renderer[] body;
        public Transform[] fixedViews;
        public BlockoutCutaway[] cutaways;
        public bool fixedMode;
        public float walkSpeed = 2.8f;
        float yaw = 180, pitch, fall;
        CharacterController controller;
        Vector3 heldForward, heldRight;
        int activeShot = -1;
        readonly List<Renderer> obscuring = new();
        public int Shot => activeShot;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            Application.runInBackground = true;
            SetCursor(true);
        }

        void SetCursor(bool capture)
        {
            Cursor.lockState = capture && !fixedMode ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        public void Teleport(Vector3 point)
        {
            var cc = GetComponent<CharacterController>();
            cc.enabled = false;
            transform.position = point;
            cc.enabled = true;
            fall = 0;
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.tabKey.wasPressedThisFrame) { fixedMode = !fixedMode; activeShot = -1; SetCursor(true); }
            if (k.escapeKey.wasPressedThisFrame) SetCursor(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetCursor(true);
            if (k.rKey.wasPressedThisFrame) Teleport(new Vector3(5.3f, 3.06f, 15));
            var axis = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            axis = Vector2.ClampMagnitude(axis, 1);
            if (!fixedMode && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
            {
                Vector2 mouse = Mouse.current.delta.ReadValue();
                yaw += mouse.x * .12f;
                pitch = Mathf.Clamp(pitch - mouse.y * .12f, -75, 75);
            }
            Vector3 forward, right;
            if (fixedMode)
            {
                // Keep the movement basis until keys are released: camera cuts do not reverse held input.
                if (axis.sqrMagnitude == 0 || heldForward == Vector3.zero)
                {
                    heldForward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
                    heldRight = Vector3.Cross(Vector3.up, heldForward);
                }
                forward = heldForward; right = heldRight;
            }
            else { forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward; right = Quaternion.Euler(0, yaw, 0) * Vector3.right; }
            Vector3 direction = forward * axis.y + right * axis.x;
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(direction);
            if (controller.isGrounded && fall < 0) fall = -2;
            fall = Mathf.Max(fall - 22 * Time.deltaTime, -30);
            controller.Move((direction * walkSpeed * (k.leftShiftKey.isPressed ? 1.55f : 1) + Vector3.up * fall) * Time.deltaTime);
            if (transform.position.y < -8) Teleport(new Vector3(5.3f, 3.06f, 15));
        }

        public void RefreshView()
        {
            foreach(var r in obscuring) if(r) r.enabled=true;
            obscuring.Clear();
            var p = transform.position;
            bool inApartment=p.x>3 && p.x<15.2f && p.z>11.8f && p.z<20.2f && p.y>2.5f && p.y<6;
            bool home = p.z > 11.5f && p.z < 24.5f && Mathf.Abs(p.x) < 15.5f;
            bool shop = p.x > -21.5f && p.x < -6.5f && p.z < -11.8f && p.z > -23;
            bool bakery = p.x > 7.5f && p.x < 24.5f && p.z < -11.8f && p.z > -25;
            int shot;
            if (home && p.x > 3 && p.y > 2.5f) shot = p.z > 17.2f || p.x > 10.6f ? 1 : (p.x > 7 ? 2 : 0);
            else if (home) shot = p.y > 1.2f ? 3 : 4;
            else if (shop) shot = 7;
            else if (bakery) shot = 8;
            else shot = p.z > 2 ? 5 : 6;
            activeShot = shot;
            foreach (var r in body) if (r) r.enabled = fixedMode;
            foreach (var c in cutaways)
            {
                bool inside = c.area.Contains(p);
                foreach (var r in c.roof) if (r) r.enabled = !(inside && fixedMode);
                foreach (var r in c.nearWalls) if (r) r.enabled = !(inside && fixedMode && !inApartment);
            }
            view.orthographic = fixedMode;
            if (fixedMode)
            {
                Transform t = fixedViews[shot];
                view.transform.SetPositionAndRotation(t.position, t.rotation);
                view.orthographicSize = shot <= 4 ? 6 : shot >= 7 ? 9 : 20;
                view.nearClipPlane = .08f;
                // The actor must remain readable behind corridor walls in every fixed shot.
                for(int sample=0;sample<(inApartment?0:2);sample++)
                {
                    Vector3 delta=p+Vector3.up*(sample==0?.8f:1.55f)-view.transform.position;
                    foreach(var hit in Physics.SphereCastAll(view.transform.position,.18f,delta.normalized,Mathf.Max(0,delta.magnitude-.4f)))
                    {
                        if(hit.collider.transform.IsChildOf(transform))continue;
                        var r=hit.collider.GetComponent<Renderer>();
                        if(r && r.enabled) { r.enabled=false;obscuring.Add(r); }
                    }
                }
            }
            else view.transform.SetPositionAndRotation(p + Vector3.up * 1.64f, Quaternion.Euler(pitch, yaw, 0));
        }
        void LateUpdate() => RefreshView();
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    [System.Serializable]
    public sealed class BlockoutCutaway
    {
        public Bounds area;
        public Renderer[] roof;
        public Renderer[] nearWalls;
    }
}
