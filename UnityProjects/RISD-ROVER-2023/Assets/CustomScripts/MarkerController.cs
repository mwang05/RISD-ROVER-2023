using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerController : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private GameObject compassMarkerPrefab;

    private List<(Vector3, RectTransform, RectTransform)> markers;
    private RectTransform _mapRT;
    // private RectTransform _curlocRT;

    // Start is called before the first frame update
    void Start()
    {
        // Each marker is a (worldPos, mapRT, compassRT) triple
        markers = new List<(Vector3, RectTransform, RectTransform)>();
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        // _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;

        foreach(var item in markers)
        {
            // GameObject marker = item.Item1;
            Vector3 posWorldspace = item.Item1;    // mark pos in world space
            RectTransform rtMap = item.Item2;
            RectTransform rtCompass = item.Item3;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            // Marker pos in map space, with rotation of map (xz components)
            Vector3 posMapspace = posWorldspace * scaleW2M;
            // Rotate posMapspace back to get coords in unrotated map coords (xz components)
            Vector3 posMapspaceUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * posMapspace;

            rtMap.offsetMin = _mapRT.offsetMin + new Vector2(posMapspaceUnrot.x, posMapspaceUnrot.z);
            rtMap.offsetMax = rtMap.offsetMin;


            // Adjust marker position on compass

        }
    }

    public void AddMarker()
    {
        GameObject markerOnMap = Instantiate(markerPrefab, transform);
        // GameObject markerOnCompass = Instantiate(compassMarkerPrefab, transform);
        markers.Add((Camera.main.transform.position,
                     markerOnMap.GetComponent<RectTransform>(),
                     null));
                     // markerOnCompass.GetComponent<RectTransform>()));
                     // markerOnMap.GetComponent<RectTransform>()));
    }
}
