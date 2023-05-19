using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS;
using TSS.Msgs;

public class TSSAgent : MonoBehaviour
{
    TSSConnection tss;
    string tssUri = "ws://192.168.50.10:3001";
    private bool isConnecting = false;
    private bool connected = false;
    private int gpsMsgCount, imuMsgCount, evaMsgCount;

    private MarkerController markerController;

    private GameObject EVA;
    private TMPro.TMP_Text batteryPercentageText;

    private GameObject connectMsg;

    // Start is called before the first frame update
    async void Start()
    {
        tss = new TSSConnection();
        markerController = GameObject.Find("Markers").GetComponent<MarkerController>();
        EVA = GameObject.Find("EVA");
        batteryPercentageText = GameObject.Find("Battery Capacity").GetComponent<TMPro.TMP_Text>();
        connectMsg = GameObject.Find("Connecting");
        
        connectMsg.SetActive(false);
        EVA.SetActive(false);
        // Connect();
    }

    // Update is called once per frame
    void Update()
    {

        // Updates the websocket once per frame
        if (connected) tss.Update();
        // else if (!isConnecting)
        // {
        //     isConnecting = true;
        //     connectMsg.SetActive(true);
        //     Connect();
        // }
    }

    public async void Connect()
    {
        isConnecting = true;
        connectMsg.SetActive(true);
        var connecting = tss.ConnectToURI(tssUri);
        // Create a function that takes asing TSSMsg parameter and returns void. For example:
        // public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg) { ... }
        // Then just subscribe to the OnTSSTelemetryMsg
        tss.OnTSSTelemetryMsg += (telemMsg) =>
        {
            // Do some thing with each type of message (get using telemMsg.MESSAGE[0])
            if (telemMsg.GPS.Count != gpsMsgCount)
            {
                UpdateGPS(telemMsg.GPS[gpsMsgCount]);
                gpsMsgCount = telemMsg.GPS.Count;
            }

            if (telemMsg.IMU.Count != imuMsgCount)
            {
            }

            if (telemMsg.EVA.Count != evaMsgCount)
            {
                // UpdateEVA(telemMsg.EVA[evaMsgCount]);
                evaMsgCount = telemMsg.EVA.Count;
            }
        };

        // tss.OnOpen, OnError, and OnClose events just re-raise events from websockets.
        // Similar to OnTSSTelemetryMsg, create functions with the appropriate return type and parameters, and subscribe to them
        tss.OnOpen += () =>
        {
            Debug.Log("Websocket connection opened");
            connected = true;
            isConnecting = false;
            connectMsg.SetActive(false);
        };

        tss.OnError += (string e) =>
        {
            Debug.Log("Websocket error occured: " + e);
            connected = false;
            isConnecting = false;
            connectMsg.SetActive(false);
        };

        tss.OnClose += (e) =>
        {
            Debug.Log("Websocket closed with code: " + e);
            connected = false;
            isConnecting = false;
        };

        await connecting;

    }

    // An example handler for the OnTSSMsgReceived event which just serializes to JSON and prints it all out
    // Can be any function that returns void and has a single parameter of type TSS.Msgs.TSSMsg
    public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg)
    {
        Debug.Log("Received the following telemetry data from the TSS:\n" + JsonUtility.ToJson(tssMsg, prettyPrint: true));
    }

    private void UpdateEVA(TSS.Msgs.TSSMsg tssMsg)
    {
        if (!EVA.activeSelf) return;

        var eva = tssMsg.EVA[tssMsg.EVA.Count - 1];
        
        batteryPercentageText.text = string.Format("{0:0.00}", eva.batteryPercent);
        // batteryPercentageText.text = string.Format("{0:0.00}", eva.batteryPercent);
    }

    private void UpdateGPS(GPSMsg msg)
    {
    }
}
