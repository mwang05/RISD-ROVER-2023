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

    void Awake()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        Vector3 userPos = Camera.main.transform.position;
        Vector3 userLook = Camera.main.transform.forward;

        Debug.Log(userLook);

        float mapRotZDeg = _mapRT.localEulerAngles.z;

        // Rotate curLoc icon
        userLook.y = 0.0f;
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        _curlocRT.localRotation = Quaternion.Euler(0, 0, mapRotZDeg - lookAngleZDeg);

        // Translate (offset) curLoc icon relative to map RT
        // Note: userPos.xz gives offsets in rotated MAP SPACE,
        //       but we must compute offsets in PANEL SPACE

        // User pos in map space, with rotation of map (xz components)
        Vector3 userPosMapspace = userPos * scaleW2M;
        // Rotate userPosMapspace back to get coords in unrotated map coords (xz components)
        Vector3 userPosMapspaceUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * userPosMapspace;

        _curlocRT.offsetMin = _mapRT.offsetMin + new Vector2(userPosMapspaceUnrot.x, userPosMapspaceUnrot.z);
        _curlocRT.offsetMax = _curlocRT.offsetMin;
    }

}
