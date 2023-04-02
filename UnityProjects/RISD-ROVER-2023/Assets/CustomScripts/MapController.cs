using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UX;
using Microsoft.MixedReality.Toolkit;
using System;
using UnityEngine.UI;
using RectTransform = UnityEngine.RectTransform;

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
    private Camera _mainCamera;

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

    // Marker
    enum MarkerType
    {
        Waypoint,
        Obstacle,
        Marker,
        Rover
    };
    private Vector2 lastTouchPosition;
    private bool _editMarkerMode = false;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private GameObject compassMarkerPrefab;
    [SerializeField] private float markerEditSensitivity = 0.033f;
    private Dictionary<GameObject, (Vector3, GameObject, GameObject, RectTransform, RectTransform)> _markers;
    private RectTransform _compassRT, _compassMarkersRT;
    private GameObject _newMarkerOnMap, _newMarkerOnCompass;
    private Transform _markersTF;
    private float _panelXBound, _panelYBound;
    private GameObject _markersObj;
    private float _buttonPressedTime;
    private MarkerType _selectedMarkerType;
    private FontIconSelector _waypointIcon, _obstacleIcon, _markerIcon, _roverIcon;

    void Start()
    {
        _mainCamera = Camera.main;
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _meshBC = GameObject.Find("Map Panel").GetComponent<BoxCollider>();
        _markers = new Dictionary<GameObject, (Vector3, GameObject, GameObject, RectTransform, RectTransform)>();
        _compassRT = GameObject.Find("Compass Image").GetComponent<RectTransform>();
        _compassMarkersRT = GameObject.Find("Compass Markers").GetComponent<RectTransform>();
        _markersObj = GameObject.Find("Markers");
        _markersTF = _markersObj.GetComponent<Transform>();
        var panelSize = GameObject.Find("Map Panel").GetComponent<BoxCollider>().size;
        _panelXBound = panelSize.x / 2;
        _panelYBound = panelSize.y / 2;
        _waypointIcon = GameObject.Find("WaypointIcon").GetComponent<FontIconSelector>();
        _obstacleIcon = GameObject.Find("ObstacleIcon").GetComponent<FontIconSelector>();
        _markerIcon = GameObject.Find("MarkerIcon").GetComponent<FontIconSelector>();
        _roverIcon = GameObject.Find("RoverIcon").GetComponent<FontIconSelector>();
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
        if (_markers.Count > 0) UpdateMarkers();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            foreach (var interactor in interactorsSelecting)
            {
                if (interactor is PokeInteractor)
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

                        // Marker
                        if (_editMarkerMode)
                        {
                            _newMarkerOnMap = Instantiate(markerPrefab, _markersTF);
                            _newMarkerOnCompass = Instantiate(compassMarkerPrefab, _compassMarkersRT);
                            _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                            _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                            _markers.Add(_newMarkerOnMap,
                                (MapToWorldPos(firstPosition),
                                    _newMarkerOnMap,
                                    _newMarkerOnCompass,
                                    _newMarkerOnMap.GetComponent<RectTransform>(),
                                    _newMarkerOnCompass.GetComponent<RectTransform>()
                                    ));
                        }
                        else
                        {
                            float minDist = markerEditSensitivity + 1;
                            foreach (var kvp in _markers)
                            {
                                float dist = (kvp.Value.Item1 - MapToWorldPos(firstPosition)).magnitude;
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    _newMarkerOnMap = kvp.Key;
                                    _newMarkerOnCompass = kvp.Value.Item3;
                                }
                            }

                            if (minDist < markerEditSensitivity)
                            {
                                _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                                _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                                _editMarkerMode = true;
                            }
                        }
                    }

                    // Update the offsets (top, right, bottom, left) based on the change in position
                    Vector2 delta = localTouchPosition - firstPosition;

                    if (_editMarkerMode)
                    {
                        var markerItem = _markers[_newMarkerOnMap];
                        markerItem.Item1 = MapToWorldPos(localTouchPosition);
                        _markers[_newMarkerOnMap] = markerItem;
                    }
                    else
                    {
                        _mapRT.offsetMin = initialOffsetMin + delta;
                        _mapRT.offsetMax = _mapRT.offsetMin;
                    }

                    // Write/update the last-position
                    if (lastPositions.ContainsKey(interactor))
                    {
                        lastPositions[interactor] = localTouchPosition;
                    }
                    else
                    {
                        lastPositions.Add(interactor, localTouchPosition);
                    }

                    lastTouchPosition = localTouchPosition;

                    break;
                }
            }
        }
    }

    /************* Scale ***************/
    public void MapScaleCallback(SliderEventData args)
    {
        if (_mapRT == null) return;
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
        Vector3 userLook = _mainCamera.transform.forward;

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

        Vector3 userPos = _mainCamera.transform.position;

        // User pos in map space, with rotation of map (xz components)
        Vector3 userMapPos = userPos * scaleW2M;
        // Rotate userMapPos back to get coords in un-rotated map coords (xz components)
        Vector3 userMapPosUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * userMapPos;

        _mapRT.offsetMin = -new Vector2(userMapPosUnrot.x, userMapPosUnrot.z);
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

        if (_editMarkerMode)
        {
            // Discard marker if being "thrown out" of the map
            if (Math.Abs(lastTouchPosition.x) > _panelXBound ||
                Math.Abs(lastTouchPosition.y) > _panelYBound)
            {
                _markers.Remove(_newMarkerOnMap);
                Destroy(_newMarkerOnMap);
                Destroy(_newMarkerOnCompass);
            }
            else
            {
                _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
                _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
            }

            _editMarkerMode = false;
        }

        // Remove the interactor from our last-position collection when it leaves.
        lastPositions.Remove(args.interactorObject);
    }

    /************* Marker **************/
    public void OnWaypointSelectEnter()
    {
        _selectedMarkerType = MarkerType.Waypoint;
        _buttonPressedTime = Time.time;
    }
    public void OnObstacleSelectEnter()
    {
        _selectedMarkerType = MarkerType.Obstacle;
        _buttonPressedTime = Time.time;
    }
    public void OnMarkerSelectEnter()
    {
        _selectedMarkerType = MarkerType.Marker;
        _buttonPressedTime = Time.time;
    }
    public void OnRoverSelectEnter()
    {
        _selectedMarkerType = MarkerType.Rover;
        _buttonPressedTime = Time.time;
    }

    public void OnMarkerButtonSelectExit()
    {
        float delta = Time.time - _buttonPressedTime;
        if (delta > 1f)
        {
            _editMarkerMode = true;
        }
        else
        {
            switch (_selectedMarkerType)
            {
                case MarkerType.Waypoint:
                    _waypointIcon.CurrentIconName =
                        _waypointIcon.CurrentIconName == "Icon 30" ? "Icon 10" : "Icon 30";
                    break;
                case MarkerType.Obstacle:
                    _obstacleIcon.CurrentIconName =
                        _obstacleIcon.CurrentIconName == "Icon 15" ? "Icon 10" : "Icon 15";
                    break;
                case MarkerType.Marker:
                    _markerIcon.CurrentIconName =
                        _markerIcon.CurrentIconName == "Icon 14" ? "Icon 10" : "Icon 14";
                    break;
                case MarkerType.Rover:
                    _roverIcon.CurrentIconName =
                        _roverIcon.CurrentIconName == "Icon 54" ? "Icon 10" : "Icon 54";
                    break;
            }
            _markersObj.SetActive(!_markersObj.activeSelf);
        }
    }

    private Vector3 MapToWorldPos(Vector2 mapPos)
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;
        Vector2 mapOffset = _mapRT.offsetMin;
        Vector3 worldPos = new Vector3(mapPos.x - mapOffset.x, 0, mapPos.y - mapOffset.y);

        // Un-rotate then scale to obtain the world space position
        worldPos = Quaternion.Euler(0.0f, mapRotZDeg, 0.0f) * worldPos;
        worldPos /= scaleW2M;

        return worldPos;
    }

    private Vector2 WorldToMapPos(Vector3 worldPos)
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;

        // Rotate then scale to obtain the map space position
        Vector3 mapPos = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * worldPos;
        mapPos *= scaleW2M;

        return new Vector2(mapPos.x, mapPos.z);
    }

    private void UpdateMarkers()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;
        float compassWidth = _compassRT.rect.width / 360.0f;

        Transform cameraTf = _mainCamera.transform;
        Vector3 userPos = cameraTf.position;
        Vector3 userLook = cameraTf.forward;
        userLook.y = 0.0f;

        foreach(var kvp in _markers)
        {
            Vector3 worldPos = kvp.Value.Item1;    // mark pos in world space
            RectTransform rtMap = kvp.Value.Item4;
            RectTransform rtCompass = kvp.Value.Item5;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            // Marker pos in map space, with rotation of map (xz components)
            Vector3 mapPos = worldPos * scaleW2M;
            // Rotate mapPos back to get coords in unrotated map coords (xz components)
            Vector3 mapPosUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * mapPos;

            rtMap.offsetMin = _mapRT.offsetMin + new Vector2(mapPosUnrot.x, mapPosUnrot.z);
            rtMap.offsetMax = rtMap.offsetMin;

            // Adjust marker position on compass
            // 1. Get relative angle of marker from front in range -180 ~ 180
            Vector3 markerDir = worldPos - userPos;
            markerDir.y = 0.0f;
            float angleToMarker = -Vector3.SignedAngle(markerDir, userLook, Vector3.up);
            rtCompass.offsetMin = new Vector2(angleToMarker * compassWidth, 0.0f);
            rtCompass.offsetMax = rtCompass.offsetMin;
        }
    }
}
