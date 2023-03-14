using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UX;


public class MapControl : MonoBehaviour
{
    [SerializeField] private RectTransform rt;

    public void ScaleMapWithSlider(SliderEventData args) 
    {
        float scale = 1 + args.NewValue;
        rt.localScale = new Vector3(scale, scale, 1);
    }
}
