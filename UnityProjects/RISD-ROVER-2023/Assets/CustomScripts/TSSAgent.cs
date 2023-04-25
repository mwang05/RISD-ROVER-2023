using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS;

public class TSSAgent : MonoBehaviour
{
    TSSConnection tss;
    string tssUri = "ws://10.1.77.194";
    private bool isConnecting = false;
    private bool connected = false;
    int msgCount = 0;

    private GameObject connectMsg;

    // Start is called before the first frame update
    async void Start()
    {
        tss = new TSSConnection();
        connectMsg = GameObject.Find("Connecting");
        // Connect();
    }

    // Update is called once per frame
    void Update()
    {

        // Updates the websocket once per frame
        // if (connected) tss.Update();
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
        var connecting = tss.ConnectToURI(tssUri);
        // Create a function that takes asing TSSMsg parameter and returns void. For example:
        // public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg) { ... }
        // Then just subscribe to the OnTSSTelemetryMsg
        tss.OnTSSTelemetryMsg += (telemMsg) =>
        {
            msgCount++;
            Debug.Log("Message #" + msgCount + "\nMessage:\n " + JsonUtility.ToJson(telemMsg, prettyPrint: true));

            // Do some thing with each type of message (get using telemMsg.MESSAGE[0])
            if (telemMsg.GPS.Count > 0)
            {
            }

            if (telemMsg.IMU.Count > 0)
            {
            }

            if (telemMsg.EVA.Count > 0)
            {
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
}
