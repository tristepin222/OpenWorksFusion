using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            QuaD_SDK.main.SetScene("tristepin22", "SoccerTest");
        }
    }
}
