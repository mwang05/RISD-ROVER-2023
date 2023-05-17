using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoverController : MonoBehaviour
{
    [HideInInspector] public Vector2 targetGpsCoord;
    [SerializeField] private float speed = 5e-5f;
    private GameObject roverPrefab;
    private Vector2 myGpsCoord;
    private bool isMoving;
    private GPS gps;

    void Start()
    {
        gps = GameObject.Find("GPS").GetComponent<GPS>();
        myGpsCoord = new Vector2(GPS.SatCenterLatitude, GPS.SatCenterLongitude);
    }

    void Update()
    {
        if (isMoving)
        {
            Vector2 toTarget = targetGpsCoord - myGpsCoord;
            if (toTarget.magnitude < 1e-6f)
            {
                isMoving = false;
            }
            else
            {
                myGpsCoord += toTarget.normalized * Mathf.Min(toTarget.magnitude, speed);
            }
        }
    }
}
