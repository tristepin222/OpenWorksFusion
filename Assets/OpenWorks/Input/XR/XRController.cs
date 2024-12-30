using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRController : Controller
{


    public override void HapticPulse(float intensity, float speed)
    {
        /*
        base.HapticPulse(intensity, speed);
        var channel = OVRHaptics.RightChannel;
        if (isLeft)
            channel = OVRHaptics.LeftChannel;

        int count = (int)(80000f / speed);
        byte oculusIntensity = (byte)Mathf.Clamp((0.1f * intensity), 50, 255);
        OVRHapticsClip clip = new OVRHapticsClip(count);

        for (int i = 0; i < count; i++)
        {
            clip.Samples[i] = i % 2 == 0 ? (byte)0 : oculusIntensity;
        }

        clip = new OVRHapticsClip(clip.Samples, clip.Samples.Length);

        channel.Mix(clip);
        */

    }
    /*
    public override void HapticPulse(float intensity, float speed, AudioClip sound)
    {
        var channel = OVRHaptics.RightChannel;
        if (isLeft)
            channel = OVRHaptics.LeftChannel;
        OVRHapticsClip clip = new OVRHapticsClip(sound);

        channel.Mix(clip);
    }

    */
    void Start()
    {
        /*
        if (playerInput.vrDeviceType == PlayerInput.VRDeviceType.OculusRiftCV1)
        {
            controllerGraphics.SetActive(false);
        }
        */

        //UnityEngine.XR.TrackingOriginModeFlags

        List<UnityEngine.XR.XRInputSubsystem> subsystems = new List<UnityEngine.XR.XRInputSubsystem>();
        SubsystemManager.GetInstances(subsystems);
        foreach (var subsystem in subsystems)
        {
            subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }

    InputDevice device;

    public override void UpdateHaptics()
    {
        device = isLeft ? UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand) : UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);


        base.UpdateHaptics();

        //if(_device != null && (_vibrationIntensity > 10))
        // _device.TriggerHapticPulse((ushort)_vibrationIntensity);

        return;
    }

    /*
    public void Setup(SteamVR_Controller.Device device)
    {
        _device = device;
    }
    

    public SteamVR_Controller.Device GetDevice()
    {
        return _device;
    }
    */

    

    protected void Update()
    {
        //base.Update();
        UpdateHaptics();
    }

    bool wasTriggeryPressed = false;
    bool wasGripPressed = false;
    bool wasPrimaryPressed = false;
    bool wasSecondaryPressed = false;
    bool wasJoystickPressed = false;

    /*
    private void LateUpdate()
    {

        wasTriggeryPressed = GetTriggerInput();
        wasGripPressed = GetGrabInput();
        wasPrimaryPressed = false;
        wasSecondaryPressed = GetLocomotionInput();
        wasJoystickPressed = GetTouchpadInput();
    }
    */

    override public Vector2 GetAxis()
    {
        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 result);
        return result;   
    }


    public override float GetTrigger()
    {

        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float result);

        return result;
    }



    override public float GetGrip()
    {

        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float result);

        return result;
    }


    public override Vector3 GetAngularVelocity()
    {
        /*
        if (isLeft)
            return playerInput.transform.TransformVector(OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.LTouch));
        else
            return playerInput.transform.TransformVector(OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.RTouch));
            */

        return Vector3.zero;
    }

    public override bool GetJoystickPress()
    {

        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool result);
        return result;
    }

    public override bool GetPrimaryButton()
    {
        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool result);
        return result;
    }

    public override bool GetSecondaryButton()
    {
        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool result);
        return result;
    }
}
