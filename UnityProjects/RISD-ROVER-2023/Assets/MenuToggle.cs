using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UX;

public class MenuToggle : MonoBehaviour
{
    public GameObject menuButton;
    public GameObject voiceRecordingButton;

    private Toggle menuToggle;
    
    private void Start()
    {
        menuToggle = menuButton.GetComponent<Toggle>();
        menuToggle.onValueChanged.AddListener(OnMenuButtonToggled);
    }

    private void OnMenuButtonToggled(bool isToggled)
    {
        // Detoggle the voice recording button when the menu button is toggled
        if (isToggled)
        {
            Toggle voiceToggle = voiceRecordingButton.GetComponent<Toggle>();
            if (voiceToggle != null) 
            {
                voiceToggle.isOn = false;
            }
        }
    }
}
