using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UX;

public class MapFollow : MonoBehaviour
{
    [SerializeField] private float distanceFromUser;
    private RectTransform _canvasRT;
    private RectTransform _canvasRT_Menu;
    private RectTransform _canvasRT_PosBtn;

    // Start is called before the first frame update
    void Start()
    {
        _canvasRT = GameObject.Find("Map Canvas").GetComponent<RectTransform>();
        // Menu Canvas
        // _canvasRT_Menu = GameObject.Find("Menu Canvas").GetComponent<RectTransform>();
        // Pos Btn Canvas
        // _canvasRT_PosBtn = GameObject.Find("Pos Btn Canvas").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 userPos = Camera.main.transform.position;
        Vector3 userLook = Camera.main.transform.forward;
        
        userLook.y = 0;
        userLook = Vector3.Normalize(userLook);
        userLook.y = -0.75f;

        _canvasRT.transform.position = userPos + distanceFromUser * userLook;
        _canvasRT.transform.rotation = Camera.main.transform.rotation;
        /*
        if(_canvasRT_Menu != null)
        {
            _canvasRT_Menu.transform.position = userPos + distanceFromUser * userLook;
            _canvasRT_Menu.transform.rotation = Camera.main.transform.rotation;
        }
        if(_canvasRT_PosBtn != null)
        {
            _canvasRT_PosBtn.transform.position = userPos + distanceFromUser * userLook;
            _canvasRT_PosBtn.transform.rotation = Camera.main.transform.rotation;
        }
        */
    }
}
