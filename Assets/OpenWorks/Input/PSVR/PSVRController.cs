using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_PS4
using UnityEngine.PS4;
#endif


public class PSVRController : Controller
{

#if UNITY_PS4

    public Transform forceParent;

    public override void HapticPulse(float intensity, float speed)
    {

        base.HapticPulse(intensity, speed);

        
        if (isLeft)
                PS4Input.MoveSetVibration(0, 1, Mathf.Max((int)intensity, 128));
        else
            PS4Input.MoveSetVibration(0, 0, Mathf.Max((int)intensity, 128));


    }

    public override void UpdateHaptics()
    {
        base.UpdateHaptics();

        
        if (isLeft)
            PS4Input.MoveSetVibration(0, 1, Mathf.Max((int)_vibrationIntensity , 128));
        else
            PS4Input.MoveSetVibration(0, 0, Mathf.Max((int)_vibrationIntensity, 128));


        return;
    }

    /*
    public void Setup(SteamVR_Controller.Device device)
    {
        _device = device;
    }
    */

    /*
public SteamVR_Controller.Device GetDevice()
{
    return _device;
}
*/


      /*
    Joystick(n)Button0 -> Cross
    Joystick(n)Button1 -> Circle
    Joystick(n)Button2 -> Square
    Joystick(n)Button3 -> Triangle
    Joystick(n)Button4 -> Trigger
    Joystick(n)Button5 -> Move
    Joystick(n)Button7 -> Start
    */

    enum PS4MoveButtons
    {
        RightCross = KeyCode.Joystick5Button0,
        RightCircle = KeyCode.Joystick5Button1,
        RightSquare = KeyCode.Joystick5Button2,
        RightTriangle = KeyCode.Joystick5Button3,
        RightTrigger = KeyCode.Joystick5Button4,
        RightMove = KeyCode.Joystick5Button5,
        RightStart = KeyCode.Joystick5Button7,

        LeftCross = KeyCode.Joystick6Button0,
        LeftCircle = KeyCode.Joystick6Button1,
        LeftSquare = KeyCode.Joystick6Button2,
        LeftTriangle = KeyCode.Joystick6Button3,
        LeftTrigger = KeyCode.Joystick6Button4,
        LeftMove = KeyCode.Joystick6Button5,
        LeftStart = KeyCode.Joystick6Button7
    }

    override protected void Update()
    {
        base.Update();

        transform.parent = forceParent;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;


        GetGrabInputUp();
    }

    override public bool GetRadioInputDown()
    {

        return false;
    }


    override public bool GetRadioInputUp()
    {
        return false;
    }

    override public Vector2 GetRotationAxis()
    {
        if (isLeft)
        {
            if (Input.GetKey((KeyCode)PS4MoveButtons.LeftCross))
                return new Vector2(-1.0f, 0.0f);
            else
                return Vector2.zero;
        }
        else
        {
            if (Input.GetKey((KeyCode)PS4MoveButtons.RightCircle))
                return new Vector2(1.0f, 0.0f);
            else
                return Vector2.zero;
        }
    }


    private bool justGrabbed;

    override public bool GetGrabInput()
    {
        if (isLeft)
            return Input.GetKey((KeyCode)PS4MoveButtons.LeftCircle);
        else
            return Input.GetKey((KeyCode)PS4MoveButtons.RightCross);
    }

    override public bool GetGrabInputDown()
    {
        bool r;

        if (isLeft)
            r = Input.GetKeyDown((KeyCode)PS4MoveButtons.LeftCircle);
        else
            r = Input.GetKeyDown((KeyCode)PS4MoveButtons.RightCross);


        if (r && !taken)
            justGrabbed = true;

        return r;
    }

    override public bool GetGrabInputUp()
    {
        bool r;


        if (isLeft)
            r = Input.GetKeyUp((KeyCode)PS4MoveButtons.LeftCircle);
        else
            r = Input.GetKeyUp((KeyCode)PS4MoveButtons.RightCross);


        if (r)
            justGrabbed = false;

        return r;
    }

    override public bool GetLocomotionInput()
    {
        if (isLeft)
            return Input.GetKey((KeyCode)PS4MoveButtons.LeftMove);
        else
            return Input.GetKey((KeyCode)PS4MoveButtons.RightMove);
    }

    override public bool GetLocomotionInputUp()
    {
        if (isLeft)
            return Input.GetKeyUp((KeyCode)PS4MoveButtons.LeftMove);
        else
            return Input.GetKeyUp((KeyCode)PS4MoveButtons.RightMove);
    }

    

    public override bool GetThrowInputDown()
    {

        if (!justGrabbed)
        {
            if (isLeft)
                return Input.GetKeyUp((KeyCode)PS4MoveButtons.LeftCircle);
            else
                return Input.GetKeyUp((KeyCode)PS4MoveButtons.RightCross);
        }
        else
            return false;
    }

    public override bool GetTriggerInput()
    {
        if (isLeft)
            return Input.GetKey((KeyCode)PS4MoveButtons.LeftTrigger);
        else
            return Input.GetKey((KeyCode)PS4MoveButtons.RightTrigger);
    }

    public override bool GetTriggerInputUp()
    {
        if (isLeft)
            return Input.GetKeyUp((KeyCode)PS4MoveButtons.LeftTrigger);
        else
            return Input.GetKeyUp((KeyCode)PS4MoveButtons.RightTrigger);
    }

    public override bool GetTriggerInputDown()
    {
        if (isLeft)
            return Input.GetKeyDown((KeyCode)PS4MoveButtons.LeftTrigger);
        else
            return Input.GetKeyDown((KeyCode)PS4MoveButtons.RightTrigger);
    }

    public override bool GetMenuInputDown()
    {
        if (isLeft)
            return Input.GetKeyDown((KeyCode)PS4MoveButtons.LeftStart);
        else
            return Input.GetKeyDown((KeyCode)PS4MoveButtons.RightStart);
    }


    public override Vector3 GetAngularVelocity()
    {
        return Vector3.zero;//PS4Input.GetLastGyro(isLeft ? 1 : 0);
    }

#endif
}
