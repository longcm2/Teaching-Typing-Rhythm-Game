using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    AudioSource enigma;

    // Start is called before the first frame update
    void Start()
    {
        enigma = GetComponent<AudioSource>();
        enigma.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
