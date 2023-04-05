using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UX;

public class CameraPosition : MonoBehaviour
{
    private RectTransform _mapRT;
    private RectTransform _curlocRT;

    private RectTransform _canvasRT;

    // Satellite info
    // hard coded center
    private const float satCenterLatitude  = 29.564575f;   // latitude at the center of the satellite image, in degree
    private const float satCenterLongitude = -95.081164f;  // longitude at the center of the satellite image, in degree
    // hard coded scale
    private const float satLatitudeRange = 0.002216f;  // the satellite image covers this much latitudes in degree
    private const float satLongitudeRange = 0.00255f;  // the satellite image covers this much longitudes in degree

    void Awake()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
        _canvasRT = GameObject.Find("Canvas").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate curLoc icon
        Vector3 userLook = Camera.main.transform.forward;
        userLook.y = 0.0f;
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        float mapRotZDeg = _mapRT.localEulerAngles.z;
        _curlocRT.localRotation = Quaternion.Euler(0, 0, mapRotZDeg - lookAngleZDeg);

        // Translate curLoc icon
        Vector2 gpsCoords = getGPSCoords();
        _curlocRT.offsetMin = _mapRT.offsetMin + GPSToMapPos(gpsCoords.x, gpsCoords.y);
        _curlocRT.offsetMax = _curlocRT.offsetMin;
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

    // For simulation in Unity
    private Vector2 getGPSCoords()
    {
        Vector3 worldPos = Camera.main.transform.position;
        Vector2 gpsCoords = new Vector2(satCenterLatitude, satCenterLongitude);
        gpsCoords += 0.01f * new Vector2(worldPos.z, worldPos.x);
        return gpsCoords;
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

}
