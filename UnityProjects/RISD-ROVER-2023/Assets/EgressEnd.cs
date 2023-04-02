using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EgressEnd : MonoBehaviour
{
     private GameObject[] objectsToDestroy;

    private void OnEnable()
    {
        objectsToDestroy = GameObject.FindGameObjectsWithTag("Egress");
        GameObject egressCompleteObject = GameObject.Find("Egress Complete");

        foreach (GameObject egress in objectsToDestroy)
        {
            Destroy(egress);
            Destroy(egressCompleteObject, 3f);
        
        }

   
    }
}
