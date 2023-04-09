using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavStart : MonoBehaviour
{
    public EgressEnd egressendScript;
    private GameObject[] objectsToStart;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(egressendScript.objectsToDestroy == null)
        {
            objectsToStart = GameObject.FindGameObjectsWithTag("Nav");
            
            foreach(GameObject Nav in objectsToStart)
            {
                if(!Nav.activeSelf)
                {
                    Nav.SetActive(true);
                }
                
            }
        }
             
    }
}
