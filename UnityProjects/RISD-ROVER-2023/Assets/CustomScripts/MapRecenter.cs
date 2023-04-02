using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

enum MapFocusMode
{
    MapNoFocus,
    MapCenterUser,
    MapAlignUser,
    NumMapFocusModes,
}

namespace Microsoft.MixedReality.Toolkit
{
    public class MapRecenter : MRTKBaseInteractable
    {
        private RectTransform _mapRT;
        private BoxCollider _meshBC;

        private MapFocusMode _focusMode = MapFocusMode.MapNoFocus;
        private float _mapLastRotZDeg = 0.0f;

        private bool _poking = false;

        void Start()
        {
            _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
            _meshBC = GameObject.Find("Map Panel").GetComponent<BoxCollider>();
        }

        private void RotateMapWithUser()
        {
            Vector3 userLook = Camera.main.transform.forward;

            // Rotate map so that curloc points up
            userLook.y = 0.0f;
            float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
            _mapRT.localRotation = Quaternion.Euler(0.0f, 0.0f, lookAngleZDeg);
        }

        private void CenterMapAtUser()
        {
            // Convert userPos to mapRT offsets
            // Note: userPos.xz gives offsets in ROTATED MAP SPACE,
            //       but we must compute offsets in PANEL SPACE
            float scaleW2M = 100.0f * _mapRT.localScale.x;
            float mapRotZDeg = _mapRT.localEulerAngles.z;

            Vector3 userPos = Camera.main.transform.position;

            // User pos in map space, with rotation of map (xz components)
            Vector3 userPosMapspace = userPos * scaleW2M;
            // Rotate userPosMapspace back to get coords in unrotated map coords (xz components)
            Vector3 userPosMapspaceUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * userPosMapspace;

            _mapRT.offsetMin = -new Vector2(userPosMapspaceUnrot.x, userPosMapspaceUnrot.z);
            _mapRT.offsetMax = _mapRT.offsetMin;
        }

        // Callback: Recenter
        private void AlignMapWithUser()
        {
            RotateMapWithUser();
            CenterMapAtUser();
        }

        private void MapToggleFocusMode()
        {
            int newMode = ((int)_focusMode + 1) % (int)MapFocusMode.NumMapFocusModes;
            _focusMode = (MapFocusMode)newMode;
            Debug.Log(_focusMode);
        }

        private void MapStoreLastRotation()
        {
            _mapLastRotZDeg = _mapRT.localEulerAngles.z;
        }

        private void MapRestoreLastRotation()
        {
            _mapRT.localRotation = Quaternion.Euler(0, 0, _mapLastRotZDeg);
        }

        public void MapFocusCallback()
        {
            MapToggleFocusMode();
            switch (_focusMode)
            {
                case MapFocusMode.MapNoFocus:
                    MapRestoreLastRotation();
                    CenterMapAtUser();
                    break;
                case MapFocusMode.MapCenterUser:
                    CenterMapAtUser();
                    break;
                case MapFocusMode.MapAlignUser:
                    MapRestoreLastRotation();
                    break;
            }
        }

        void Update()
        {
            if (_focusMode == MapFocusMode.MapAlignUser)
            {
                AlignMapWithUser();
            }
        }

        // public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        // {
        //     if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        //     {
        //         foreach (var interactor in interactorsSelecting)
        //         {
        //             if (interactor is Input.PokeInteractor)
        //             {
        //                 Debug.Log("MapRecenter::ProcessInteractable");
        //                 if (!_poking)
        //                 {
        //                     _focusMode = MapFocusMode.MapNoFocus;
        //                     Debug.Log(_focusMode);
        //                     _poking = true;
        //                 }
        //                 break;
        //             }
        //         }
        //     }
        // }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log("MapRecenter::OnSelectEntered");
            base.OnSelectEntered(args);

            // Do something here (?)
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            // Remove the interactor from seen when it leaves.
            _poking = false;
        }


    }

}
