using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UX;

public class MapFollow : MonoBehaviour
{
    [SerializeField] private float distanceFromUser;
    private Transform _canvasTf;
    private Camera _mainCamera;

    void Awake()
    {
        _canvasTf = GameObject.Find("Canvas").GetComponent<RectTransform>().transform;
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Transform cameraTf = _mainCamera.transform;
        Vector3 userPos = cameraTf.position;
        Debug.Log(userPos);
        Vector3 userLook = cameraTf.forward;

        userLook.y = 0;
        userLook = Vector3.Normalize(userLook);

        _canvasTf.position = userPos + distanceFromUser * userLook;
        _canvasTf.rotation = cameraTf.rotation;
    }
}
