using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaD_SDK : MonoBehaviour
{
    public static QuaD_SDK main;

    public Dictionary<int, QuaD_Content> contents;
    public List<QuaD_PlayerBehaviour> playerBehaviours; // dont forget to register



    public virtual bool IsServer() { return true; }

    void Awake()
    {
        main = this;
    }

    public virtual void RegisterContent(QuaD_Content content)
    {
        foreach (var pb in content.playerBehaviours)
            playerBehaviours.Add(pb as QuaD_PlayerBehaviour);

        contents.Add(content.hash, content);
    }

    public virtual void SpawnContent(string author, string modName, string contentName)
    {

    }
    public virtual void SetScene(string author, string modName, string sceneName = "")
    {

    }

    public virtual void PlayEffect(GameObject g)
    {
       g.GetComponent<AudioSource>().Play();
       g.GetComponent<ParticleSystem>().Play();
    }
}


public interface QuaD_PlayerBehaviour
{
    IEnumerator Start(QuaD_PlayerData data);

    IEnumerator OnInput(QuaD_PlayerData data, QuaD_ControllerInput leftHandinput, QuaD_ControllerInput rightHandInput);

    IEnumerator FixedNetworkUpdate(QuaD_PlayerData data);

    IEnumerator OnDestroy(QuaD_PlayerData data);
}

public class QuaD_PlayerData
{
    public int index;
    public string pseudo;
    public Transform transform;
    public Transform head;
    public Transform rightHand;
    public Transform leftHand;
}

public class QuaD_ControllerInput
{
    public Vector2 axis;
    public float trigger;
    public float grip;
    public bool button1;
    public bool button2;
    public bool axisButton;
}

public interface IQuad_ServerBehaviour
{

}
