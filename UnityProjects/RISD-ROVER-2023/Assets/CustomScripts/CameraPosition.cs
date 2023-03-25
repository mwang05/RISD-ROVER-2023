using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UX;

public class CameraPosition : MonoBehaviour
{
    private VerticalLayoutGroup _verticalLayoutGroup;
    private RectTransform _mapRT;
    private RectTransform _curlocRT;

    void Start()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
        _verticalLayoutGroup = GameObject.Find("Curloc").GetComponent<VerticalLayoutGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        float scale = 100.0f * _mapRT.localScale.x;

        Vector3 worldPos = Camera.main.transform.position;

        _curlocRT.offsetMin = new Vector2(scale * worldPos.x + _mapRT.offsetMin.x, scale * worldPos.z + _mapRT.offsetMin.y);
        _curlocRT.offsetMax = _curlocRT.offsetMin;

        Vector3 userLook = Camera.main.transform.forward;
        userLook.y = 0;
        float angle = Vector3.Angle(Vector3.forward, userLook);
        angle = userLook.x > 0 ? -angle : angle;
        _curlocRT.localRotation = Quaternion.Euler(0, 0, angle);
    }

}