using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EgressEnd : MonoBehaviour
{
     public GameObject[] objectsToDestroy;
     public float delay = 3f;
    
private void Start()
{
    Invoke("DisabledObject", delay);
}
    private void DisabledObject()
    {
       GameObject egressCompleteObject = GameObject.Find("Egress Complete");
        objectsToDestroy = GameObject.FindGameObjectsWithTag("Egress");
        

        foreach (GameObject egress in objectsToDestroy)
        {
            egress.SetActive(false);   
            egressCompleteObject.SetActive(false);
        }
       
        
   
    }
}
