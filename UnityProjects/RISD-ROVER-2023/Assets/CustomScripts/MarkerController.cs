using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public enum MarkerType
{
    Obstacle,
    Rover,
    POI
};
public enum MarkerActionMode
{
    None,
    Add,
    Edit,
    Select,
    Memo
}

public class MarkerController : MonoBehaviour
{
    private class Marker
    {
        public readonly MarkerType Type;
        public Vector2 GpsCoord;
        public readonly GameObject MapMarkerObj;
        public readonly RectTransform MapMarkerRT;
        private readonly GameObject compassMarkerObj;
        private readonly RectTransform compassMarkerRT;

        public Marker(MarkerType type, Vector2 gpsCoord)
        {
            Type = type;
            GpsCoord = gpsCoord;
            MapMarkerObj = Instantiate(prefabDict[type], markersTf);
            compassMarkerObj = Instantiate(prefabDict[type], compassMarkersRT);
            MapMarkerRT = MapMarkerObj.GetComponent<RectTransform>();
            compassMarkerRT = compassMarkerObj.GetComponent<RectTransform>();
        }

        public void CleanUp()
        {
            Destroy(MapMarkerObj);
            Destroy(compassMarkerObj);
        }

        public void Update(Vector2 userGps, Vector3 userLook)
        {
            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE
            Vector2 mapMarkerOffset = gps.GpsToMapPos(GpsCoord.x, GpsCoord.y);
            MapMarkerRT.offsetMin = mapRT.offsetMin + mapMarkerOffset;
            MapMarkerRT.offsetMax = MapMarkerRT.offsetMin;

            // Adjust marker position on compass
            // Given userGPS and markerGPS, get markerDir that points from user to marker
            Vector2 markerRelGps = GpsCoord - userGps;  // delta (latitude, longitude)
            Vector3 markerDir = new Vector3(markerRelGps.y, 0.0f, markerRelGps.x);
            float angleToMarker = -Vector3.SignedAngle(markerDir, userLook, Vector3.up);
            compassMarkerRT.offsetMin = new Vector2(angleToMarker * compassWidth, 0.0f);
            compassMarkerRT.offsetMax = compassMarkerRT.offsetMin;
        }

        public void SetOpacity(float opacity)
        {
            MapMarkerObj.GetComponent<RawImage>().color = new Color(1, 1, 1, opacity);
            compassMarkerObj.GetComponent<RawImage>().color = new Color(1, 1, 1, opacity);
        }
    }

    [SerializeField] private float markerEditSensitivity = 0.000033f;

    private static RectTransform mapRT, compassRT, compassMarkersRT;
    private static Transform markersTf;
    private static float compassWidth;

    private Camera mainCamera;
    private static GPS gps;

    private GameObject obstacleDisabled, poiDisabled, roverDisabled;
    private static Dictionary<MarkerType, GameObject> prefabDict;
    private Dictionary<MarkerType, bool> showMarker;
    private Dictionary<GameObject, Marker> markers;
    [HideInInspector] public MarkerActionMode mode;
    private MarkerType selectedMarkerType;
    private RectTransform currLocRT;
    private Marker currMarker;

    private GameObject actionButtons;
    private float buttonPressedTime;

    private Navigation navigation;
    private bool isNavigating;

    // Voice
    private GameObject voiceMemoObj;

    void Start()
    {
        mainCamera = Camera.main;
        gps = GameObject.Find("GPS").GetComponent<GPS>();
        mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        compassRT = GameObject.Find("Compass Image").GetComponent<RectTransform>();
        compassMarkersRT = GameObject.Find("Compass Markers").GetComponent<RectTransform>();
        markersTf = transform;
        markers = new Dictionary<GameObject, Marker>();
        prefabDict = new Dictionary<MarkerType, GameObject>
        {
            { MarkerType.Obstacle, Resources.Load<GameObject>("CustomPrefabs/Obstacle") },
            { MarkerType.Rover, Resources.Load<GameObject>("CustomPrefabs/Rover") },
            { MarkerType.POI,  Resources.Load<GameObject>("CustomPrefabs/POI") }
        };
        showMarker = new Dictionary<MarkerType, bool>
        {
            { MarkerType.Obstacle, true },
            { MarkerType.Rover, true },
            { MarkerType.POI, true }
        };
        currLocRT = GameObject.Find("CurrLoc").GetComponent<RectTransform>();
        compassWidth = compassRT.rect.width / 360.0f;
        actionButtons = GameObject.Find("Marker Action Buttons");
        actionButtons.SetActive(false);
        roverDisabled = GameObject.Find("Rover Disabled");
        obstacleDisabled = GameObject.Find("Obstacle Disabled");
        poiDisabled = GameObject.Find("POI Disabled");
        roverDisabled.SetActive(false);
        obstacleDisabled.SetActive(false);
        poiDisabled.SetActive(false);
        navigation = GameObject.Find("Navigation").GetComponent<Navigation>();
        voiceMemoObj = GameObject.Find("Voice Memo");
        voiceMemoObj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMarkers();
    }

    private void UpdateMarkers()
    {
        Vector2 userGps = gps.GetGpsCoords();
        Vector3 userLook = mainCamera.transform.forward;
        userLook.y = 0.0f;

		/************* Current Location ************/
		// Rotate
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        float mapRotZDeg = mapRT.localEulerAngles.z;
        currLocRT.localRotation = Quaternion.Euler(0, 0, mapRotZDeg - lookAngleZDeg);

        // Translate
        currLocRT.offsetMin = mapRT.offsetMin + gps.GpsToMapPos(userGps.x, userGps.y);
        currLocRT.offsetMax = currLocRT.offsetMin;
		/*********************************/

        foreach(var kvp in markers)
        {
            MarkerType type = kvp.Value.Type;

            kvp.Key.SetActive(showMarker[type]);
            if (!showMarker[type]) continue;

            kvp.Value.Update(userGps, userLook);
        }
    }

    public void HandleMarker(Vector2 touchCoord)
    {
        switch (mode)
        {
            case MarkerActionMode.Add:
                AddMarker(touchCoord);
                mode = MarkerActionMode.Edit;
                break;
            case MarkerActionMode.Edit:
                currMarker.GpsCoord = gps.MapPosToGps(touchCoord);
                break;
            case MarkerActionMode.Select:
                Vector3 pos = currMarker.MapMarkerObj.transform.position;
                pos.z -= 0.02f;
                pos.y -= 0.02f;
                actionButtons.transform.position = pos;
                break;
        }
    }

    public void HideActionButtons()
    {
        currMarker = null;
        actionButtons.SetActive(false);
        mode = MarkerActionMode.None;
    }

    public void HideVoiceMemo()
    {
        voiceMemoObj.SetActive(false);
        mode = MarkerActionMode.None;
    }

    private void AddMarker(Vector2 touchCoord)
    {
        currMarker = new Marker(selectedMarkerType, gps.MapPosToGps(touchCoord));
        currMarker.SetOpacity(0.5f);
        markers.Add(currMarker.MapMarkerObj, currMarker);
    }

    public void OnMapEnter(Vector2 touchCoord)
    {
        if (mode != MarkerActionMode.None) return;

        float minDist = markerEditSensitivity + 1;
        Vector2 touchGps = gps.MapPosToGps(touchCoord);

        foreach (var kvp in markers)
        {
            float dist = (kvp.Value.GpsCoord - touchGps).magnitude;
            if (dist < minDist)
            {
                minDist = dist;
                currMarker = kvp.Value;
            }
        }

        if (minDist < markerEditSensitivity)
        {
            actionButtons.SetActive(true);
            mode = MarkerActionMode.Select;
        }
        else
        {
            currMarker = null;
            mode = MarkerActionMode.None;
        }
    }

    public void OnMapExit()
    {
        if (mode == MarkerActionMode.Edit)
        {
            currMarker.SetOpacity(1f);
            mode = MarkerActionMode.None;
        }
    }

    // Button callbacks
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
        currMarker.SetOpacity(0.5f);
        actionButtons.SetActive(false);
        mode = MarkerActionMode.Edit;
    }

    public void OnMarkerDeletePressed()
    {
        if (navigation.destMarkerRT == currMarker.MapMarkerRT)
        {
            navigation.StopMarkerNavigate();
        }
        markers.Remove(currMarker.MapMarkerObj);
        currMarker.CleanUp();
        currMarker = null;

        actionButtons.SetActive(false);
        mode = MarkerActionMode.None;
    }

    public void OnMarkerNavigatePressed()
    {
        isNavigating = !isNavigating;
        if (isNavigating) navigation.StartMarkerNavigate(currMarker.MapMarkerRT);
        else navigation.StopMarkerNavigate();

        actionButtons.SetActive(false);
        mode = MarkerActionMode.None;
    }

    public void OnMarkerVoiceMemoPressed()
    {
        voiceMemoObj.SetActive(true);
        actionButtons.SetActive(false);
        mode = MarkerActionMode.Memo;
    }

    public void OnMarkerSelectExit()
    {
        if (Time.time - buttonPressedTime > 0.5f)
        {
            mode = MarkerActionMode.Add;
        }
        else
        {
            switch (selectedMarkerType)
            {
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


}
