using System;
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
	public DateTime? _startTimestamp { get; private set; }

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
    private List<float> zoomSeries = new List<float>{ 1, 2, 5, 10 };
    private int zoomIndex = 1;

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
        POI,
        Rover
    };
    private Vector2 _lastTouchPosition;

    enum MapActionMode
    {
        Pan,
        AddMarker,
        SelectMarker,
        EditMarker
    }
    private Vector2 lastTouchPosition;

    private Dictionary<MarkerType, bool> showMarker;

    [SerializeField] private GameObject POIPrefab, obstaclePrefab, roverPrefab;
    [SerializeField] private GameObject compassMarkerPrefab;
    [SerializeField] private float markerEditSensitivity = 0.000033f;

    // Each marker is a (type, gpsCoords, mapMarkerObj, compassMarkerObj, mapRT, compassRT) 5-tuple
    private Dictionary<GameObject, (MarkerType, Vector2, GameObject, GameObject, RectTransform, RectTransform)> _markers;

    private RectTransform _compassRT, _compassMarkersRT;
    private GameObject _newMarkerOnMap, _newMarkerOnCompass;
    private Transform _markersTF;
    private float _panelXBound, _panelYBound;
    private GameObject _markersObj;
    private float _buttonPressedTime;
    private MarkerType _selectedMarkerType;
    private GameObject _obstacleDisabled, _POIDisabled, _roverDisabled;
    private GameObject _actionButtons;
    private RectTransform _actionButtonsRT;
    private MapActionMode _actionMode;
    private bool _navigationOn = false;
    private RectTransform _navigateTo;
    private LineRenderer _lineRenderer;
    private RectTransform _curlocRT;
    private Transform _panelTf;

    // Waypoint
    private List<(Vector2, GameObject, RectTransform)> waypoints;
    private bool recordingWaypoints = true;
    [SerializeField] private float waypointInterval = 0.00005f;
    [SerializeField] private GameObject waypointPrefab;
    private Transform waypointsTF;

    private float _canvasScale;
    private float _canvasHalfWidth;
    private float _canvasHalfHeight;

    // Voice
    private GameObject voiceMemoObj;

    void Start()
    {
        _mainCamera = Camera.main;
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _canvasRT = GameObject.Find("Canvas").GetComponent<RectTransform>();
        _meshBC = GameObject.Find("Map Panel").GetComponent<BoxCollider>();
        _markers = new Dictionary<GameObject, (MarkerType, Vector2, GameObject, GameObject, RectTransform, RectTransform)>();
        _compassRT = GameObject.Find("Compass Image").GetComponent<RectTransform>();
        _compassMarkersRT = GameObject.Find("Compass Markers").GetComponent<RectTransform>();
        _markersObj = GameObject.Find("Markers");
        _markersTF = _markersObj.GetComponent<Transform>();
        var panelSize = GameObject.Find("Map Panel").GetComponent<BoxCollider>().size;
        _panelXBound = panelSize.x / 2;
        _panelYBound = panelSize.y / 2;
        // _waypointDisabled = GameObject.Find("Waypoint Disabled");
        _roverDisabled = GameObject.Find("Rover Disabled");
        _obstacleDisabled = GameObject.Find("Obstacle Disabled");
        _POIDisabled = GameObject.Find("POI Disabled");
        // _waypointDisabled.SetActive(false);
        _roverDisabled.SetActive(false);
        _obstacleDisabled.SetActive(false);
        _POIDisabled.SetActive(false);
        _actionButtons = GameObject.Find("Marker Action Buttons");
        _actionButtonsRT = _actionButtons.GetComponent<RectTransform>();
        _actionButtons.SetActive(false);
        _actionMode = MapActionMode.Pan;
        _lineRenderer = GameObject.Find("Map").GetComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.001f;
        _lineRenderer.endWidth = 0.001f;
        _lineRenderer.numCornerVertices = 5;
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
        _panelTf = GameObject.Find("Map Panel").GetComponent<Transform>();
        _canvasScale = GameObject.Find("Canvas").transform.localScale.x;
        Rect canvasR = _canvasRT.rect;
        _canvasHalfWidth = canvasR.width / 2;
        _canvasHalfHeight = canvasR.height / 2;
        _mapRT.localScale = getLocalScale(zoomSeries[zoomIndex]);
        waypoints = new List<(Vector2, GameObject, RectTransform)>();
        waypointsTF = GameObject.Find("Waypoints").GetComponent<Transform>();
        showMarker = new Dictionary<MarkerType, bool>
        {
            { MarkerType.Obstacle, true },
            { MarkerType.Rover, true },
            { MarkerType.Waypoint, true },
            { MarkerType.POI, true }
        };
        voiceMemoObj = GameObject.Find("Voice Memo");
        voiceMemoObj.SetActive(false);
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
        UpdateMarkers();
        if (_navigationOn) Navigate();
        if (recordingWaypoints) RecordWaypoints();
        else ReplayWaypoints();
    }

    private void RecordWaypoints()
    {
        // Vector2 currGPS = MapPosToGPS(new Vector2(0, 0));
        Vector2 currGPS = getGPSCoords();
        int numWaypoints = waypoints.Count;

        if (numWaypoints == 0 || (currGPS - waypoints[numWaypoints-1].Item1).magnitude > waypointInterval)
        {
            GameObject newWaypoint = Instantiate(waypointPrefab, waypointsTF);
            newWaypoint.SetActive(false);
            waypoints.Add((currGPS, newWaypoint, newWaypoint.GetComponent<RectTransform>()));
        }
    }

    private void ReplayWaypoints()
    {
        Vector2 currGPS = getGPSCoords();
        int numWaypoints = waypoints.Count;

        if (numWaypoints == 0) return;

        if ((currGPS - waypoints[numWaypoints - 1].Item1).magnitude < waypointInterval / 3)
        {
            numWaypoints--;
            Destroy(waypoints[numWaypoints].Item2);
            waypoints.RemoveAt(numWaypoints);
            _lineRenderer.positionCount = numWaypoints + 1;
        }

        for (int i = 0; i < numWaypoints; i++)
        {
            Vector2 waypointGPS = waypoints[i].Item1;
            RectTransform rtMap = waypoints[i].Item3;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            rtMap.offsetMin = _mapRT.offsetMin + GPSToMapPos(waypointGPS.x, waypointGPS.y);
            rtMap.offsetMax = rtMap.offsetMin;
            _lineRenderer.SetPosition(i, OffsetToPos(rtMap.offsetMin));
        }

        _lineRenderer.SetPosition(numWaypoints, OffsetToPos(_curlocRT.offsetMin));
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
                            switch (_selectedMarkerType)
                            {
                                case MarkerType.Obstacle:
                                    _newMarkerOnMap = Instantiate(obstaclePrefab, _markersTF);
                                    _newMarkerOnCompass = Instantiate(obstaclePrefab, _compassMarkersRT);
                                    break;
                                case MarkerType.Rover:
                                    _newMarkerOnMap = Instantiate(roverPrefab, _markersTF);
                                    _newMarkerOnCompass = Instantiate(roverPrefab, _compassMarkersRT);
                                    break;
                                default:
                                    _newMarkerOnMap = Instantiate(POIPrefab, _markersTF);
                                    _newMarkerOnCompass = Instantiate(POIPrefab, _compassMarkersRT);
                                    break;
                            }
                            _newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                            _newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                            _markers.Add(_newMarkerOnMap,
								(_selectedMarkerType,
                                    MapPosToGPS(firstPosition),
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
                                float dist = (kvp.Value.Item2 - MapPosToGPS(firstPosition)).magnitude;
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
                            else
                            {
                                _newMarkerOnMap = null;
                                _newMarkerOnCompass = null;
                                if (voiceMemoObj.activeSelf) {
                                    voiceMemoObj.SetActive(false);
                                }
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
                            markerItem.Item2 = MapPosToGPS(localTouchPosition);
                            _markers[_newMarkerOnMap] = markerItem;
                            break;
                        case MapActionMode.SelectMarker:
                            var newPos = _newMarkerOnMap.transform.position;
                            newPos.z -= 0.02f;
                            newPos.y -= 0.02f;
                            _actionButtons.transform.position = newPos;
                            break;
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

                    _lastTouchPosition = localTouchPosition;

                    break;
                }
            }
        }
    }

    /************* Scale ***************/

    private Vector3 getLocalScale(float scale)
    {
        return new Vector3(scale, scale, 1.0f);
    }

    public void MapZoomInCallback()
    {
        zoomIndex = zoomIndex >= zoomSeries.Count - 1 ? zoomIndex : zoomIndex + 1;
        _mapRT.localScale = getLocalScale(zoomSeries[zoomIndex]);
    }

    public void MapZoomOutCallback()
    {
        zoomIndex = zoomIndex <= 0 ? zoomIndex : zoomIndex - 1;
        _mapRT.localScale = getLocalScale(zoomSeries[zoomIndex]);
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
    public void OnPOISelectEnter()
    {
        _selectedMarkerType = MarkerType.POI;
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

    public void OnMarkerNavigatePressed()
    {
        _navigationOn = !_navigationOn;
        _navigateTo = _navigationOn ? _newMarkerOnMap.GetComponent<RectTransform>() : null;
        _lineRenderer.positionCount = _navigationOn ? 2 : 0;
        _actionMode = MapActionMode.Pan;
        _actionButtons.SetActive(false);
    }

    private Vector3 OffsetToPos(Vector2 offset)
    {
        Vector3 pos = _panelTf.position;
        Quaternion rot = _panelTf.rotation;

        pos += 0.088f * offset.x / _canvasHalfWidth * (rot * Vector3.right);
        pos += 0.070f * offset.y / _canvasHalfHeight * (rot * Vector3.up);
        pos += 0.015f * (rot * Vector3.back);

        return pos;
    }

    private void Navigate()
    {
        Vector3 mapPos = _panelTf.position;
        _lineRenderer.SetPosition(0, OffsetToPos(_curlocRT.offsetMin));
        _lineRenderer.SetPosition(1, OffsetToPos(_navigateTo.offsetMin));
    }

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
                // case MarkerType.Waypoint:
                //     _waypointDisabled.SetActive(!_waypointDisabled.activeSelf);
                //     showMarker[MarkerType.Waypoint] = !_waypointDisabled.activeSelf;
                //     break;
                case MarkerType.Obstacle:
                    _obstacleDisabled.SetActive(!_obstacleDisabled.activeSelf);
                    showMarker[MarkerType.Obstacle] = !_obstacleDisabled.activeSelf;
                    break;
                case MarkerType.POI:
                    _POIDisabled.SetActive(!_POIDisabled.activeSelf);
                    showMarker[MarkerType.POI] = !_POIDisabled.activeSelf;
                    break;
                case MarkerType.Rover:
                    _roverDisabled.SetActive(!_roverDisabled.activeSelf);
                    showMarker[MarkerType.Rover] = !_roverDisabled.activeSelf;
                    break;
            }
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
        gpsCoords += 5e-5f * new Vector2(worldPos.z, worldPos.x);
        return gpsCoords;
    }

    private void UpdateMarkers()
    {
        float compassWidth = _compassRT.rect.width / 360.0f;

        // Vector3 userPos = Camera.main.transform.position;
        Vector2 userGPS = getGPSCoords();
        Vector3 userLook = _mainCamera.transform.forward;
        userLook.y = 0.0f;

		/************* CurLoc ************/
		// Rotate CurLoc
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        float mapRotZDeg = _mapRT.localEulerAngles.z;
        _curlocRT.localRotation = Quaternion.Euler(0, 0, mapRotZDeg - lookAngleZDeg);
		// Translate CurLoc
		Vector2 gpsCoords = getGPSCoords();
        _curlocRT.offsetMin = _mapRT.offsetMin + GPSToMapPos(gpsCoords.x, gpsCoords.y);
        _curlocRT.offsetMax = _curlocRT.offsetMin;
		/*********************************/


        foreach(var kvp in _markers)
        {
            // Vector3 posWorldspace = item.Item1;    // marker pos in world space
            GameObject obj = kvp.Key;
            MarkerType type = kvp.Value.Item1;

            obj.SetActive(showMarker[type]);
            if (!showMarker[type]) continue;

            Vector2 markerGPS = kvp.Value.Item2;    // marker's GPS coords
            RectTransform rtMap = kvp.Value.Item5;
            RectTransform rtCompass = kvp.Value.Item6;

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

    public void ToggleWaypointAction()
    {
        recordingWaypoints = !recordingWaypoints;

        foreach (var waypoint in waypoints)
        {
            if (!recordingWaypoints) waypoint.Item2.SetActive(true);
            else Destroy(waypoint.Item2);
        }

        if (recordingWaypoints) waypoints.Clear();
        else _lineRenderer.positionCount = waypoints.Count + 1;
    }

    public void VoiceMemoOnclick()
    {
        _actionMode = MapActionMode.Pan;
        _actionButtons.SetActive(false);
        voiceMemoObj.SetActive(true);
    }

	// Time
	public void RecordStartTime()
	{
		_startTimestamp = DateTime.Now;
	}
}

