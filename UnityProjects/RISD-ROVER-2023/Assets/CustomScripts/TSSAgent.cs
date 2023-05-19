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
    private const string team_name = "";
    private const string username = "";
    private const string university = "";
    private const string user_guid = "";
    
    
    private bool isConnecting = false;
    private bool connected = false;

    private MarkerController markerController;

    private GameObject EVA;
    private TMPro.TMP_Text timerText, heartRateText, POPressureText, PORateText, POTimeText, POPrecentText; 
    private TMPro.TMP_Text SOPessureText, SORateText, SOTimeText, SOPercentText;
    private TMPro.TMP_Text h2oGasPressureText, h2oLiquidPressureText, suitPressureText, fanRateText; 
    private TMPro.TMP_Text EEPressure, EETemperature, batteryTimeText, batteryCapacityText;

    private GameObject connectMsg;

    // Start is called before the first frame update
    async void Start()
    {
        tss = new TSSConnection();
        markerController = GameObject.Find("Markers").GetComponent<MarkerController>();
        EVA = GameObject.Find("EVA");
        timerText = GameObject.Find("Timer").GetComponent<TMPro.TMP_Text>();
        heartRateText = GameObject.Find("BPM").GetComponent<TMPro.TMP_Text>();
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
        var connecting = tss.ConnectToURI(tssUri, team_name, username, university, user_guid);
        // Create a function that takes asing TSSMsg parameter and returns void. For example:
        // public static void PrintInfo(TSS.Msgs.TSSMsg tssMsg) { ... }
        // Then just subscribe to the OnTSSTelemetryMsg
        tss.OnTSSTelemetryMsg += (telemMsg) =>
        {
            // Do some thing with each type of message (get using telemMsg.MESSAGE[0])
            if (telemMsg.gpsMsg != null)
            {
                UpdateGPS(telemMsg.gpsMsg);
            }

            if (telemMsg.imuMsg != null)
            {
            }

            if (telemMsg.simulationStates != null)
            {
                UpdateEVA(telemMsg.simulationStates);
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

    private void UpdateEVA(SimulationStates eva)
    {
        if (!EVA.activeSelf) return;

        timerText.text = string.Format("{00:00:00}", eva.battery_percentage);
        
        heartRateText.text = eva.heart_rate.ToString("###bpm");

        POPressureText.text = eva.p_o2.ToString("###%");
        PORateText.text = eva.rate_o2.ToString("###%");
        POTimeText.text = string.Format("{00:00:00}", eva.t_oxygen);
        POPrecentText.text = eva.t_oxygenPrimary.ToString("###%");

        SOPessureText.text = eva.p_sop.ToString("###psia");
        SORateText.text = eva.rate_sop.ToString("#.#psi/min");
        SOTimeText.text = string.Format("{00:00:00}", eva.t_oxygen);
        SOPercentText.text = eva.t_oxygenSec.ToString("###%");

        h2oGasPressureText.text = eva.p_h2o_g.ToString("###psia");

        h2oLiquidPressureText.text = eva.p_h2o_l.ToString("###psia");

        suitPressureText.text = eva.p_suit.ToString("#psid"); 

        string v_fan_str = eva.v_fan.ToString("0F");
        fanRateText.text = v_fan_str.Insert(v_fan_str.Length - 3, ",");

        EEPressure.text = eva.p_sub.ToString("#psia");
        EETemperature.text = eva.t_sub.ToString("##F");

        batteryTimeText.text = string.Format("{00:00:00}", eva.t_battery);
        batteryCapacityText.text = eva.cap_battery.ToString("##amp-hr");

    }

    private void UpdateGPS(GPSMsg msg)
    {
    }
}
