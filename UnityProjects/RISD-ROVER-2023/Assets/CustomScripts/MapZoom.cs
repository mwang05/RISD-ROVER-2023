using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UX;


public class MapZoom : MonoBehaviour
{
    [SerializeField] private float _maxZoom = 2.0f;
    private RectTransform _mapRT;

    void Start()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
    }

    public void ScaleMapWithSlider(SliderEventData args)
    {
        // Prevent NULL reference before Start() runs
        if (!_mapRT)
            return;

        float scale = 1.0f + args.NewValue * _maxZoom;
        _mapRT.localScale = new Vector3(scale, scale, 1.0f);
    }
}
