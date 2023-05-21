qsusing System;
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
            1 => true,
            _ => false,
        },
    };

    private static UIAMsg uiaState;
    private int currTask;
    private int currStep;

    private GameObject nav;
    private MapController mapControllerScript;
    private GameObject[] normalTextObjs;
    private TMPro.TMP_Text[] normalTexts;
    private GameObject[] greenTextObjs;
    private TMPro.TMP_Text[] greenTexts;
    private GameObject[] completeIcons;
    private GameObject[] loadingIcons;
    

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
        SetupPanel();
    }

    // Update is called once per frame
    void Update()
    {
        // Current task complete, move on
        if (currStep >= taskSteps[currTask].Count)
        {
            currTask++;
            currStep = 0;

            // Check if we are done with all tasks
            if (currTask >= taskTitles.Length) 
            {
                gameObject.SetActive(false);
                // nav.SetActive(true);
                // mapControllerScript.RecordStartTime();
            }
            else
            {
                SetupPanel();
            }
        }

        // Perform the current check
        if (taskExecutions[currTask](currStep)) currStep++;
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
    }
}
