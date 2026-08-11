// ******------------------------------------------------------******
// BasicCameraControl.cs
// Really bad and basic camera movement
// meant to be used just for the demo purposes.
// Author:
//       K.Sinan Acar <ksa@puzzledwizard.com>
//
// Copyright (c) 2019 PuzzledWizard
// ******------------------------------------------------------******
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
namespace PW
{
    [RequireComponent(typeof(Camera))]
    public class BasicCameraControl : MonoBehaviour
    {

        [Range(0.2f, 6f)]
        public float rotateSpeed = 2f;

        public float scrollSmooth = 2f;

        private Camera controlledCamera;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            //Gets scroll wheel delta and zoom in out based on the cursor position
            float delta = mouse.scroll.ReadValue().y;

            if (Mathf.Abs(delta) > Mathf.Epsilon)
            {

                RaycastHit hit;
                Ray ray = controlledCamera.ScreenPointToRay(mouse.position.ReadValue());
                Vector3 desiredPosition;

                if (Physics.Raycast(ray, out hit))
                {
                    desiredPosition = hit.point;
                }
                else
                {
                    desiredPosition = transform.localPosition + transform.forward*5f;
                }


                float curDir = Vector3.Distance(desiredPosition, transform.localPosition);

                Vector3 direction = Vector3.Normalize(desiredPosition - transform.localPosition) * (delta);

                transform.localPosition += direction.normalized * scrollSmooth * Time.deltaTime;

            }
        }
        private void LateUpdate()
        {

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            //Gets right mouseButton rotates around the pivot

            Vector3 eulerRotation = transform.localRotation.eulerAngles;
            eulerRotation.z = 0f;

            if (mouse.rightButton.isPressed)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                float rot_x = mouseDelta.x;
                float rot_y = -mouseDelta.y;

                eulerRotation.x += rot_y * rotateSpeed;
                eulerRotation.y += rot_x * rotateSpeed;

            }

            transform.localRotation = Quaternion.Euler(eulerRotation);

        }


    }


}
