using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerController : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;

    private List<(GameObject, Vector3, RectTransform)> markers;
    private RectTransform _mapRT;
    private RectTransform _curlocRT;

    // Start is called before the first frame update
    void Start()
    {
        markers = new List<(GameObject, Vector3, RectTransform)>();
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;

        foreach(var item in markers)
        {
            // GameObject marker = item.Item1;
            Vector3 pos = item.Item2;    // mark pos in world space
            RectTransform rt = item.Item3;

            // Translate (offset) markers relative to map RT
            // Note: pos gives offsets in rotated MAP SPACE,
            //       but we must compute offsets in PANEL SPACE

            // Marker pos in map space, with rotation of map (xz components)
            Vector3 posMapspace = pos * scaleW2M;
            // Rotate posMapspace back to get coords in unrotated map coords (xz components)
            Vector3 posMapspaceUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * posMapspace;

            rt.offsetMin = _mapRT.offsetMin + new Vector2(posMapspaceUnrot.x, posMapspaceUnrot.z);
            rt.offsetMax = rt.offsetMin;
        }
    }

    public void AddMarker()
    {
        GameObject obj = Instantiate(markerPrefab, transform);
        markers.Add((obj, Camera.main.transform.position, obj.GetComponent<RectTransform>()));
    }
}
