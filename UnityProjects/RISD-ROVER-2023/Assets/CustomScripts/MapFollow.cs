using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UX;

public class MapFollow : MonoBehaviour
{
    [SerializeField] private float distanceFromUser;
    private RectTransform _canvasRT;

    void Awake()
    {
        _canvasRT = GameObject.Find("Map Canvas").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 userPos = Camera.main.transform.position;
        Vector3 userLook = Camera.main.transform.forward;

        userLook.y = 0.0f;
        userLook = Vector3.Normalize(userLook);

        _canvasRT.transform.position = userPos + distanceFromUser * userLook;
        _canvasRT.transform.rotation = Camera.main.transform.rotation;
    }
}
