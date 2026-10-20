using UnityEngine;
using UnityEngine.InputSystem;

public class PilotStation : CrewStation
{
    [Header("Plane")]
    public PlaneController plane;
    public float inputSmoothing = 5f;
    public float throttleRate = 0.25f;
    public bool invertPitch;

    public float PitchInput {get;private set;}
    public float RollInput {get;private set;}
    public float YawInput {get;private set;}


    protected override void OnBegin()
    {
        PitchInput = RollInput = YawInput = 0f;
    }
    protected override void OnEnd()
    {
        PitchInput = RollInput = YawInput = 0f;
        if (plane != null) plane.SetInput(0f,0f,0f);
    }
    protected override void OnStationUpdate()
    {
        if (plane == null || Keyboard.current == null) return;

        Keyboard k = Keyboard.current;

        float pitchTarget = Axis(k.wKey.isPressed, k.sKey.isPressed);
        if (invertPitch) pitchTarget = -pitchTarget;
        float rollTarget = Axis(k.dKey.isPressed, k.aKey.isPressed);
        float yawTarget = Axis(k.eKey.isPressed, k.qKey.isPressed);

        float t = inputSmoothing * Time.deltaTime;
        PitchInput = Mathf.Lerp(PitchInput, pitchTarget, t);
        RollInput = Mathf.Lerp(RollInput, rollTarget, t);
        YawInput = Mathf.Lerp(YawInput, yawTarget , t);

        plane.SetInput(PitchInput, RollInput, YawInput);

        float throttleDir = Axis(k.leftShiftKey.isPressed, k.leftCtrlKey.isPressed);
        if (throttleDir != 0f) plane.ChangeThrottle(throttleDir * throttleRate * Time.deltaTime);
    }
    private static float Axis(bool positive, bool negative)
    {
        return (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}