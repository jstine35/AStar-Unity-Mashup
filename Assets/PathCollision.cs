using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        Quaternion rotation = Quaternion.FromToRotation(Cardinal.up, contact.normal);
        Vector3 position = contact.point;
        Debug.Log("Thump!");
        //Instantiate(explosionPrefab, position, rotation);
        //Destroy(gameObject);
    }
}
