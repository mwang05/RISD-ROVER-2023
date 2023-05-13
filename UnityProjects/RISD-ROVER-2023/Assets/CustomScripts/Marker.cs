using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MarkerType
{
    Waypoint,
    Obstacle,
    POI,
    Rover
};

public class Marker
{
    private MarkerType type;

    public Marker(MarkerType t)
    {
        this.type = t;
    }
}
