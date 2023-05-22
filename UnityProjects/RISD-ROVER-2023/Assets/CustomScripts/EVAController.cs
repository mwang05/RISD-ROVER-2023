using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS.Msgs;

public class EVAController : MonoBehaviour
{
    private TMPro.TMP_Text timerText, heartRateText, POPressureText, PORateText, POTimeText, POPrecentText;
    private TMPro.TMP_Text SOPessureText, SORateText, SOTimeText, SOPercentText;
    private TMPro.TMP_Text h2oGasPressureText, h2oLiquidPressureText, suitPressureText, fanRateText;
    private TMPro.TMP_Text EEPressure, EETemperature, batteryTimeText, batteryCapacityText;

    void Awake()
    {
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

    }

    public void EVAMsgUpdateCallback(SimulationStates eva)
    {
        timerText.text = string.Format("{00:00:00}", eva.timer);

        heartRateText.text = eva.heart_rate.ToString("###bpm");

        POPressureText.text = eva.o2_pressure.ToString(".%");
        PORateText.text = (eva.o2_rate/100).ToString("###%");
        POTimeText.text = string.Format("{00:00:00}", eva.oxygen_primary_time);
        POPrecentText.text = (eva.primary_oxygen/100).ToString("###%");

        SOPessureText.text = eva.sop_pressure.ToString("###psia");
        SORateText.text = eva.sop_rate.ToString("#.#psi/min");
        SOTimeText.text = string.Format("{00:00:00}", eva.oxygen_secondary_time);
        SOPercentText.text = (eva.secondary_oxygen/100).ToString("###%");

        h2oGasPressureText.text = eva.h2o_gas_pressure.ToString("###psia");

        h2oLiquidPressureText.text = eva.h2o_liquid_pressure.ToString("###psia");

        suitPressureText.text = eva.suit_pressure.ToString("#psid");

        string v_fan_str = eva.fan_tachometer.ToString("0F");
        fanRateText.text = v_fan_str.Insert(v_fan_str.Length - 3, ",");

        EEPressure.text = eva.sub_pressure.ToString("#psia");
        EETemperature.text = eva.temperature.ToString("##.#F");

        batteryTimeText.text = string.Format("{00:00:00}", eva.battery_time_left);
        batteryCapacityText.text = eva.battery_capacity.ToString("##amp-hr");
    }
}
