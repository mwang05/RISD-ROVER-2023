using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScreenspaceCanvas : MonoBehaviour
{
    // Screenspace Canvas

    // Set the distance slighter larger than the near plane of the camera
    [SerializeField] private float distanceFromUser = 0.31f;
    private Transform _canvasTf, _cameraTf;

    private GameObject _systemMessage, _countdown;
    private TMPro.TMP_Text _systemMessageText, _countdownText;

    public void DisplayMessage(String msg, int nsecs)
    {
        _systemMessage.SetActive(true);
        _systemMessageText.text = msg;
        // TODO: wait for nsecs, then HideMessage
        
    }

    public void HideMessage()
    {
        _systemMessage.SetActive(false);
    }

    public void DisplayCountdown(String s)
    {
        _countdown.SetActive(true);
        _countdownText.text = s;
    }

    public void HideCountdown()
    {
        _countdown.SetActive(false);
    }

    void Awake()
    {
        _canvasTf = GetComponent<RectTransform>().transform;
        _cameraTf = Camera.main.transform;

        _systemMessage = GameObject.Find("SS SysMsg");
        _systemMessageText = GameObject.Find("SS SysMsg Text").GetComponent<TMPro.TMP_Text>();

        _countdown = GameObject.Find("SS Countdown");
        _countdownText = _countdown.GetComponent<TMPro.TMP_Text>();
    }

    // Start is called before the first frame update
    void Start()
    {
        HideMessage();
        HideCountdown();
    }

    // Update is called once per frame
    void Update()
    {
        SSCanvasDoFollow();
    }

    private void SSCanvasDoFollow()
    {
        Vector3 userPos = _cameraTf.position;
        Vector3 userLook = _cameraTf.forward;

        userLook = Vector3.Normalize(userLook);

        _canvasTf.position = userPos + distanceFromUser * userLook;
        _canvasTf.rotation = _cameraTf.rotation;
    }

}
