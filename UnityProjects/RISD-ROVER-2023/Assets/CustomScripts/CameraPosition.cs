using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UX;

public class CameraPosition : MonoBehaviour
{
    [SerializeField] private VerticalLayoutGroup _verticalLayoutGroup;

    private RectTransform _mapRT;
    private RectTransform _curlocRT;

    void Start()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
        _curlocRT = GameObject.Find("Curloc").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        float scale = 100.0f * _mapRT.localScale.x;

        Vector3 worldPos = Camera.main.transform.position;

        _curlocRT.offsetMin = new Vector2(scale * worldPos.x + _mapRT.offsetMin.x, scale * worldPos.z + _mapRT.offsetMin.y);
        _curlocRT.offsetMax = _curlocRT.offsetMin;

        // Convert worldPos.xz to vlayoutgrp left & top
        // TODO: Currently assumes map to be centered at world origin
        // _verticalLayoutGroup.padding.left = (int)(scale * worldPos.x);
        // _verticalLayoutGroup.padding.top = (int)(scale * -worldPos.z);

        // LayoutRebuilder.ForceRebuildLayoutImmediate(_child.GetComponent<RectTransform>());
        // Somehow the above doesn't work... so do it manually
        // _verticalLayoutGroup.CalculateLayoutInputHorizontal();
        // _verticalLayoutGroup.CalculateLayoutInputVertical();
        // _verticalLayoutGroup.SetLayoutHorizontal();
        // _verticalLayoutGroup.SetLayoutVertical();

        // Debug.Log(worldPos);
        // Debug.Log(_mapScale);
        // Debug.Log(verticalLayoutGroup.padding.left);
    }

}
