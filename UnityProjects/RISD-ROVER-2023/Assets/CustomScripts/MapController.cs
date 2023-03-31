using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UX;


namespace Microsoft.MixedReality.Toolkit
{
    public class MapController : MRTKBaseInteractable
    {
        enum MapFocusMode
        {
            MapNoFocus,
            MapCenterUser,
            MapAlignUser,
            NumMapFocusModes,
        };

        private RectTransform _mapRT;
        private BoxCollider _meshBC;

        // Zoom
        [SerializeField] private float _maxZoom = 2.0f;

        // Pan
        private Dictionary<IXRInteractor, Vector2> lastPositions = new Dictionary<IXRInteractor, Vector2>();
        private Vector2 firstPosition = new Vector2();
        private Vector2 initialOffsetMin = new Vector2();
        private Vector2 initialOffsetMax = new Vector2();

        // Focus
        private MapFocusMode _focusMode = MapFocusMode.MapNoFocus;
        private float _mapLastRotZDeg = 0.0f;

        void Start()
        {
            _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
            _meshBC = GameObject.Find("Map Panel").GetComponent<BoxCollider>();
        }

        void Update()
        {
            switch (_focusMode)
            {
                case MapFocusMode.MapCenterUser:
                    CenterMapAtUser();
                    break;
                case MapFocusMode.MapAlignUser:
                    AlignMapWithUser();
                    break;
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                foreach (var interactor in interactorsSelecting)
                {
                    if (interactor is Input.PokeInteractor)
                    {
                        // attachTransform will be the actual point of the touch interaction (e.g. index tip)
                        Vector2 localTouchPosition = transform.InverseTransformPoint(interactor.GetAttachTransform(this).position);

                        // Have we seen this interactor before? If not, last position = current position
                        if (!lastPositions.TryGetValue(interactor, out Vector2 lastPosition))
                        {
                            // Pan
                            firstPosition = localTouchPosition;
                            lastPosition = localTouchPosition;
                            initialOffsetMin = _mapRT.offsetMin;
                            initialOffsetMax = _mapRT.offsetMax;

                            // Focus
                            _focusMode = MapFocusMode.MapNoFocus;
                            Debug.Log(_focusMode);
                        }

                        // Update the offsets (top, right, bottom, left) based on the change in position
                        Vector2 delta = localTouchPosition - firstPosition;
                        _mapRT.offsetMin = initialOffsetMin + delta;
                        _mapRT.offsetMax = _mapRT.offsetMin;

                        // Write/update the last-position
                        if (lastPositions.ContainsKey(interactor))
                        {
                            lastPositions[interactor] = localTouchPosition;
                        }
                        else
                        {
                            lastPositions.Add(interactor, localTouchPosition);
                        }

                        break;
                    }
                }
            }
        }

        /************* Scale ***************/
        public void MapScaleCallback(SliderEventData args)
        {
            float scale = 1.0f + args.NewValue * _maxZoom;
            _mapRT.localScale = new Vector3(scale, scale, 1.0f);
        }

        /************* Focus **************/
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
                    // CenterMapAtUser();
                    break;
                case MapFocusMode.MapAlignUser:
                    MapRestoreLastRotation();
                    break;
            }
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
        /***************************/

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // Do something here (?)
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            // Remove the interactor from our last-position collection when it leaves.
            lastPositions.Remove(args.interactorObject);
        }

    }
}
