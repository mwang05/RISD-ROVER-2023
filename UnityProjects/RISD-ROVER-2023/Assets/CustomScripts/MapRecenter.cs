using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRecenter : MonoBehaviour
{
    private RectTransform _mapRT;

    void Awake()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
    }

    // Callback: Recenter
    public void CenterMapAtUser()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;

        Vector3 userPos = Camera.main.transform.position;
        Vector3 userLook = Camera.main.transform.forward;

        // Rotate map so that curloc points up
        userLook.y = 0.0f;
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        _mapRT.localRotation = Quaternion.Euler(0.0f, 0.0f, lookAngleZDeg);

        // Convert userPos to map RT offset
        // Note: userPos.xz gives offsets in rotated MAP SPACE,
        //       but we must compute offsets in PANEL SPACE

        // User pos in map space, with rotation of map (xz components)
        Vector3 userPosMapspace = userPos * scaleW2M;
        // Rotate userPosMapspace back to get coords in unrotated map coords (xz components)
        Vector3 userPosMapspaceUnrot = Quaternion.Euler(0.0f, -lookAngleZDeg, 0.0f) * userPosMapspace;

        _mapRT.offsetMin = -new Vector2(userPosMapspaceUnrot.x, userPosMapspaceUnrot.z);
        _mapRT.offsetMax = _mapRT.offsetMin;
    }

    void Update()
    {
        #if false
        if (true)
        {
            CenterMapAtUser();
        }
        #endif
    }
}

