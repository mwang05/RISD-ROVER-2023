using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;


namespace Microsoft.MixedReality.Toolkit
{
    public class MapPan : MRTKBaseInteractable
    {
        private RectTransform _mapRT;
        private BoxCollider _meshBC;
        private Dictionary<IXRInteractor, Vector2> lastPositions = new Dictionary<IXRInteractor, Vector2>();
        private Vector2 firstPosition = new Vector2();
        private Vector2 initialOffsetMin = new Vector2();
        private Vector2 initialOffsetMax = new Vector2();

        void Start()
        {
            _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
            _meshBC = GameObject.Find("Map Panel").GetComponent<BoxCollider>();
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
                            firstPosition = localTouchPosition;
                            lastPosition = localTouchPosition;
                            initialOffsetMin = _mapRT.offsetMin;
                            initialOffsetMax = _mapRT.offsetMax;
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
