using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TSS.Msgs;

public class NewEgressController : MonoBehaviour
{
    private string[] taskTitles = {
        "Power on EMU-1",
    };

    private List<string>[] taskSteps = {
        new List<string> {
            "Switch EMU-1 Power to ON",
            "When the SUIT is booted (emu1_is_booted), proceed"
        },
    };

    private Func<int, bool>[] taskExecutions = {
        // Power on EMU-1
        step => step switch {
            0 => uiaState.emu1_pwr_switch,
            1 => uiaState.depress_pump_switch, // emu1_is_booted
            _ => false,
        },
    };

    private static UIAMsg uiaState;
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
    private TMPro.TMP_Text heading;
    private TMPro.TMP_Text stage;
    private RectTransform panelRT;

    // private float simulationTime;
    private float panelMaxHeight = 260;
    private float panelWidth = 400;

    void Awake()
    {
        // nav = GameObject.Find("Main Panel");
        // mapControllerScript = GameObject.Find("Map Panel").GetComponent<MapController>();
        normalTextObjs = new GameObject[6];
        normalTexts = new TMPro.TMP_Text[6];
        greenTextObjs = new GameObject[6];
        greenTexts = new TMPro.TMP_Text[6];
        completeIcons = new GameObject[6];
        loadingIcons = new GameObject[6];
        heading = GameObject.Find("Heading Text").GetComponent<TMPro.TMP_Text>();
        stage = GameObject.Find("Procedure Stage").GetComponent<TMPro.TMP_Text>();
        panelRT = GameObject.Find("List Back Plate").GetComponent<RectTransform>();
        nav = GameObject.Find("Main Panel");
        mapControllerScript = GameObject.Find("Map Panel").GetComponent<MapController>();

        for (int i = 0; i < 6; i++)
        {
            normalTextObjs[i] = GameObject.Find("Stage Text " + (i + 1).ToString());
            greenTextObjs[i] = GameObject.Find("Stage Text g " + (i + 1).ToString());
            normalTexts[i] = normalTextObjs[i].GetComponent<TMPro.TMP_Text>();
            greenTexts[i] = greenTextObjs[i].GetComponent<TMPro.TMP_Text>();
            completeIcons[i] = GameObject.Find("Complete " + (i + 1));
            loadingIcons[i] = GameObject.Find("Loading " + (i + 1));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        nav.SetActive(false);
        uiaState = new UIAMsg();
        // simulationTime = Time.time;
        SetupPanel();
    }

    void FixedUpdate()
    {
        // if (Time.time - simulationTime > 3) uiaState.emu1_pwr_switch = true;
        // if (Time.time - simulationTime > 6) uiaState.depress_pump_switch = true;
        loadingRotation = (loadingRotation + 1f) % 360;
        if (currLoadingIcon != null) currLoadingIcon.transform.localRotation = Quaternion.Euler(0, 0, loadingRotation);
    }

    // Update is called once per frame
    void Update()
    {
        // Current task complete, move on
        if (currTask < taskSteps.Length && currStep >= taskSteps[currTask].Count)
        {
            currTask++;
            currStep = 0;

            // Check if we are done with all tasks
            if (currTask >= taskTitles.Length) 
            {
                Debug.Log("Done");
                gameObject.SetActive(false);
                nav.SetActive(true);
                mapControllerScript.RecordStartTime();
            }
            else
            {
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
        panelRT.sizeDelta = new Vector2(panelWidth, panelMaxHeight - 30 * (6 - taskSteps[currTask].Count));
    }

    public void UIAUpdateCallback(UIAMsg msg)
    {
        uiaState = msg;
    }
}
