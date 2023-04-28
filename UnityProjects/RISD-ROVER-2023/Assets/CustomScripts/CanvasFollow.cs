using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasFollow : MonoBehaviour
{
    // Canvas follow
    [SerializeField] private float distanceFromUser = 0.5f;
    private Transform _canvasTf;
    private Camera _mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        _canvasTf = GetComponent<RectTransform>().transform;
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Follow();
    }

    private void Follow()
    {
        Transform cameraTf = _mainCamera.transform;
        Vector3 userPos = cameraTf.position;
        Vector3 userLook = cameraTf.forward;

        userLook.y = 0;
        userLook = Vector3.Normalize(userLook);

        _canvasTf.position = userPos + distanceFromUser * userLook;
        _canvasTf.rotation = cameraTf.rotation;
    }
}
