using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Geosampling : MonoBehaviour
{
    private MapController _mapControllerScript;

    // Start is called before the first frame update
    void Start()
    {
        _mapControllerScript = GameObject.Find("Map Panel").GetComponent<MapController>();
    }

    // Update is called once per frame
    void Update()
    {
        // TakeSampleCallback();
    }

    public void TakeSampleCallback()
    {
        // DateTime currTime = DateTime.Now;
        var deltaTime = DateTime.Now - _mapControllerScript._startTimestamp;
        Debug.Log(deltaTime);
    }

}
