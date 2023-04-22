using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UX;

public class EgressComplete : MonoBehaviour
{
     GameObject[] egressObjects;


    void Start()
    {
       GameObject[] egressObjects = GameObject.FindGameObjectsWithTag("Egress");
    }

    void Update()
    {
        bool allToggled = true;

        GameObject egress = GameObject.Find("Egress");

        foreach (GameObject egressObject in egressObjects)
        {
            // Get the PressableButton component on the current egress object
            PressableButton pressableButton = egressObject.GetComponent<PressableButton>();

            // Check if the button is toggled on
            if (pressableButton != null && pressableButton.IsToggled)
            {
                // Check if the "Is Toggled" property's checkbox is checked
                if (pressableButton.IsToggled.Active)
                {
                    allToggled = false;
                    // Destroy the egress object if the checkbox is checked
                    Destroy(egress);
                }
            }
        }

        if (allToggled)
        {
            
            GameObject egressDestroy = GameObject.Find("Egress");
            Destroy(egressDestroy);
        }
    }
}
