using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS.Msgs;

public class NewEgressController : MonoBehaviour
{
    private string[] taskHeadings = {
        //step 1
        "Power on EMU-1", 
        //step 2
        "Prepare UIA",
        //step 3
        "Purge N2",
        //step 4
        "Initial O2 Pressurization",
        //step 5 - 1
        "Dump waste water", 
        //step 5 - 2
        "Refill EMU Water",
        //step 7
        "Complete EMU Pressurization",
        //step 8
        "Complete Airlock Depressurization",
        //step 9
        "UIA Procedures are complete, exit the airlock",
    };

    private List<string>[] taskSteps = {
        new List<string> {
            //step 1
            "Switch EMU-1 Power to ON",
            "When the SUIT is booted (emu1_is_booted), proceed",
            //step 2
            "Switch O2 Vent to OPEN",
            "When UIA Supply Pressure (uia_ < 23 psi, proceed)",
            "Switch O2 Vent to CLOSE",
            //step 3
            "Switch O2 Supply to OPEN",
            "When UIA Supply Pressure is > 3000 psi, proceed",
            "Switch O2 Supply to CLOSE",
           
            "Switch O2 Vent to OPEN",
            "When UIA Supply Pressure is < 23 psi, proceed",
            "Switch O2 Vent to CLOSE",
            //step 4
            "Switch O2 Supply to OPEN",
            "When UIA Supply Pressure is > 1500 psi, proceed",
            "Switch O2 Supply to CLOSE",
            //step 5-1
            "Dump waste water",
            "Switch EV-1 Waste to OPEN",
            "When water level if < 5%, proceed",
            "Switch EV-1 Waste to CLOSE",
            //step 5-2
            "Refill EMU Water",
            "Switch EV-1 Supply to OPEN",
            "When water level if < 5%, proceed",
            "Switch EV-1 Waste to CLOSE",
            //step 6
            "Switch Depress Pump to ON",
            "IF the pump faults:",
            "Switch the Depress Pump to OFF",
            "When the fault goes away, proceed",
            "Switch the Depress Pump to ON",
            "When airlock pressure is < 10.2 psi, switch to OFF proceed",
            //step 7
            "Switch O2 Supply to OPEN",
            "When UIA Supply Pressure > 3000 psi, proceed",
            "Switch O2 Supply to CLOSE",
            //step 8
            "Switch Depress Pump to ON",
            "When airlock pressure is < 0.1 psi, proceed",
            "Switch Depress Pump to OFF",
            
        },
    };

    private Func<int, bool>[] taskExecutions = {
        //step 1 Power on EMU-1
        step => step switch {
            0 => uiaMsg.emu1_pwr_switch,
            1 => uiaState.emu1_is_booted,
            _ => false, // default case
        },

        // step 2 Prepare UIA
        step => step switch {
            0 => uiaMsg.o2_vent_switch,
            1 => uiaState.uia_supply_pressure < 23,
            2 => uiaMsg.o2_vent_switch,
            _ => false, // default case
        },

        // step 3 Purge N2
        step => step switch {
            0 => uiaMsg.emu1_o2_supply_switch,
            1 => uiaState.uia_supply_pressure > 3000,
            2 => uiaMsg.emu1_o2_supply_switch,
            3 => uiaMsg.o2_vent_switch,
            4 => uiaState.uia_supply_pressure < 23,
            5 => uiaMsg.o2_vent_switch,
            _ => false, // default case
        },

        // step 4 Initial O2 Pressurization
        step => step switch {
            0 => uiaMsg.emu1_o2_supply_switch,
            1 => uiaState.uia_supply_pressure < 1500,
            2 => uiaMsg.emu1_o2_supply_switch,
            _ => false, // default case
        },

        // step 5 -1 Dump waste water
        step => step switch {
            0 => uiaMsg.emu1_water_waste,
            1 => uiaState.water_level < 5,
            2 => uiaMsg.emu1_water_waste,
            _ => false, // default case
        },

        // step 5 - 2 Refill EMU Water
        step => step switch {
            0 => uiaMsg.ev1_supply_switch,
            1 => uiaState.water_level > 95,
            2 => uiaMsg.ev1_supply_switch,
            _ => false, // default case
        },

        // step 6 Depressurize Airlock to 10.2 psi
        step => step switch {
            0 => uiaMsg.depress_pump_switch,
            1 => uiaState.depress_pump_fault,
            2 => uiaMsg.depress_pump_switch,
            3 => uiaState.depress_pump_fault,
            4 => uiaMsg.depress_pump_switch,
            5 => uiaState.airlock_pressure < 10.2,
            _ => false, // default case
        },

        // step 7 Complete EMU Pressurization
        step => step switch {
            0 => uiaMsg.emu1_o2_supply_switch,
            1 => uiaState.uia_supply_pressure > 3000,
            2 => uiaMsg.emu1_o2_supply_switch,
            _ => false, // default case
        },

        // step 8 Complete Airlock Depressurization
        step => step switch {
            0 => uiaMsg.depress_pump_switch,
            1 => uiaState.airlock_pressure < 0.1,
            2 => uiaMsg.depress_pump_switch,
            _ => false, // default case
        },

        // UIA Procedures are complete, exit the airlock
        step => step switch {
            0 => uiaMsg.emu1_pwr_switch,
            1 => uiaState.emu1_is_booted,
            _ => false, // default case
        },

    };

    private static UIAMsg uiaMsg;
    private static UIAState uiaState;

    private int currTask = 0;
    private int currStep = 0;
    private float loadingRotation;
    private GameObject currLoadingIcon;

    private GameObject nav;
    private MapController mapControllerScript;
    private GameObject[] normalTextObjs;
    private TMPro.TMP_Text[] normalTexts;
    private GameObject[] greenTextObjs;
    private TMPro.TMP_Text[] greenTexts;
    private GameObject[] completeIcons;
    private GameObject[] loadingIcons;
    private GameObject[] whiteDots;
    private GameObject[] emptyDots;
    private GameObject[] greenDots;
    private GameObject[] whiteLines;
    private GameObject[] greenLines;

    private TMPro.TMP_Text heading;
    private TMPro.TMP_Text stage;
    private RectTransform panelRT;

    private float panelMaxHeight = 260;
    private float panelWidth = 400;

    void Awake()
    {
        nav = GameObject.Find("Main Panel");
        mapControllerScript = GameObject.Find("Map Panel").GetComponent<MapController>();
        normalTextObjs = new GameObject[6];
        normalTexts = new TMPro.TMP_Text[6];
        greenTextObjs = new GameObject[6];
        greenTexts = new TMPro.TMP_Text[6];
        completeIcons = new GameObject[6];
        loadingIcons = new GameObject[6];
        whiteDots = new GameObject[10];
        emptyDots = new GameObject[10];
        greenDots = new GameObject[10];
        whiteLines = new GameObject[9];
        greenLines = new GameObject[9];
        heading = GameObject.Find("Heading Text").GetComponent<TMPro.TMP_Text>();
        stage = GameObject.Find("Procedure Stage").GetComponent<TMPro.TMP_Text>();
        panelRT = GameObject.Find("List Back Plate").GetComponent<RectTransform>();

        for (int i = 0; i < 6; i++)
        {
            normalTextObjs[i] = GameObject.Find("Stage Text " + (i + 1).ToString());
            greenTextObjs[i] = GameObject.Find("Stage Text g " + (i + 1).ToString());
            normalTexts[i] = normalTextObjs[i].GetComponent<TMPro.TMP_Text>();
            greenTexts[i] = greenTextObjs[i].GetComponent<TMPro.TMP_Text>();
            completeIcons[i] = GameObject.Find("Complete " + (i + 1));
            loadingIcons[i] = GameObject.Find("Loading " + (i + 1));
        }
        
        for (int i = 0; i < 10; i++)
        {
            whiteDots[i] = GameObject.Find("White Dot " + (i + 1).ToString());
            emptyDots[i] = GameObject.Find("Empty Dot " + (i + 1).ToString());
            greenDots[i] = GameObject.Find("Green Dot " + (i + 1).ToString());
            if (i < 9)
            {
                whiteLines[i] = GameObject.Find("White Line " + (i + 1).ToString());
                greenLines[i] = GameObject.Find("Green Line " + (i + 1).ToString());
                if (whiteLines[i] == null) Debug.Log("White Line " + (i + 1).ToString());
                if (greenLines[i] == null) Debug.Log("Green Line " + (i + 1).ToString());
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        nav.SetActive(false);
        uiaMsg = new UIAMsg();
        uiaState = new UIAState();
        SetupPanel();
    }

    void FixedUpdate()
    {
        loadingRotation = (loadingRotation + 1f) % 360;
        if (currLoadingIcon != null) currLoadingIcon.transform.localRotation = Quaternion.Euler(0, 0, loadingRotation);
    }

    // Update is called once per frame
    void Update()
    {
        // Current task complete, move on
        if (currTask < taskSteps.Length && currStep >= taskSteps[currTask].Count)
        {
            Debug.Log("Completed " + currTask);
            currTask++;
            currStep = 0;

            // Check if we are done with all tasks
            if (currTask >= taskSteps.Length) 
            {
                Debug.Log("Done");
                gameObject.SetActive(false);
                nav.SetActive(true);
                mapControllerScript.RecordStartTime();
            }
            else
            {
                Debug.Log("Move on to " + currTask);
                SetupPanel();
            }
        }

        if (TaskInProgress())
        {
            if (taskExecutions[currTask](currStep))
            {
                loadingIcons[currStep].SetActive(false);
                completeIcons[currStep].SetActive(true);
                currStep++;
                if (TaskInProgress())
                {
                    loadingIcons[currStep].SetActive(true);
                    currLoadingIcon = loadingIcons[currStep];
                }
                else 
                {
                    currLoadingIcon = null;
                }
            }
        }
    }

    private bool TaskInProgress()
    {
        return currTask < taskSteps.Length && currStep < taskSteps[currTask].Count;
    }

    void SetupPanel()
    {
        // Reset Procedure to the default state
        for (int i = 0; i < 11; i++)
        {
            emptyDots[i].SetActive(false);
            whiteDots[i].SetActive(false);
            greenDots[i].SetActive(false);

            if (i < 9)
            {
                whiteLines[i].SetActive(false);
                greenLines[i].SetActive(false);
            }
        }

        // Setup Procedure
        for (int i = 0; i < 11; i++)
        {
            if (i < currTask)
            {
                greenDots[i].SetActive(true);
                if (i > 0) greenLines[i-1].SetActive(true);
            }
            else 
            {
                if (i > currTask) emptyDots[i].SetActive(true);
                else whiteDots[i].SetActive(true);
                if (i > 0) whiteLines[i-1].SetActive(true);
            }
        }

        // Setup Title
        heading.text = taskHeadings[currTask];
        stage.text = "Procedure " + (currTask + 1).ToString() + " of 11";

        // Setup List
        for (int i = 0; i < 6; i++)
        {
            if (i < taskSteps[currTask].Count)
            {
                normalTexts[i].text = taskSteps[currTask][i];
                normalTextObjs[i].SetActive(true);
                greenTexts[i].text = taskSteps[currTask][i];
                greenTextObjs[i].SetActive(false);
            }
            else 
            {
                normalTextObjs[i].SetActive(false);
                greenTextObjs[i].SetActive(false);
            }
            completeIcons[i].SetActive(false);
            loadingIcons[i].SetActive(false);
        }
        loadingIcons[0].SetActive(true);
        currLoadingIcon = loadingIcons[0];
        
        // Resize the panel
        int numInactive = 6 - taskSteps[currTask].Count;
        panelRT.sizeDelta = new Vector2(panelWidth, panelMaxHeight - 30 * numInactive);
        Vector2 newPosition = panelRT.anchoredPosition;
        newPosition.y = 15 * numInactive;
        panelRT.anchoredPosition = newPosition;
    }

    public void UIAMsgUpdateCallback(UIAMsg msg)
    {
        uiaMsg = msg;
    }

    public void UIAStateUpdateCallback(UIAState state)
    {
        uiaState = state;
    }
}
