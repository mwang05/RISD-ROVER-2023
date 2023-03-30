using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UX;

public class CameraPosition : MonoBehaviour
{
    // private VerticalLayoutGroup _verticalLayoutGroup;
    private RectTransform _mapRT;
    private RectTransform _curlocRT;
    public float lookAngleWithFront;  // dot product <look, vec3.front>

    void Start()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
        // _verticalLayoutGroup = GameObject.Find("Curloc").GetComponent<VerticalLayoutGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        Vector3 userPos = Camera.main.transform.position;
        Vector3 userLook = Camera.main.transform.forward;

        // Translate (offset) curLoc icon relative to map RT
        _curlocRT.offsetMin = _mapRT.offsetMin + new Vector2(userPos.x, userPos.z) * scaleW2M;
        _curlocRT.offsetMax = _curlocRT.offsetMin;

        // Rotate curLoc icon
        userLook.y = 0;
        lookAngleWithFront = Vector3.Angle(Vector3.forward, userLook) * -Mathf.Sign(userLook.x);
        Debug.Log(lookAngleWithFront);

        // float angle = userLook.x > 0 ? -lookAngleWithFront : lookAngleWithFront;
        // _curlocRT.localRotation = Quaternion.Euler(0, 0, angle);
        _curlocRT.localRotation = Quaternion.Euler(0, 0, lookAngleWithFront);
    }

}
