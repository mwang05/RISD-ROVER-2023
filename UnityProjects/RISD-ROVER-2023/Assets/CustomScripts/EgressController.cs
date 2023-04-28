using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EgressController : MonoBehaviour
{
    // public struct UIA
    // {
    //     public 
    // }
    private bool isTesting = false;
    public enum Test 
    {
        PrepareUIA, PurgeN2, O2Pressure, EMUwater, AirLock, EMUPressure, AirLockDepress
       
    } 

    private Test currTest;

    private int currStep;

    private float startTime;

    private TMPro.TMP_Text messageText;
    private TMPro.TMP_Text UIAText, N2Text, O2PressureText, EMUwaterText, AirLockText,EMUPressureText, AirLockDepressText;   
    private GameObject message, Egress;
    

    public void PerformTask(float s)
    {
        if(Time.time - startTime > s)
        {
            currStep++;
            startTime = Time.time;
        }
    }

    public void PrepareUIA()
    {
        isTesting = true;
        currTest = Test.PrepareUIA;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void PurgeN2()
    {
        isTesting = true;
        currTest = Test.PurgeN2;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void O2Pressure()
    {
        isTesting = true;
        currTest = Test.O2Pressure;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void EMUwater()
    {
        isTesting = true;
        currTest = Test.EMUwater;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void AirLock()
    {
        isTesting = true;
        currTest = Test.AirLock;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void AirLockDepress()
    {
        isTesting = true;
        currTest = Test.AirLockDepress;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void EMUPressure()
    {
        isTesting = true;
        currTest = Test.EMUPressure;
        currStep = 0;
        startTime = Time.time;
        message.SetActive(true);
    }

    public void PerformTest()
    {
        switch (currTest)
        {
            case Test.PrepareUIA: 
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Switch O2 Vent to OPEN";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "When UIA Supply Pressure (uia_ < 23 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "Switch O2 Vent to CLOSE";
                        PerformTask(1);
                        break; 
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        UIAText.color = new Color(0, 255, 0);
                        break;
                }
                break;

            case Test.PurgeN2:
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Switch O2 Supply to OPEN";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "When UIA Supply Pressure is > 3000 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "Switch O2 Supply to CLOSE";
                        PerformTask(1);
                        break;
                    case 3: 
                        messageText.text = "Switch O2 Vent to OPEN";
                        PerformTask(1);
                        break; 
                    case 4: 
                        messageText.text = "When UIA Supply Pressure is < 23 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 5: 
                        messageText.text = "Switch O2 Vent to CLOSE";
                        PerformTask(1);
                        break;  
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        N2Text.color = new Color(0, 255, 0);
                        break;
                }
                break;
            
            case Test.O2Pressure:
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Switch O2 Vent to OPEN";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "When UIA Supply Pressure is > 1500 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "Switch O2 Supply to CLOSE";
                        PerformTask(1);
                        break; 
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        O2PressureText.color = new Color(0, 255, 0);
                        break;
                }
                break;

                case Test.EMUwater: 
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Dump waste water";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "Switch EV-1 Waste to";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "When water level if < 5%, proceed";
                        PerformTask(1);
                        break; 
                    case 3: 
                        messageText.text = "Switch EV-1 Waste to CLOSE";
                        PerformTask(1);
                        break; 
                    case 4: 
                        messageText.text = "Refill EMU Water";
                        PerformTask(1);
                        break; 
                    case 5: 
                        messageText.text = "Switch EV-1 Supply to OPEN";
                        PerformTask(1);
                        break;  
                    case 6: 
                        messageText.text = "When water level is > 95%, proceed";
                        PerformTask(1);
                        break; 
                    case 7: 
                        messageText.text = "Switch EV-1 Supply to CLOSE";
                        PerformTask(1);
                        break; 
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        EMUwaterText.color = new Color(0, 255, 0);
                        break;
                }
                break;

                case Test.AirLock:
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Switch Depress Pump to ON";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "When airlock pressure is < 0.1 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "Switch Depress Pump to OFF";
                        PerformTask(1);
                        break; 
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        AirLockText.color = new Color(0, 255, 0);
                        break;
                }
                break;

                case Test.EMUPressure:
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Switch O2 Supply to OPEN";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "When UIA Supply Pressure > 3000 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "Switch O2 Supply to CLOSE";
                        PerformTask(1);
                        break; 
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        EMUPressureText.color = new Color(0, 255, 0);
                        break;
                }
                break;

                case Test.AirLockDepress:
                switch (currStep)
                {
                    case 0:
                        messageText.text = "Switch Depress Pump to ON";
                        PerformTask(1);
                        break;
                    case 1:
                        messageText.text = "When airlock pressure is < 0.1 psi, proceed";
                        PerformTask(1);
                        break; 
                    case 2: 
                        messageText.text = "Switch Depress Pump to OFF";
                        PerformTask(1);
                        break; 
                    default:
                        message.SetActive(false);
                        isTesting = false;
                        AirLockDepressText.color = new Color(0, 255, 0);
                        break;
                }
                break;
        }
    }

    // public void EndTest ()
    // {
    //    float delay = 3.0f;
    //    Egress = GameObject.Find("Egress");
    //    Destroy(Egress, delay);
    // }

    
    // Start is called before the first frame update
    void Start()
    {
        message = GameObject.Find("egress msg");
        messageText = GameObject.Find("egress msg text").GetComponent<TMPro.TMP_Text>();
        UIAText = GameObject.Find("UIA Text").GetComponent<TMPro.TMP_Text>();
        N2Text = GameObject.Find("N2 Text").GetComponent<TMPro.TMP_Text>();
        O2PressureText = GameObject.Find("O2 Pressure Text").GetComponent<TMPro.TMP_Text>();
        EMUwaterText = GameObject.Find("EMU water Text").GetComponent<TMPro.TMP_Text>();
        AirLockText = GameObject.Find("Airlock Text").GetComponent<TMPro.TMP_Text>();
        EMUPressureText = GameObject.Find("EMU Text").GetComponent<TMPro.TMP_Text>();
        AirLockDepressText = GameObject.Find("Airlock Depressure Text").GetComponent<TMPro.TMP_Text>();
        message.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isTesting)
        {
            PerformTest();
        }

        // else
        // {
        //     EndTest();
        // }
    }
}