using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayFPS : MonoBehaviour
{

    void Update()
    {
        GetComponent<TextMesh>().text = "FPS : " + (int)(1.0f / Time.deltaTime);
    }
}
