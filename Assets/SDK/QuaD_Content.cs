using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaD_Content : MonoBehaviour
{
    public string contentName = "";
    public int hash;
    public Component[] playerBehaviours;
    public QuaD_SyncTransform[] syncedTransforms;

    private void Start()
    {
        hash = contentName.GetHashCode();
        QuaD_SDK.main.RegisterContent(this);
    }
}
