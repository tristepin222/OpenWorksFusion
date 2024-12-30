using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;



public class Controller : MonoBehaviour
{
    public PlayerInput playerInput;

    public bool isLeft;
    

    // Rumble

    protected float _vibrationIntensity;
    protected float _vibrationSpeed;


    // Compute Velocity

    private Vector3 _velocity;


    // Build array of positions 

    public Vector3[] lastHandPositions;
    public Vector3[] lastPlayerPositions;

    public int lastPositionSize = 15;
    private int oldestPositionIndex;
    public float frequencyPosSample = 0.001f; // how much time between position sample
    private float lastSampleTime;             // record the time where the last position sample was done


    
    void Awake()
    {
        lastHandPositions = new Vector3[lastPositionSize];
        lastPlayerPositions = new Vector3[lastPositionSize];
    }

    public virtual void HapticPulse(float intensity, float speed)
    {
            _vibrationIntensity = intensity;
            _vibrationSpeed = speed;
    }

    public virtual void UpdateHaptics()
    {

        if (_vibrationIntensity > 0.0f)
        {
            _vibrationIntensity -= _vibrationSpeed * Time.deltaTime;
        }
        else
        {
            _vibrationIntensity = 0.0f;
        }

        return;
    }


    public void UpdatePosition()
    {
        // Sample positions at a specific frequency
        if (Time.time - lastSampleTime > frequencyPosSample)
        {
            // Store the positions of the Hand (override the oldest position with the new one)
            lastHandPositions[oldestPositionIndex] = transform.position - PlayerInput.singleton.transform.position;
            lastPlayerPositions[oldestPositionIndex] = PlayerInput.singleton.transform.position;

            // Increase the index AFTER storing the last position to always have the index of the OLDEST position
            oldestPositionIndex += 1;
            if (oldestPositionIndex >= lastPositionSize)
            {
                oldestPositionIndex = 0;
            }

            lastSampleTime = Time.time;
        }
    }


    public void ComputeVelocity()
    {

        Vector3 initalHandVelocity = ((transform.position - PlayerInput.singleton.transform.position) - lastHandPositions[oldestPositionIndex]) / (Time.deltaTime * lastHandPositions.Length);
        Vector3 initialPlayerVelocity = (PlayerInput.singleton.transform.position - lastPlayerPositions[oldestPositionIndex]) / (Time.deltaTime * lastPlayerPositions.Length);
        Vector3 newVelocity = (initialPlayerVelocity * 0.5f + initalHandVelocity * initalHandVelocity.magnitude);

        _velocity = newVelocity;//Vector3.Lerp(_velocity, newVelocity,  0.6f);

    }


    virtual public bool GetPrimaryButton()
    {
        return false;
    }

    virtual public bool GetSecondaryButton()
    {
        return false;
    }

    virtual public Vector2 GetAxis()
    {
        return Vector2.zero;
    }

    virtual public float GetGrip()
    {
        return 0.0f;
    }


    virtual public float GetTrigger()
    {
        return 0.0f;
    }


    virtual public bool GetJoystickPress()
    {
        return false;
    }


    virtual public Vector3 GetVelocity()
    {
        return _velocity;
    }

    virtual public Vector3 GetAngularVelocity()
    {
        return Vector3.zero;
    }

}
