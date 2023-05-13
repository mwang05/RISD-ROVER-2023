using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UX;
using Microsoft.MixedReality.Toolkit;
using System;
using UnityEngine.Serialization;
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

    private RectTransform mapRT, canvasRT;
    private Camera mainCamera;

    // Satellite info
    // hard coded center
    private const float SatCenterLatitude  = 29.564575f;   // latitude at the center of the satellite image, in degree
    private const float SatCenterLongitude = -95.081164f;  // longitude at the center of the satellite image, in degree
    // hard coded scale
    private const float SatLatitudeRange = 0.002216f;  // the satellite image covers this much latitudes in degree
    private const float SatLongitudeRange = 0.00255f;  // the satellite image covers this much longitudes in degree


    // Zoom
    private readonly List<float> zoomSeries = new List<float>{ 1, 2, 5, 10 };
    private int zoomIndex = 1;

    // Pan
    private Dictionary<IXRInteractor, Vector2> lastPositions = new Dictionary<IXRInteractor, Vector2>();
    private Vector2 firstPosition;
    private Vector2 initialOffsetMin;

    // Focus
    private MapFocusMode focusMode = MapFocusMode.MapNoFocus;

    // Marker
    enum MarkerType
    {
        Waypoint,
        Obstacle,
        POI,
        Rover
    };

    enum MapActionMode
    {
        Pan,
        AddMarker,
        SelectMarker,
        EditMarker
    }
    private Vector2 lastTouchPosition;

    private Dictionary<MarkerType, bool> showMarker;

    [SerializeField] private GameObject poiPrefab;
    [SerializeField] private GameObject obstaclePrefab, roverPrefab;
    [SerializeField] private float markerEditSensitivity = 0.000033f;

    // Each marker is a (type, gpsCoords, mapMarkerObj, compassMarkerObj, mapRT, compassRT) 5-tuple
    private Dictionary<GameObject, (MarkerType, Vector2, GameObject, GameObject, RectTransform, RectTransform)> markers;

    private RectTransform compassRT, compassMarkersRT;
    private GameObject newMarkerOnMap, newMarkerOnCompass;
    private Transform markersTf;
    private GameObject markersObj;
    private float buttonPressedTime;
    private MarkerType selectedMarkerType;
    private GameObject obstacleDisabled, poiDisabled, roverDisabled;
    private GameObject actionButtons;
    private MapActionMode actionMode;
    private bool navigationOn = false;
    private RectTransform navigateTo;
    private LineRenderer markerLineRenderer;
    private RectTransform currLocRT;
    private Transform panelTf;

    // Waypoint
    private List<(Vector2, GameObject, RectTransform)> waypoints;
    private bool recordingWaypoints = true;
    [SerializeField] private float waypointInterval = 0.00005f;
    [SerializeField] private GameObject waypointPrefab;
    private Transform waypointsTf;

    // // Navigation
    [SerializeField] private GameObject segmentPrefab;
    private List<GameObject> segmentObjs;
    private List<LineRenderer> lineRenderers;
    private Transform navigationTf;

    private float canvasHalfWidth;
    private float canvasHalfHeight;

    // Voice
    private GameObject voiceMemoObj;

    void Start()
    {
        mainCamera = Camera.main;
        mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        canvasRT = GameObject.Find("Canvas").GetComponent<RectTransform>();
        markers = new Dictionary<GameObject, (MarkerType, Vector2, GameObject, GameObject, RectTransform, RectTransform)>();
        compassRT = GameObject.Find("Compass Image").GetComponent<RectTransform>();
        compassMarkersRT = GameObject.Find("Compass Markers").GetComponent<RectTransform>();
        markersObj = GameObject.Find("Markers");
        markersTf = markersObj.GetComponent<Transform>();
        var panelSize = GameObject.Find("Map Panel").GetComponent<BoxCollider>().size;
        // _waypointDisabled = GameObject.Find("Waypoint Disabled");
        roverDisabled = GameObject.Find("Rover Disabled");
        obstacleDisabled = GameObject.Find("Obstacle Disabled");
        poiDisabled = GameObject.Find("POI Disabled");
        // _waypointDisabled.SetActive(false);
        roverDisabled.SetActive(false);
        obstacleDisabled.SetActive(false);
        poiDisabled.SetActive(false);
        actionButtons = GameObject.Find("Marker Action Buttons");
        actionButtons.SetActive(false);
        actionMode = MapActionMode.Pan;
        markerLineRenderer = GameObject.Find("Map").GetComponent<LineRenderer>();
        markerLineRenderer.startWidth = 0.001f;
        markerLineRenderer.endWidth = 0.001f;
        currLocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
        panelTf = GameObject.Find("Map Panel").GetComponent<Transform>();
        Rect canvasR = canvasRT.rect;
        canvasHalfWidth = canvasR.width / 2;
        canvasHalfHeight = canvasR.height / 2;
        mapRT.localScale = GetLocalScale(zoomSeries[zoomIndex]);
        waypoints = new List<(Vector2, GameObject, RectTransform)>();
        waypointsTf = GameObject.Find("Waypoints").GetComponent<Transform>();
        showMarker = new Dictionary<MarkerType, bool>
        {
            { MarkerType.Obstacle, true },
            { MarkerType.Rover, true },
            { MarkerType.Waypoint, true },
            { MarkerType.POI, true }
        };
        voiceMemoObj = GameObject.Find("Voice Memo");
        voiceMemoObj.SetActive(false);
        navigationTf = GameObject.Find("Navigation").transform;
        segmentObjs = new List<GameObject>();
        lineRenderers = new List<LineRenderer>();
    }

    void Update()
    {
        switch (focusMode)
        {
            case MapFocusMode.MapCenterUser:
                CenterMapAtUser();
                break;
            case MapFocusMode.MapAlignUser:
                AlignMapWithUser();
                break;
        }
        UpdateMarkers();
        if (navigationOn) Navigate();
        if (recordingWaypoints) RecordWaypoints();
        else ReplayWaypoints();
    }

    private void RecordWaypoints()
    {
        // Vector2 currGPS = MapPosToGPS(new Vector2(0, 0));
        Vector2 gpsCoords = GetGpsCoords();
        int numWaypoints = waypoints.Count;

        if (numWaypoints == 0 || (gpsCoords - waypoints[numWaypoints-1].Item1).magnitude > waypointInterval)
        {
            GameObject newWaypoint = Instantiate(waypointPrefab, waypointsTf);
            newWaypoint.SetActive(false);
            waypoints.Add((gpsCoords, newWaypoint, newWaypoint.GetComponent<RectTransform>()));
        }
    }

    private void ReplayWaypoints()
    {
        Vector2 currGps = GetGpsCoords();
        int numWaypoints = waypoints.Count;

        if (numWaypoints == 0) return;

        if ((currGps - waypoints[numWaypoints - 1].Item1).magnitude < waypointInterval / 3)
        {
            numWaypoints--;
            Destroy(waypoints[numWaypoints].Item2);
            waypoints.RemoveAt(numWaypoints);
        }

        List<Vector2> offsets = new List<Vector2>();

        for (int i = 0; i < numWaypoints; i++)
        {
            Vector2 waypointGPS = waypoints[i].Item1;
            RectTransform rtMap = waypoints[i].Item3;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            rtMap.offsetMin = mapRT.offsetMin + GpsToMapPos(waypointGPS.x, waypointGPS.y);
            rtMap.offsetMax = rtMap.offsetMin;
            offsets.Add(rtMap.offsetMin);
        }
        offsets.Add(currLocRT.offsetMin);

        List<List<Vector3>> routes = GetRoute(offsets);
        if (routes.Count > segmentObjs.Count)
        {
            for (int i = 0; i < routes.Count - segmentObjs.Count; i++)
            {
                GameObject segment = Instantiate(segmentPrefab, navigationTf);
                segmentObjs.Add(segment);
                lineRenderers.Add(segment.GetComponent<LineRenderer>());
            }
        }
        else
        {
            for (int i = routes.Count; i < segmentObjs.Count; i++)
            {
                Destroy(segmentObjs[i]);
            }

            segmentObjs.RemoveRange(routes.Count, segmentObjs.Count - routes.Count);
            lineRenderers.RemoveRange(routes.Count, lineRenderers.Count - routes.Count);
        }

        for (int i = 0; i < routes.Count; i++)
        {
            lineRenderers[i].positionCount = routes[i].Count;
            lineRenderers[i].SetPositions(routes[i].ToArray());
        }
    }

    private List<List<Vector3>> GetRoute(List<Vector2> offsets)
    {
        List<List<Vector3>> segments = new List<List<Vector3>>();
        List<Vector3> positions = new List<Vector3>();

        bool prevInScope = true;
        float maxHeight = canvasHalfHeight * 0.97f;
        float maxWidth = canvasHalfWidth * 1.01f;

        bool IsWithinScope(Vector2 offset)
        {
            return (Math.Abs(offset.x) <= maxWidth && Math.Abs(offset.y) <= maxHeight);
        }

        Vector3 GetIntersectionWithBorder(Vector2 outside, Vector2 inside)
        {
            Vector2 outsideToInside = inside - outside;
            float deltaX = Math.Abs(outside.x) - maxWidth;
            float deltaY = Math.Abs(outside.y) - maxHeight;
            float scale = deltaX > deltaY ? Math.Abs(deltaX / outsideToInside.x) : Math.Abs(deltaY / outsideToInside.y);

            return OffsetToPos(outside + outsideToInside * scale);
        }

        for (int i = 0; i < offsets.Count; i++)
        {
            bool currInScope = IsWithinScope(offsets[i]);
            bool nextInScope = i == offsets.Count - 1 || IsWithinScope(offsets[i + 1]);

            if (currInScope)
            {
                if (!prevInScope)
                {
                    Vector3 prevIntersection = GetIntersectionWithBorder(offsets[i - 1], offsets[i]);
                    positions.Add(prevIntersection);
                }
                positions.Add(OffsetToPos(offsets[i]));
                if (!nextInScope)
                {
                    Vector3 nextIntersection = GetIntersectionWithBorder(offsets[i + 1], offsets[i]);
                    positions.Add(nextIntersection);
                    segments.Add(positions);
                    positions = new List<Vector3>();
                }
            }

            prevInScope = currInScope;
        }

        if (positions.Count > 1) segments.Add(positions);

        return segments;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

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
                    initialOffsetMin = mapRT.offsetMin;

                    // Focus
                    focusMode = MapFocusMode.MapNoFocus;

                    // Marker
                    if (actionMode == MapActionMode.AddMarker)
                    {
                        switch (selectedMarkerType)
                        {
                            case MarkerType.Obstacle:
                                newMarkerOnMap = Instantiate(obstaclePrefab, markersTf);
                                newMarkerOnCompass = Instantiate(obstaclePrefab, compassMarkersRT);
                                break;
                            case MarkerType.Rover:
                                newMarkerOnMap = Instantiate(roverPrefab, markersTf);
                                newMarkerOnCompass = Instantiate(roverPrefab, compassMarkersRT);
                                break;
                            default:
                                newMarkerOnMap = Instantiate(poiPrefab, markersTf);
                                newMarkerOnCompass = Instantiate(poiPrefab, compassMarkersRT);
                                break;
                        }
                        newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                        newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
                        markers.Add(newMarkerOnMap,
							(selectedMarkerType,
                                MapPosToGps(firstPosition),
                                newMarkerOnMap,
                                newMarkerOnCompass,
                                newMarkerOnMap.GetComponent<RectTransform>(),
                                newMarkerOnCompass.GetComponent<RectTransform>()
                                ));
                        actionMode = MapActionMode.EditMarker;
                    }
                    else if (actionMode != MapActionMode.EditMarker)
                    {
                        float minDist = markerEditSensitivity + 1;
                        foreach (var kvp in markers)
                        {
                            float dist = (kvp.Value.Item2 - MapPosToGps(firstPosition)).magnitude;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                newMarkerOnMap = kvp.Key;
                                newMarkerOnCompass = kvp.Value.Item3;
                            }
                        }

                        if (minDist < markerEditSensitivity)
                        {
                            actionButtons.SetActive(true);
                            actionMode = MapActionMode.SelectMarker;
                        }
                        else
                        {
                            newMarkerOnMap = null;
                            newMarkerOnCompass = null;
                            if (voiceMemoObj.activeSelf) {
                                voiceMemoObj.SetActive(false);
                            }
                        }
                    }
                }

                // Update the offsets (top, right, bottom, left) based on the change in position
                Vector2 delta = localTouchPosition - firstPosition;

                switch (actionMode)
                {
                    case MapActionMode.Pan:
                        mapRT.offsetMin = initialOffsetMin + delta;
                        mapRT.offsetMax = mapRT.offsetMin;
                        break;
                    case MapActionMode.EditMarker:
                        var markerItem = markers[newMarkerOnMap];
                        markerItem.Item2 = MapPosToGps(localTouchPosition);
                        markers[newMarkerOnMap] = markerItem;
                        break;
                    case MapActionMode.SelectMarker:
                        var newPos = newMarkerOnMap.transform.position;
                        newPos.z -= 0.02f;
                        newPos.y -= 0.02f;
                        actionButtons.transform.position = newPos;
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

                break;
            }
        }

    }

    /************* Scale ***************/

    private static Vector3 GetLocalScale(float scale)
    {
        return new Vector3(scale, scale, 1.0f);
    }

    public void MapZoomInCallback()
    {
        zoomIndex = zoomIndex >= zoomSeries.Count - 1 ? zoomIndex : zoomIndex + 1;
        mapRT.localScale = GetLocalScale(zoomSeries[zoomIndex]);
    }

    public void MapZoomOutCallback()
    {
        zoomIndex = zoomIndex <= 0 ? zoomIndex : zoomIndex - 1;
        mapRT.localScale = GetLocalScale(zoomSeries[zoomIndex]);
    }

    /************* Focus **************/
    public void MapFocusCallback()
    {
        MapToggleFocusMode();
        switch (focusMode)
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
        Vector3 userLook = mainCamera.transform.forward;

        // Rotate map so that currLoc points up
        userLook.y = 0.0f;
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        mapRT.localRotation = Quaternion.Euler(0.0f, 0.0f, lookAngleZDeg);
    }

    private void CenterMapAtUser()
    {
        Vector2 gpsCoords = GetGpsCoords();
		Vector2 userPosMap = GpsToMapPos(gpsCoords.x, gpsCoords.y);
        mapRT.offsetMin = -userPosMap;
        mapRT.offsetMax = mapRT.offsetMin;
    }

    private void AlignMapWithUser()
    {
        RotateMapWithUser();
        CenterMapAtUser();
    }

    private void MapToggleFocusMode()
    {
        int newMode = ((int)focusMode + 1) % (int)MapFocusMode.NumMapFocusModes;
        focusMode = (MapFocusMode)newMode;
    }

    private void MapRestoreLastRotation()
    {
        mapRT.localRotation = Quaternion.Euler(0, 0, 0);
    }
    /***************************/

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (actionButtons.activeSelf)
        {
            actionButtons.SetActive(false);
            actionMode = MapActionMode.Pan;
            newMarkerOnMap = null;
            newMarkerOnCompass = null;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (actionMode == MapActionMode.EditMarker)
        {
            newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
            newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 1);
            actionMode = MapActionMode.Pan;
        }

        // Remove the interactor from our last-position collection when it leaves.
        lastPositions.Remove(args.interactorObject);
    }

    /************* Marker **************/
    public void OnWaypointSelectEnter()
    {
        selectedMarkerType = MarkerType.Waypoint;
        buttonPressedTime = Time.time;
    }
    public void OnObstacleSelectEnter()
    {
        selectedMarkerType = MarkerType.Obstacle;
        buttonPressedTime = Time.time;
    }
    public void OnPOISelectEnter()
    {
        selectedMarkerType = MarkerType.POI;
        buttonPressedTime = Time.time;
    }
    public void OnRoverSelectEnter()
    {
        selectedMarkerType = MarkerType.Rover;
        buttonPressedTime = Time.time;
    }

    public void OnMarkerMovePressed()
    {
        newMarkerOnMap.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        newMarkerOnCompass.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        actionMode = MapActionMode.EditMarker;
        actionButtons.SetActive(false);
    }

    public void OnMarkerDeletePressed()
    {
        if (navigationOn && navigateTo == newMarkerOnMap.GetComponent<RectTransform>())
        {
            navigationOn = false;
            navigateTo = null;
            markerLineRenderer.positionCount = 0;
        }
        markers.Remove(newMarkerOnMap);
        Destroy(newMarkerOnMap);
        Destroy(newMarkerOnCompass);
        actionMode = MapActionMode.Pan;
        actionButtons.SetActive(false);
    }

    public void OnMarkerNavigatePressed()
    {
        navigationOn = !navigationOn;
        navigateTo = navigationOn ? newMarkerOnMap.GetComponent<RectTransform>() : null;
        markerLineRenderer.positionCount = navigationOn ? 2 : 0;
        actionMode = MapActionMode.Pan;
        actionButtons.SetActive(false);
    }

    private Vector3 OffsetToPos(Vector2 offset)
    {
        Vector3 pos = panelTf.position;
        Quaternion rot = panelTf.rotation;

        pos += 0.088f * offset.x / canvasHalfWidth * (rot * Vector3.right);
        pos += 0.070f * offset.y / canvasHalfHeight * (rot * Vector3.up);
        pos += 0.006f * (mainCamera.transform.position - pos);

        return pos;
    }

    private void Navigate()
    {
        markerLineRenderer.positionCount = 2;
        List<Vector2> offsets = new List<Vector2> { navigateTo.offsetMin, currLocRT.offsetMin };
        markerLineRenderer.SetPositions(GetRoute(offsets)[0].ToArray());
    }

    public void OnMarkerButtonSelectExit()
    {
        float delta = Time.time - buttonPressedTime;
        if (delta > 0.7f)
        {
            actionMode = MapActionMode.AddMarker;
        }
        else
        {
            switch (selectedMarkerType)
            {
                // case MarkerType.Waypoint:
                //     _waypointDisabled.SetActive(!_waypointDisabled.activeSelf);
                //     showMarker[MarkerType.Waypoint] = !_waypointDisabled.activeSelf;
                //     break;
                case MarkerType.Obstacle:
                    obstacleDisabled.SetActive(!obstacleDisabled.activeSelf);
                    showMarker[MarkerType.Obstacle] = !obstacleDisabled.activeSelf;
                    break;
                case MarkerType.POI:
                    poiDisabled.SetActive(!poiDisabled.activeSelf);
                    showMarker[MarkerType.POI] = !poiDisabled.activeSelf;
                    break;
                case MarkerType.Rover:
                    roverDisabled.SetActive(!roverDisabled.activeSelf);
                    showMarker[MarkerType.Rover] = !roverDisabled.activeSelf;
                    break;
            }
        }
    }

    private Vector3 MapToWorldPos(Vector2 mapPos)
    {
        Vector2 mapOffset = mapRT.offsetMin;
        Vector3 worldPos = new Vector3(mapPos.x - mapOffset.x, 0, mapPos.y - mapOffset.y);

        // Un-rotate then scale to obtain the world space position
        float mapRotZDeg = mapRT.localEulerAngles.z;
        worldPos = Quaternion.Euler(0.0f, mapRotZDeg, 0.0f) * worldPos;

        float scaleW2M = 1000.0f * mapRT.localScale.x;
        worldPos /= scaleW2M;

        return worldPos;
    }

    private Vector2 WorldToMapPos(Vector3 worldPos)
    {
        float scaleW2M = 1000.0f * mapRT.localScale.x;
        float mapRotZDeg = mapRT.localEulerAngles.z;

        // Rotate then scale to obtain the map space position
        Vector3 mapPos = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * worldPos;
        mapPos *= scaleW2M;

        return new Vector2(mapPos.x, mapPos.z);
    }

    private Vector2 GpsToMapPos(float latitudeDeg, float longitudeDeg)
    {
        float du = (longitudeDeg - SatCenterLongitude) / SatLongitudeRange;  // -.5 ~ +.5 in horizontal map space
        float dv = (latitudeDeg - SatCenterLatitude) / SatLatitudeRange;     // -.5 ~ +.5 in vertical map sapce

        float mapRotZDeg = mapRT.localEulerAngles.z;
        Vector3 mapPos =  mapRT.localScale.x * canvasRT.rect.height * new Vector3(du, 0, dv);
        mapPos = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * mapPos;

        return new Vector2(mapPos.x, mapPos.z);
    }

	// Actually: PanelPos to GPS
    private Vector2 MapPosToGps(Vector2 mapPos)
    {
        Vector2 mapOffset = mapRT.offsetMin;
        Vector3 worldPos = new Vector3(mapPos.x - mapOffset.x, 0, mapPos.y - mapOffset.y);

        // Un-rotate then scale to obtain the world space position
        float mapRotZDeg = mapRT.localEulerAngles.z;
        worldPos = Quaternion.Euler(0.0f, mapRotZDeg, 0.0f) * worldPos;

		worldPos /= (mapRT.localScale.x * canvasRT.rect.height);  // (du, 0, dv) in GPSToMapPos

		float longitudeDeg = worldPos.x * SatLongitudeRange + SatCenterLongitude;
		float latitudeDeg = worldPos.z * SatLatitudeRange + SatCenterLatitude;

        return new Vector2(latitudeDeg, longitudeDeg);
    }

    // For simulation in Unity
    private Vector2 GetGpsCoords()
    {
        Vector3 worldPos = mainCamera.transform.position;
        Vector2 gpsCoords = new Vector2(SatCenterLatitude, SatCenterLongitude);
        gpsCoords += 5e-5f * new Vector2(worldPos.z, worldPos.x);
        return gpsCoords;
    }

    private void UpdateMarkers()
    {
        float compassWidth = compassRT.rect.width / 360.0f;

        // Vector3 userPos = Camera.main.transform.position;
        Vector2 userGps = GetGpsCoords();
        Vector3 userLook = mainCamera.transform.forward;
        userLook.y = 0.0f;

		/************* CurLoc ************/
		// Rotate CurLoc
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        float mapRotZDeg = mapRT.localEulerAngles.z;
        currLocRT.localRotation = Quaternion.Euler(0, 0, mapRotZDeg - lookAngleZDeg);
		// Translate CurLoc
		Vector2 gpsCoords = GetGpsCoords();
        currLocRT.offsetMin = mapRT.offsetMin + GpsToMapPos(gpsCoords.x, gpsCoords.y);
        currLocRT.offsetMax = currLocRT.offsetMin;
		/*********************************/


        foreach(var kvp in markers)
        {
            // Vector3 posWorldSpace = item.Item1;    // marker pos in world space
            GameObject obj = kvp.Key;
            MarkerType type = kvp.Value.Item1;

            obj.SetActive(showMarker[type]);
            if (!showMarker[type]) continue;

            Vector2 markerGps = kvp.Value.Item2;    // marker's GPS coords
            RectTransform rtMap = kvp.Value.Item5;
            RectTransform rtCompass = kvp.Value.Item6;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            // rtMap.offsetMin = _mapRT.offsetMin + new Vector2(posMapSpaceUnrot.x, posMapSpaceUnrot.z);
            rtMap.offsetMin = mapRT.offsetMin + GpsToMapPos(markerGps.x, markerGps.y);
            rtMap.offsetMax = rtMap.offsetMin;

            // Adjust marker position on compass
            // Given userGPS and markerGPS, get markerDir that points from user to marker
            // Vector3 markerDir = posWorldSpace - userPos;
            // markerDir.y = 0.0f;
            Vector2 markerRelGps = markerGps - userGps;  // delta (latitude, longitude)
            Vector3 markerDir = new Vector3(markerRelGps.y, 0.0f, markerRelGps.x);
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
        else markerLineRenderer.positionCount = waypoints.Count + 1;
    }

    public void VoiceMemoOnclick()
    {
        actionMode = MapActionMode.Pan;
        actionButtons.SetActive(false);
        voiceMemoObj.SetActive(true);
    }
}

