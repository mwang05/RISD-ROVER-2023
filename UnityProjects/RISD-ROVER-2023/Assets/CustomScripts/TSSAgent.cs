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

    private GameObject EVA;
    private TMPro.TMP_Text timerText, heartRateText, POPressureText, PORateText, POTimeText, POPrecentText;
    private TMPro.TMP_Text SOPessureText, SORateText, SOTimeText, SOPercentText;
    private TMPro.TMP_Text h2oGasPressureText, h2oLiquidPressureText, suitPressureText, fanRateText;
    private TMPro.TMP_Text EEPressure, EETemperature, batteryTimeText, batteryCapacityText;

    private GameObject egress;
    private EgressController egressController;
    private GPS gps;
    private bool firstConnect;

    private GameObject connectMsg;

    // Start is called before the first frame update
    private void Awake()
    {
        markerController = GameObject.Find("Markers").GetComponent<MarkerController>();
        EVA = GameObject.Find("EVA");
        timerText = GameObject.Find("Timer").GetComponent<TMPro.TMP_Text>();
        heartRateText = GameObject.Find("Bpm").GetComponent<TMPro.TMP_Text>();
        POPressureText = GameObject.Find("Primary Oxygen Pressure").GetComponent<TMPro.TMP_Text>();
        PORateText = GameObject.Find("Primary Oxygen Rate").GetComponent<TMPro.TMP_Text>();
        POTimeText = GameObject.Find("Primary Oxygen Time").GetComponent<TMPro.TMP_Text>();
        POPrecentText = GameObject.Find("Primary Oxygen Percent").GetComponent<TMPro.TMP_Text>();
        SOPessureText = GameObject.Find("Secondary Oxygen Pressure").GetComponent<TMPro.TMP_Text>();
        SORateText = GameObject.Find("Secondary Oxygen Rate").GetComponent<TMPro.TMP_Text>();
        SOTimeText = GameObject.Find("Secondary Oxygen Time").GetComponent<TMPro.TMP_Text>();
        SOPercentText = GameObject.Find("Secondary Oxygen Percent").GetComponent<TMPro.TMP_Text>();
        h2oGasPressureText = GameObject.Find("H2O Gas Pressure").GetComponent<TMPro.TMP_Text>();
        h2oLiquidPressureText = GameObject.Find("H2O Liquid Pressure").GetComponent<TMPro.TMP_Text>();
        suitPressureText = GameObject.Find("Suit Pressure").GetComponent<TMPro.TMP_Text>();
        fanRateText = GameObject.Find("Fan Rate").GetComponent<TMPro.TMP_Text>();
        EEPressure = GameObject.Find("External Environment Pressure").GetComponent<TMPro.TMP_Text>();
        EETemperature = GameObject.Find("External Environment Temperature").GetComponent<TMPro.TMP_Text>();
        batteryTimeText = GameObject.Find("Battery Time").GetComponent<TMPro.TMP_Text>();
        batteryCapacityText = GameObject.Find("Battery Capacity").GetComponent<TMPro.TMP_Text>();
        connectMsg = GameObject.Find("Connecting");
        egress = GameObject.Find("Egress");
        egressController = egress.GetComponent<EgressController>();
        gps = GameObject.Find("GPS").GetComponent<GPS>();
    }

  void Start()
    {
        tss = new TSSConnection();
        connectMsg.SetActive(false);
        egress.SetActive(false);
        EVA.SetActive(false);
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
        isConnecting = true;
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
                UpdateEVA(telemMsg.simulationStates);
            }

            var uia = telemMsg.uiaMsg;
            bool validUia = uia.depress_pump_switch || uia.emu1_o2_supply_switch || uia.ev1_supply_switch ||
                              uia.emu1_pwr_switch || uia.emu1_water_waste || uia.o2_vent_switch;
            if (validUia && egress.activeSelf)
            {
                egressController.UIAUpdateCallback(telemMsg.uiaMsg);
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
            egress.SetActive(true);
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

	public void SendRoverMoveCommand(Vector2 loc)
	{
		Debug.Log("Send rover to " + loc);
		tss.SendRoverNavigateCommand(loc.x, loc.y);
	}

    private void UpdateEVA(SimulationStates eva)
    {
        if (!EVA.activeSelf) return;

        timerText.text = string.Format("{00:00:00}", eva.timer);

        heartRateText.text = eva.heart_rate.ToString("###bpm");

        POPressureText.text = eva.o2_pressure.ToString("###%");
        PORateText.text = eva.o2_rate.ToString("###%");
        POTimeText.text = string.Format("{00:00:00}", eva.oxygen_primary_time);
        POPrecentText.text = eva.primary_oxygen.ToString("###%");

        SOPessureText.text = eva.sop_pressure.ToString("###psia");
        SORateText.text = eva.sop_rate.ToString("#.#psi/min");
        SOTimeText.text = string.Format("{00:00:00}", eva.oxygen_secondary_time);
        SOPercentText.text = eva.secondary_oxygen.ToString("###%");

        h2oGasPressureText.text = eva.h2o_gas_pressure.ToString("###psia");

        h2oLiquidPressureText.text = eva.h2o_liquid_pressure.ToString("###psia");

        suitPressureText.text = eva.suit_pressure.ToString("#psid");

        string v_fan_str = eva.fan_tachometer.ToString("0F");
        fanRateText.text = v_fan_str.Insert(v_fan_str.Length - 3, ",");

        EEPressure.text = eva.sub_pressure.ToString("#psia");
        EETemperature.text = eva.temperature.ToString("##F");

        batteryTimeText.text = string.Format("{00:00:00}", eva.battery_time_left);
        batteryCapacityText.text = eva.battery_capacity.ToString("##amp-hr");

    }
}
