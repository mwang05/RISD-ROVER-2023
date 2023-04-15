using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerController : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private GameObject compassMarkerPrefab;

    private List<(Vector2, RectTransform, RectTransform)> _markers;
    private RectTransform _mapRT;
    private RectTransform _compassRT, _compassMarkersRT;
    private RectTransform _canvasRT;

    // private RectTransform _curlocRT;
    // private GameObject _compassObj, _compassImgObj;

    // Satellite info
    // hard coded center
    private const float satCenterLatitude  = 29.564575f;   // latitude at the center of the satellite image, in degree
    private const float satCenterLongitude = -95.081164f;  // longitude at the center of the satellite image, in degree
    // hard coded scale
    private const float satLatitudeRange = 0.002216f;  // the satellite image covers this much latitudes in degree
    private const float satLongitudeRange = 0.00255f;  // the satellite image covers this much longitudes in degree

    // Start is called before the first frame update
    void Start()
    {
        // Each marker is a (gpsCoords, mapRT, compassRT) triple
        _markers = new List<(Vector2, RectTransform, RectTransform)>();
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _canvasRT = GameObject.Find("Canvas").GetComponent<RectTransform>();
        _compassRT = GameObject.Find("Compass Image").GetComponent<RectTransform>();
        _compassMarkersRT = GameObject.Find("Compass Markers").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        return;
        // float scaleW2M = 100.0f * _mapRT.localScale.x;
        // float mapRotZDeg = _mapRT.localEulerAngles.z;
        float compassWidth = _compassRT.rect.width / 360.0f;

        // Vector3 userPos = Camera.main.transform.position;
        Vector2 userGPS = getGPSCoords();
        Vector3 userLook = Camera.main.transform.forward;
        userLook.y = 0.0f;

        foreach(var item in _markers)
        {
            // Vector3 posWorldspace = item.Item1;    // marker pos in world space
            Vector2 markerGPS = item.Item1;    // marker's GPS coords
            RectTransform rtMap = item.Item2;
            RectTransform rtCompass = item.Item3;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            // Marker pos in map space, with rotation of map (xz components)
            // Vector3 posMapspace = posWorldspace * scaleW2M;
            // Rotate posMapspace back to get coords in unrotated map coords (xz components)
            // Vector3 posMapspaceUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * posMapspace;

            // rtMap.offsetMin = _mapRT.offsetMin + new Vector2(posMapspaceUnrot.x, posMapspaceUnrot.z);
            rtMap.offsetMin = _mapRT.offsetMin + GPSToMapPos(markerGPS.x, markerGPS.y);
            rtMap.offsetMax = rtMap.offsetMin;

            // Adjust marker position on compass
            // Given userGPS and markerGPS, get markerDir that points from user to marker
            // Vector3 markerDir = posWorldspace - userPos;
            // markerDir.y = 0.0f;
            Vector2 markerRelGPS = markerGPS - userGPS;
            Vector3 markerDir = new Vector3(markerRelGPS.x, 0.0f, markerRelGPS.y);
            float angleToMarker = Vector3.SignedAngle(markerDir, userLook, Vector3.up);
            rtCompass.offsetMin = new Vector2(angleToMarker * compassWidth, 0.0f);
            rtCompass.offsetMax = rtCompass.offsetMin;
        }
    }

    // public void AddMarker()
    // {
    //     GameObject markerOnMap = Instantiate(markerPrefab, transform);
    //     GameObject markerOnCompass = Instantiate(compassMarkerPrefab, _compassMarkersRT);
    //     Debug.Log(_compassMarkersRT);
    //     // _markers.Add((Camera.main.transform.position,
    //     _markers.Add((getGPSCoords(),
    //                  markerOnMap.GetComponent<RectTransform>(),
    //                  markerOnCompass.GetComponent<RectTransform>()));
    // }

    private Vector2 GPSToMapPos(float latitudeDeg, float longitudeDeg)
    {
        float du = (longitudeDeg - satCenterLongitude) / satLongitudeRange;  // -.5 ~ +.5 in horizontal map space
        float dv = (latitudeDeg - satCenterLatitude) / satLatitudeRange;     // -.5 ~ +.5 in vertical map sapce

        float mapRotZDeg = _mapRT.localEulerAngles.z;
        Vector3 mapPos = new Vector3(du, 0, dv) * _mapRT.localScale.x * _canvasRT.rect.height;
        mapPos = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * mapPos;

        return new Vector2(mapPos.x, mapPos.z);
    }

    // For simulation in Unity
    private Vector2 getGPSCoords()
    {
        Vector3 worldPos = Camera.main.transform.position;
        Vector2 gpsCoords = new Vector2(satCenterLatitude, satCenterLongitude);
        gpsCoords += 0.001f * new Vector2(worldPos.z, worldPos.x);
        return gpsCoords;
    }

}
