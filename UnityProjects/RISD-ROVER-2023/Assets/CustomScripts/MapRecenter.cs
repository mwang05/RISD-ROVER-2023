using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRecenter : MonoBehaviour
{
    private RectTransform _mapRT;

    // Start is called before the first frame update
    void Start()
    {
        _mapRT = GameObject.Find("Map").GetComponent<RectTransform>();
    }

    public void CenterMapAtUser()
    {
        float scaleW2M = 100.0f * _mapRT.localScale.x;

        Vector3 userPos = Camera.main.transform.position;
        Vector3 userLook = Camera.main.transform.forward;

        // Convert userPos to map RT offset
        _mapRT.offsetMin = new Vector2(userPos.x, userPos.z) * -scaleW2M;
        _mapRT.offsetMax = _mapRT.offsetMin;
    }
}

