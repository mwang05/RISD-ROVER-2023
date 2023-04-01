using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Compass : MonoBehaviour
{
    private RawImage _compassImage;
    private TextMeshProUGUI _compassDirText;

    void Awake()
    {
        _compassImage = GameObject.Find("Compass Image").GetComponent<RawImage>();;
        _compassDirText = GameObject.Find("Compass Degree").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        float angle = Camera.main.transform.localEulerAngles.y;
        _compassImage.uvRect = new Rect(angle / 360.0f, 0.0f, 1.0f, 1.0f);
        _compassDirText.text = Mathf.RoundToInt(angle).ToString();
    }
}
