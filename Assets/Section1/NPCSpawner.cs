using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public List<GameObject> instances = new List<GameObject>();
    public int numberOfInstances = 50;
    public Transform player;
    public float distToPlayer = 1;
    public float box;

    public List<GameObject> instantiatedObjects = new List<GameObject>();


    void Awake()
    {
        for (int i = 0; i < numberOfInstances; i++)
        {
            Vector3 pos = Vector3.zero;
            do 
            {
                pos = GetNewPosition();
            } while (Vector3.Distance(pos, player.position) <= distToPlayer);

            int index = Random.Range(0, instances.Count - 1);
            GameObject newObj = GameObject.Instantiate(instances[index], pos, Quaternion.identity);
        }
    }



    Vector3 GetNewPosition()
    {
        Vector3 position = new Vector3(Random.Range(-box, box) + transform.position.x, 
                                        transform.position.y, 
                                        Random.Range(-box, box) + transform.position.z);
        
        return position;
    }

}
