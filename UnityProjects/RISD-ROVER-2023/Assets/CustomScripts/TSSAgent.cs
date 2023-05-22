using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS;
using TSS.Msgs;

public class TSSAgent : MonoBehaviour
{
    // TSS related information
    TSSConnection tss;
    private const string tssUri = "ws://192.168.50.10:3001";
    private const string team_name = "RISD";
    private const string username = "VK05";
    private const string university = "Rhode Island School of Design";
    private const string user_guid = "cab500cc-d4ab-4ddc-98e4-780bd720a30c";


    private bool isConnecting = false;
    private bool connected = false;

    private MarkerController markerController;

    private GameObject eva;
    private EVAController evaController;

    private GameObject egress;
    private NewEgressController egressController;
    private GPS gps;
    private bool firstConnect;

    private GameObject connectMsg;

    // Start is called before the first frame update
    void Awake()
    {
        markerController = GameObject.Find("Markers").GetComponent<MarkerController>();
        eva = GameObject.Find("EVA");
        evaController = eva.GetComponent<EVAController>();
        connectMsg = GameObject.Find("Connecting");
        egress = GameObject.Find("New Egress");
        egressController = egress.GetComponent<NewEgressController>();
        gps = GameObject.Find("GPS").GetComponent<GPS>();
    }

    void Start()
    {
        tss = new TSSConnection();
        connectMsg.SetActive(false);
        egress.SetActive(false);
        eva.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!firstConnect)
        {
            firstConnect = true;
            Connect();
        }
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
        // isConnecting = true;
        connectMsg.SetActive(true);
        var connecting = tss.ConnectToURI(tssUri, team_name, username, university, user_guid);
        // Create a function that takes asing TSSMsg parameter and returns void. For example:
        // public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg) { ... }
        // Then just subscribe to the OnTSSTelemetryMsg
        tss.OnTSSTelemetryMsg += (telemMsg) =>
        {
            // Do some thing with each type of message (get using telemMsg.MESSAGE[0])
            if (telemMsg.gpsMsg.lat != 0)
            {
                gps.UpdateUserGps(new Vector2(telemMsg.gpsMsg.lat, telemMsg.gpsMsg.lon));
            }

            if (telemMsg.roverMsg.lat != 0)
            {
                markerController.SetRoverLocation(new Vector2(telemMsg.gpsMsg.lat, telemMsg.gpsMsg.lon));
            }

            if (telemMsg.simulationStates.battery_capacity != 0)
            {
                evaController.EVAMsgUpdateCallback(telemMsg.simulationStates);
            }

            var uia = telemMsg.uiaMsg;
            bool validUia = uia.depress_pump_switch || uia.emu1_o2_supply_switch || uia.ev1_supply_switch ||
                              uia.emu1_pwr_switch || uia.emu1_water_waste || uia.o2_vent_switch;

            if (validUia && egress.activeSelf)
            {
                egressController.UIAMsgUpdateCallback(telemMsg.uiaMsg);
            }

            if (telemMsg.specMsg.CaO != 0)
            {
                Debug.Log(telemMsg.specMsg.CaO);
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
            // egress.SetActive(true);
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

    public void SendRoverMoveCommand(Vector2 loc)
    {
        Debug.Log("Send rover to " + loc);
        tss.SendRoverNavigateCommand(loc.x, loc.y);
    }

    // An example handler for the OnTSSMsgReceived event which just serializes to JSON and prints it all out
    // Can be any function that returns void and has a single parameter of type TSS.Msgs.TSSMsg
    public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg)
    {
        Debug.Log("Received the following telemetry data from the TSS:\n" + JsonUtility.ToJson(tssMsg, prettyPrint: true));
    }
}
