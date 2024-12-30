using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ModItem : MonoBehaviour
{
    [SerializeField] public RawImage image;
    [SerializeField] public TextMeshProUGUI text;
    public string author;
    public string modName;
    public string contentName;
    public string modTag;
    public bool isContent;
    public void Click()
    {
        if (!isContent)
        {
            QuaD_SDK.main.SetScene(author, modName);
        }
        else
        {
            QuaD_SDK.main.SpawnContent(author, modName, contentName);
        }
    }
}
