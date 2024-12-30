using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaD_SyncTransform : MonoBehaviour
{
    public int index;

    [ExecuteInEditMode]
    void Awake()
    {
        List<int> indexUsed = new List<int>();

        bool alreadyExist = true;
        while(alreadyExist)
        {
            alreadyExist = false;
            int n = Random.Range(int.MinValue, int.MaxValue);

            foreach (QuaD_SyncTransform q in FindObjectsOfType<QuaD_SyncTransform>())
                if(q.index == n)
                {
                    alreadyExist = true;
                    break;
                }
        }
    }

}
