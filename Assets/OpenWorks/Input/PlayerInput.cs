using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using System;
using System.Collections.Generic;

using Fusion;
using Fusion.Sockets;


public class PlayerInput : MonoBehaviour, INetworkRunnerCallbacks
{

    public struct NetworkInputData : INetworkInput
    {
        public const byte Fire = 1;

        public Vector3 headLocalPosition;
        public Quaternion headLocalRotation;

        public ControllerInput leftControllerInput;
        public ControllerInput rightControllerInput;

        // public GrabInfo leftGrabInfo;
        // public GrabInfo rightGrabInfo;
    }



    [System.Serializable]
    public struct ControllerInput : INetworkStruct
    {
        public Vector3 localPosition;
        public Quaternion localRotation;

        public Vector2 axis;
        public float trigger;
        public float grip;
        public bool button1;
        public bool button2;
        public bool axisButton;
    }



    [System.Serializable]
    public struct GrabInfo : INetworkStruct
    {
        public NetworkBehaviourId grabbedObject;

        public Vector3 localPositionOffset;
        public Quaternion localRotationOffset;

        // We want the local user accurate ungrab position to be enforced on the network, and so shared in the input (to avoid the grabbable following "too long" the grabber)
        public Vector3 ungrabPosition;
        public Quaternion ungrabRotation;
        public Vector3 ungrabVelocity;
        public Vector3 ungrabAngularVelocity;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        print("Update");

        var data = new NetworkInputData();


        // Head

        data.headLocalPosition = head.position - transform.position;
        data.headLocalRotation = head.rotation;


        // Right Controller

        data.rightControllerInput.localPosition = rightController.transform.position - transform.position;
        data.rightControllerInput.localRotation = rightController.transform.rotation;

        data.rightControllerInput.axis = rightController.GetAxis();
        data.rightControllerInput.trigger = rightController.GetTrigger();
        data.rightControllerInput.grip = rightController.GetGrip();
        data.rightControllerInput.button1 = rightController.GetPrimaryButton();
        data.rightControllerInput.button2 = rightController.GetSecondaryButton();
        data.rightControllerInput.axisButton = rightController.GetJoystickPress();


        //Left Controller

        data.leftControllerInput.localPosition = leftController.transform.position - transform.position;
        data.leftControllerInput.localRotation = leftController.transform.rotation;

        data.rightControllerInput.axis = rightController.GetAxis();
        data.rightControllerInput.trigger = rightController.GetTrigger();
        data.rightControllerInput.grip = rightController.GetGrip();
        data.rightControllerInput.button1 = rightController.GetPrimaryButton();
        data.rightControllerInput.button2 = rightController.GetSecondaryButton();
        data.rightControllerInput.axisButton = rightController.GetJoystickPress();

        /*
        {
            Vector2 controllerInput = leftController.GetAxis();

            Transform reference = head;

            Vector3 forward;
            

            if(reference.forward.y > 0.5f)
                forward = -reference.transform.up;
            else if (reference.forward.y < -0.5f)
                forward = reference.transform.up;
            else
                forward = reference.transform.forward;

            Vector3 right = reference.transform.right;

            forward.y = 0.0f;  forward.Normalize();
            right.y = 0.0f; right.Normalize();

            data.locomotionInput = Vector3.ClampMagnitude(forward * controllerInput.y + right * controllerInput.x, 1.0f);
        }
        */

        input.Set(data);

        //print("Input");
    }


    // ------------------------------------------------------------------------------------------------------------------------


    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }


    //-------------------------------------------------------------------------------------------------------------------------------------------------


    // Inputs

    public Transform head;
    public Controller leftController;
    public Controller rightController;

    public Transform wobble;

    public Player player;


    public static PlayerInput singleton;



    public virtual void Awake()
    {
        singleton = this;
    }

    public enum SeatedCrouchLvl { Stand = 0, Crouch = 1, Prone = 2 }

    public SeatedCrouchLvl seatedCrouchLvl = SeatedCrouchLvl.Stand;


    protected virtual void Update()
    {




        // Seated Mode

        /*
        if (VRPreferences.singleton.dataSaved.locomotion == 3)
        {
            Controller crouchController = VRPreferences.singleton.dataSaved.leftJoystickMovement ? rightController : leftController;

            seatedCrouchLvl = (SeatedCrouchLvl)Mathf.Clamp((int)seatedCrouchLvl + crouchController.GetCrouchInputDown(), 0, 2);

            if (wobble)
            {
                wobble.localPosition = Vector3.down * (-0.3f + 0.4f * (int)seatedCrouchLvl);
            }
        }
        */

        // Following Player

        if(player)
        {
            transform.position = player.transform.position;
        }
        else if(NetworkRunner.Instances[0].IsRunning)
        {
            foreach (Player p in FindObjectsOfType<Player>())
                if (p.HasInputAuthority)
                {
                    player = p;
                    break;
                }
        }
    }


    public virtual IEnumerator Start()
    {

        while (NetworkRunner.Instances.Count == 0)
            yield return null;

        NetworkRunner.Instances[0].AddCallbacks(this);

        print("calledBack");
    }


    //changes the scale depending on the height of the player (in centimeters)

    public void SetHeight(int height)
    {
        transform.localScale = Vector3.one * 180f / ((float)Mathf.Clamp(height, 150, 200));
    }

}
