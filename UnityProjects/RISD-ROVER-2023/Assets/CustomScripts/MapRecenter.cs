using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum MapFocusMode
{
    MapNoFocus,
    MapCenterUser,
    MapAlignUser,
    NumMapFocusModes,
}

public class MapRecenter : MonoBehaviour
{
    private RectTransform _mapRT;
    private MapFocusMode _focusMode;
    private float _mapLastRotZDeg;

    void Awake()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _focusMode = MapFocusMode.MapNoFocus;
        _mapLastRotZDeg = 0.0f;
    }

    private void RotateMapWithUser()
    {
        Vector3 userLook = Camera.main.transform.forward;

        // Rotate map so that curloc points up
        userLook.y = 0.0f;
        float lookAngleZDeg = Vector3.Angle(Vector3.forward, userLook) * Mathf.Sign(userLook.x);
        _mapRT.localRotation = Quaternion.Euler(0.0f, 0.0f, lookAngleZDeg);
    }

    private void CenterMapAtUser()
    {
        // Convert userPos to mapRT offsets
        // Note: userPos.xz gives offsets in ROTATED MAP SPACE,
        //       but we must compute offsets in PANEL SPACE
        float scaleW2M = 100.0f * _mapRT.localScale.x;
        float mapRotZDeg = _mapRT.localEulerAngles.z;

        Vector3 userPos = Camera.main.transform.position;

        // User pos in map space, with rotation of map (xz components)
        Vector3 userPosMapspace = userPos * scaleW2M;
        // Rotate userPosMapspace back to get coords in unrotated map coords (xz components)
        Vector3 userPosMapspaceUnrot = Quaternion.Euler(0.0f, -mapRotZDeg, 0.0f) * userPosMapspace;

        _mapRT.offsetMin = -new Vector2(userPosMapspaceUnrot.x, userPosMapspaceUnrot.z);
        _mapRT.offsetMax = _mapRT.offsetMin;
    }

    // Callback: Recenter
    private void AlignMapWithUser()
    {
        RotateMapWithUser();
        CenterMapAtUser();
    }

    private void MapToggleFocusMode()
    {
        int newMode = ((int)_focusMode + 1) % (int)MapFocusMode.NumMapFocusModes;
        _focusMode = (MapFocusMode)newMode;
        Debug.Log(_focusMode);
    }

    private void MapStoreLastRotation()
    {
        _mapLastRotZDeg = _mapRT.localEulerAngles.z;
    }

    private void MapRestoreLastRotation()
    {
        _mapRT.localRotation = Quaternion.Euler(0, 0, _mapLastRotZDeg);
    }

    public void MapFocusCallback()
    {
        MapToggleFocusMode();
        switch (_focusMode)
        {
            case MapFocusMode.MapNoFocus:
                MapRestoreLastRotation();
                CenterMapAtUser();
                break;
            case MapFocusMode.MapCenterUser:
                CenterMapAtUser();
                break;
            case MapFocusMode.MapAlignUser:
                MapRestoreLastRotation();
                break;
        }
    }

    void Update()
    {
        #if true
        if (_focusMode == MapFocusMode.MapAlignUser)
        {
            AlignMapWithUser();
        }
        #endif
    }
}

