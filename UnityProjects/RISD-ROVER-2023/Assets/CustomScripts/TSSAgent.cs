using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS.Msgs;
using TSS;

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

    private GameObject mainPanel;
    private MarkerController markerController;
    private NotificationController notificationController;

    private GameObject eva;
    private EVAController evaController;

    private GameObject egress;
    private NewEgressController egressController;
    private GPS gps;
    private bool firstConnect;

    private GPSMsg prevGPSMsg = new GPSMsg();
    private RoverMsg prevRoverMsg = new RoverMsg();
    private SimulationStates prevSimStates = new SimulationStates();
    private UIAMsg prevUIAMsg = new UIAMsg();
    private UIAState prevUIAState = new UIAState();
    private SpecMsg prevSpecMsg = new SpecMsg();

    // Start is called before the first frame update
    void Awake()
    {
        mainPanel = GameObject.Find("Main Panel");
        markerController = GameObject.Find("Markers").GetComponent<MarkerController>();
        notificationController = GameObject.Find("Notifications").GetComponent<NotificationController>();
        eva = GameObject.Find("EVA");
        evaController = eva.GetComponent<EVAController>();
        egress = GameObject.Find("New Egress");
        egressController = egress.GetComponent<NewEgressController>();
        gps = GameObject.Find("GPS").GetComponent<GPS>();
    }

    void Start()
    {
        tss = new TSSConnection();
        mainPanel.SetActive(false);
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
        else if (!isConnecting)
        {
            Connect();
        }
    }

    public async void Connect()
    {
        isConnecting = true;
        notificationController.PushSystemMessage("Connecting to the TSS server", 300);
        var connecting = tss.ConnectToURI(tssUri, team_name, username, university, user_guid);
        // Create a function that takes asing TSSMsg parameter and returns void. For example:
        // public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg) { ... }
        // Then just subscribe to the OnTSSTelemetryMsg
        tss.OnTSSTelemetryMsg += (telemMsg) =>
        {
            // Do some thing with each type of message
            if (IsValidGPSMsg(telemMsg.gpsMsg))
            {
                gps.UpdateUserGps(new Vector2(telemMsg.gpsMsg.lat, telemMsg.gpsMsg.lon));
            }

            if (IsValidRoverMsg(telemMsg.roverMsg))
            {
                markerController.SetRoverLocation(new Vector2(telemMsg.gpsMsg.lat, telemMsg.gpsMsg.lon));
            }

            if (IsValidSimStates(telemMsg.simulationStates))
            {
                evaController.EVAMsgUpdateCallback(telemMsg.simulationStates);
                prevSimStates = telemMsg.simulationStates;
            }

            if (IsValidUIAMsg(telemMsg.uiaMsg))
            {
                egressController.UIAMsgUpdateCallback(telemMsg.uiaMsg);
                prevUIAMsg = telemMsg.uiaMsg;
            }

            if (IsValidUIAState(telemMsg.uiaState))
            {
                egressController.UIAStateUpdateCallback(telemMsg.uiaState);
                prevUIAState = telemMsg.uiaState;
            }

            // Spec
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
            notificationController.PushSystemMessage("TSS connection established", 3);
            egress.SetActive(true);
        };

        tss.OnError += (string e) =>
        {
            Debug.Log("Websocket error occured: " + e);
            connected = false;
            isConnecting = false;
            notificationController.PushSystemMessage("TSS connection error", 3);
        };

        tss.OnClose += (e) =>
        {
            Debug.Log("Websocket closed with code: " + e);
            connected = false;
            isConnecting = false;
            notificationController.PushSystemMessage("TSS connection closed", 3);
        };

        await connecting;

    }

    private bool IsValidGPSMsg(GPSMsg msg)
    {
        return !(msg.lat == prevGPSMsg.lat && msg.lon == prevGPSMsg.lon);
    }

    private bool IsValidRoverMsg(RoverMsg msg)
    {
        return !(msg.lat == prevRoverMsg.lat && msg.lon == prevRoverMsg.lon);
    }

    private bool IsValidSimStates(SimulationStates states)
    {
        return states.battery_percentage != 0;
    }

    private bool IsValidUIAMsg(UIAMsg msg)
    {
        return !(
            msg.emu1_pwr_switch == prevUIAMsg.emu1_pwr_switch &&
            msg.ev1_supply_switch == prevUIAMsg.ev1_supply_switch &&
            msg.emu1_water_waste == prevUIAMsg.emu1_water_waste &&
            msg.emu1_o2_supply_switch == prevUIAMsg.emu1_o2_supply_switch &&
            msg.o2_vent_switch == prevUIAMsg.o2_vent_switch &&
            msg.depress_pump_switch == prevUIAMsg.depress_pump_switch
        );
    }

    public bool IsValidUIAState(UIAState state)
    {
        return !(
            state.emu1_is_booted == prevUIAState.emu1_is_booted &&
            state.uia_supply_pressure == prevUIAState.uia_supply_pressure &&
            state.water_level == prevUIAState.water_level &&
            state.airlock_pressure == prevUIAState.airlock_pressure &&
            state.depress_pump_fault == prevUIAState.depress_pump_fault
        );
    }

    public void SendRoverMoveCommand(Vector2 loc)
    {
        Debug.Log("Send rover to " + loc);
        tss.SendRoverNavigateCommand(loc.x, loc.y);
    }

    public void RoverRecallCommand()
    {
        Debug.Log("Recall rover to the user's location");
        tss.SendRoverRecallCommand();
    }

    // An example handler for the OnTSSMsgReceived event which just serializes to JSON and prints it all out
    // Can be any function that returns void and has a single parameter of type TSS.Msgs.TSSMsg
    public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg)
    {
        Debug.Log("Received the following telemetry data from the TSS:\n" + JsonUtility.ToJson(tssMsg, prettyPrint: true));
    }
}
