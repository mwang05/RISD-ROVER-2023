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
            1 => true,
            _ => false,
        },
    };

    private static UIAMsg uiaState;
    private int currTask;
    private int currStep;

    private GameObject nav;
    private MapController mapControllerScript;

    void Awake()
    {
        nav = GameObject.Find("Main Panel");
        mapControllerScript = GameObject.Find("Map Panel").GetComponent<MapController>();
    }

    // Start is called before the first frame update
    void Start()
    {
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
                nav.SetActive(true);
                mapControllerScript.RecordStartTime();
            }
        }

        // Perform the current check
        if (taskExecutions[currTask](currStep)) currStep++;
    }

    void SetupPanel()
    {
        
    }
}
