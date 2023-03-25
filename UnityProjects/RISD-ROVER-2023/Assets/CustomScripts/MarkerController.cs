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
        float scale = 100.0f * _mapRT.localScale.x;

        foreach(var item in markers)
        {
            GameObject marker = item.Item1;
            Vector3 pos = item.Item2;
            RectTransform rt = item.Item3;

            rt.offsetMin = new Vector2(scale * pos.x + _mapRT.offsetMin.x, scale * pos.z + _mapRT.offsetMin.y);
            rt.offsetMax = rt.offsetMin;
        }
    }

    public void AddMarker()
    {
        GameObject obj = Instantiate(markerPrefab, transform);
        markers.Add((obj, Camera.main.transform.position, obj.GetComponent<RectTransform>()));
    }
}
