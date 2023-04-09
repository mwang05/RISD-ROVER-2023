using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UX;
using Microsoft.MixedReality.Toolkit;
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

    private RectTransform _mapRT, _canvasRT;
    private BoxCollider _meshBC;
    private Camera _mainCamera;

    // Satellite info
    // hard coded center
    private const float satCenterLatitude  = 29.564575f;   // latitude at the center of the satellite image, in degree
    private const float satCenterLongitude = -95.081164f;  // longitude at the center of the satellite image, in degree
    // hard coded scale
    private const float satLatitudeRange = 0.002216f;  // the satellite image covers this much latitudes in degree
    private const float satLongitudeRange = 0.00255f;  // the satellite image covers this much longitudes in degree


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
    private Vector2 _lastTouchPosition;
    private bool _editMarkerMode = false;

    enum MapActionMode
    {
        Pan,
        AddMarker,
        SelectMarker,
        EditMarker
    }
    private Vector2 lastTouchPosition;

    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private GameObject compassMarkerPrefab;
    [SerializeField] private float markerEditSensitivity = 0.033f;

    // Each marker is a (gpsCoords, mapMarkerObj, compassMarkerObj, mapRT, compassRT) 5-tuple
    private Dictionary<GameObject, (Vector2, GameObject, GameObject, RectTransform, RectTransform)> _markers;

    private RectTransform _compassRT, _compassMarkersRT;
    private GameObject _newMarkerOnMap, _newMarkerOnCompass;
    private Transform _markersTF;
    private float _panelXBound, _panelYBound;
    private GameObject _markersObj;
    private float _buttonPressedTime;
    private MarkerType _selectedMarkerType;
    private FontIconSelector _waypointIcon, _obstacleIcon, _markerIcon, _roverIcon;
    private GameObject _actionButtons;
    private RectTransform _actionButtonsRT;
    private MapActionMode _actionMode;
    private bool _navigationOn = false;
    private GameObject _navigateTo;
    // private LineRenderer _lineRenderer;
    // private GameObject _curloc;

    void Start()
    {
        _mainCamera = Camera.main;
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _canvasRT = GameObject.Find("Canvas").GetComponent<RectTransform>();
        _meshBC = GameObject.Find("Map Panel").GetComponent<BoxCollider>();
        _markers = new Dictionary<GameObject, (Vector2, GameObject, GameObject, RectTransform, RectTransform)>();
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
        _actionButtons = GameObject.Find("Marker Action Buttons");
        _actionButtonsRT = _actionButtons.GetComponent<RectTransform>();
        _actionButtons.SetActive(false);
        _actionMode = MapActionMode.Pan;
        // _lineRenderer = GetComponent<LineRenderer>();
        // _curloc = GameObject.Find("Curloc");
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
                        if (_actionMode == MapActionMode.AddMarker)
                        {
                            _newMarkerOnMap = Instantiate(markerPrefab, _markersTF);
                            _newMarkerOnCompass = Instantiate(compassMarkerPrefab, _compassMarkersRT);
                            _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                            _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                            _markers.Add(_newMarkerOnMap,
								(MapPosToGPS(firstPosition),
                                //(MapToWorldPos(firstPosition),
                                    _newMarkerOnMap,
                                    _newMarkerOnCompass,
                                    _newMarkerOnMap.GetComponent<RectTransform>(),
                                    _newMarkerOnCompass.GetComponent<RectTransform>()
                                    ));
                            _actionMode = MapActionMode.EditMarker;
                        }
                        else if (_actionMode != MapActionMode.EditMarker)
                        {
                            float minDist = markerEditSensitivity + 1;
                            foreach (var kvp in _markers)
                            {
                                float dist = (kvp.Value.Item1 - MapPosToGPS(firstPosition)).magnitude;
                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    _newMarkerOnMap = kvp.Key;
                                    _newMarkerOnCompass = kvp.Value.Item3;
                                }
                            }

                            if (minDist < markerEditSensitivity)
                            {
                                _actionButtons.SetActive(true);
                                _actionMode = MapActionMode.SelectMarker;
                            }
                        }
                    }

                    // Update the offsets (top, right, bottom, left) based on the change in position
                    Vector2 delta = localTouchPosition - firstPosition;

                    switch (_actionMode)
                    {
                        case MapActionMode.Pan:
                            _mapRT.offsetMin = initialOffsetMin + delta;
                            _mapRT.offsetMax = _mapRT.offsetMin;
                            break;
                        case MapActionMode.EditMarker:
                            var markerItem = _markers[_newMarkerOnMap];
                            markerItem.Item1 = MapPosToGPS(localTouchPosition);
                            _markers[_newMarkerOnMap] = markerItem;
                            break;
                        case MapActionMode.SelectMarker:
                            var newPos = _newMarkerOnMap.transform.position;
                            newPos.z -= 0.02f;
                            newPos.y -= 0.033f;
                            _actionButtons.transform.position = newPos;
                            break;
                    }

                    // if (_navigationOn)
                    // {
                    //     Debug.Log("Navigate");
                    //     _lineRenderer.SetPosition(0, _curloc.transform.position);
                    //     _lineRenderer.SetPosition(1, _navigateTo.transform.position);
                    // }

                    // Write/update the last-position
                    if (lastPositions.ContainsKey(interactor))
                    {
                        lastPositions[interactor] = localTouchPosition;
                    }
                    else
                    {
                        lastPositions.Add(interactor, localTouchPosition);
                    }

                    _lastTouchPosition = localTouchPosition;

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
        // Vector3 userPos = _mainCamera.transform.position;
		// Vector2 userPosMap = WorldToMapPos(userPos);
		Vector2 GPSCoords = getGPSCoords();
		Vector2 userPosMap = GPSToMapPos(GPSCoords.x, GPSCoords.y);
        _mapRT.offsetMin = -userPosMap;
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

        if (_actionButtons.activeSelf)
        {
            _actionButtons.SetActive(false);
            _actionMode = MapActionMode.Pan;
            _newMarkerOnMap = null;
            _newMarkerOnCompass = null;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (_actionMode == MapActionMode.EditMarker)
        {
            _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
            _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
            _actionMode = MapActionMode.Pan;
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

    public void OnMarkerMovePressed()
    {
        _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        _actionMode = MapActionMode.EditMarker;
        _actionButtons.SetActive(false);
    }

    public void OnMarkerDeletePressed()
    {
        _markers.Remove(_newMarkerOnMap);
        Destroy(_newMarkerOnMap);
        Destroy(_newMarkerOnCompass);
        _actionMode = MapActionMode.Pan;
        _actionButtons.SetActive(false);
    }

    // public void OnMarkerNavigatePressed()
    // {
    //     _navigationOn = !_navigationOn;
    //     _navigateTo = _navigationOn ? _newMarkerOnMap : null;
    //     _lineRenderer.positionCount = _navigationOn ? 2 : 0;
    // }

    public void OnMarkerButtonSelectExit()
    {
        float delta = Time.time - _buttonPressedTime;
        if (delta > 0.7f)
        {
            _actionMode = MapActionMode.AddMarker;
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
        Vector2 mapOffset = _mapRT.offsetMin;
        Vector3 worldPos = new Vector3(mapPos.x - mapOffset.x, 0, mapPos.y - mapOffset.y);

        // Un-rotate then scale to obtain the world space position
        float mapRotZDeg = _mapRT.localEulerAngles.z;
        worldPos = Quaternion.Euler(0.0f, mapRotZDeg, 0.0f) * worldPos;

        float scaleW2M = 1000.0f * _mapRT.localScale.x;
        worldPos /= scaleW2M;

        return worldPos;
    }

    private Vector2 WorldToMapPos(Vector3 worldPos)
    {
        float scaleW2M = 1000.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;

        // Rotate then scale to obtain the map space position
        Vector3 mapPos = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * worldPos;
        mapPos *= scaleW2M;

        return new Vector2(mapPos.x, mapPos.z);
    }

    private Vector2 GPSToMapPos(float latitudeDeg, float longitudeDeg)
    {
        float du = (longitudeDeg - satCenterLongitude) / satLongitudeRange;  // -.5 ~ +.5 in horizontal map space
        float dv = (latitudeDeg - satCenterLatitude) / satLatitudeRange;     // -.5 ~ +.5 in vertical map sapce

        float mapRotZDeg = _mapRT.localEulerAngles.z;
        Vector3 mapPos = new Vector3(du, 0, dv) * _mapRT.localScale.x * _canvasRT.rect.height;
        mapPos = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * mapPos;

        return new Vector2(mapPos.x, mapPos.z);
    }

	// Acutually: PanelPos to GPS
    private Vector2 MapPosToGPS(Vector2 mapPos)
    {
        Vector2 mapOffset = _mapRT.offsetMin;
        Vector3 worldPos = new Vector3(mapPos.x - mapOffset.x, 0, mapPos.y - mapOffset.y);

        // Un-rotate then scale to obtain the world space position
        float mapRotZDeg = _mapRT.localEulerAngles.z;
        worldPos = Quaternion.Euler(0.0f, mapRotZDeg, 0.0f) * worldPos;

		worldPos /= (_mapRT.localScale.x * _canvasRT.rect.height);  // (du, 0, dv) in GPSToMapPos

		float longitudeDeg = worldPos.x * satLongitudeRange + satCenterLongitude;
		float latitudeDeg = worldPos.z * satLatitudeRange + satCenterLatitude;

        return new Vector2(latitudeDeg, longitudeDeg);
    }

    // For simulation in Unity
    private Vector2 getGPSCoords()
    {
        Vector3 worldPos = _mainCamera.transform.position;
        Vector2 gpsCoords = new Vector2(satCenterLatitude, satCenterLongitude);
        gpsCoords += 0.001f * new Vector2(worldPos.z, worldPos.x);
        return gpsCoords;
    }

    private void UpdateMarkers()
    {
        float compassWidth = _compassRT.rect.width / 360.0f;

        // Vector3 userPos = Camera.main.transform.position;
        Vector2 userGPS = getGPSCoords();
        Vector3 userLook = Camera.main.transform.forward;
        userLook.y = 0.0f;

        foreach(var kvp in _markers)
        {
            // Vector3 posWorldspace = item.Item1;    // marker pos in world space
            Vector2 markerGPS = kvp.Value.Item1;    // marker's GPS coords
            RectTransform rtMap = kvp.Value.Item4;
            RectTransform rtCompass = kvp.Value.Item5;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            // rtMap.offsetMin = _mapRT.offsetMin + new Vector2(posMapspaceUnrot.x, posMapspaceUnrot.z);
            rtMap.offsetMin = _mapRT.offsetMin + GPSToMapPos(markerGPS.x, markerGPS.y);
            rtMap.offsetMax = rtMap.offsetMin;

            // Adjust marker position on compass
            // Given userGPS and markerGPS, get markerDir that points from user to marker
            // Vector3 markerDir = posWorldspace - userPos;
            // markerDir.y = 0.0f;
            Vector2 markerRelGPS = markerGPS - userGPS;  // delta (latitutude, longitude)
            Vector3 markerDir = new Vector3(markerRelGPS.y, 0.0f, markerRelGPS.x);
            float angleToMarker = -Vector3.SignedAngle(markerDir, userLook, Vector3.up);
            rtCompass.offsetMin = new Vector2(angleToMarker * compassWidth, 0.0f);
            rtCompass.offsetMax = rtCompass.offsetMin;
        }
	}
}
