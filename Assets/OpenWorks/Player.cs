using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Fusion;
using ExitGames.Client.Photon;


//using static MenuOptionButton;


public class Player : NetworkBehaviour
{
   
    public PlayerInput input;

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    public Rigidbody body;
    
    public float speed = 5.0f;
    public float locomotionForceFactor = 1000.0f;
    public float locomotionMaxForce = 1000.0f;

    private QuaD_PlayerData playerData = new QuaD_PlayerData();
    private QuaD_ControllerInput rightControllerInput = new QuaD_ControllerInput();
    private QuaD_ControllerInput leftControllerInput = new QuaD_ControllerInput();

    // Keep Start as Light as Possible

    public  void Start()
    {
        transform.position = GameObject.Find("InitialSpawnPosition").transform.position;

        playerData.transform = transform;
        playerData.head = head;
        playerData.rightHand = rightHand;
        playerData.leftHand = leftHand;

        foreach(var pb in QuaD_SDK.main.playerBehaviours)
            pb.Start(playerData);
    }


    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput.NetworkInputData data) && HasStateAuthority)
        {
            head.transform.SetPositionAndRotation(data.headLocalPosition + transform.position, data.headLocalRotation);
            rightHand.transform.SetPositionAndRotation(data.rightControllerInput.localPosition + transform.position, data.rightControllerInput.localRotation);
            leftHand.transform.SetPositionAndRotation(data.leftControllerInput.localPosition + transform.position, data.leftControllerInput.localRotation);

            Vector3 locomotion = data.leftControllerInput.axis;

            body.AddForce(Vector3.ClampMagnitude(locomotion * locomotionForceFactor, locomotionMaxForce));

            leftControllerInput.axis = data.leftControllerInput.axis;
            leftControllerInput.trigger = data.leftControllerInput.trigger;
            leftControllerInput.grip = data.leftControllerInput.grip;
            leftControllerInput.button1 = data.leftControllerInput.button1;
            leftControllerInput.button2 = data.leftControllerInput.button2;
            leftControllerInput.axisButton = data.leftControllerInput.axisButton;

            rightControllerInput.axis = data.rightControllerInput.axis;
            rightControllerInput.trigger = data.rightControllerInput.trigger;
            rightControllerInput.grip = data.rightControllerInput.grip;
            rightControllerInput.button1 = data.rightControllerInput.button1;
            rightControllerInput.button2 = data.rightControllerInput.button2;
            rightControllerInput.axisButton = data.rightControllerInput.axisButton;

            foreach (var pb in QuaD_SDK.main.playerBehaviours)
                pb.OnInput(playerData, leftControllerInput, rightControllerInput);
        }

        foreach (var pb in QuaD_SDK.main.playerBehaviours)
            pb.FixedNetworkUpdate(playerData);

        base.FixedUpdateNetwork();
    }

    private void OnDestroy()
    {
        foreach (var pb in QuaD_SDK.main.playerBehaviours)
            pb.OnDestroy(playerData);
    }

}
