using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class EngineWithGear : MonoBehaviour
{
    public AnimationCurve torqueCurve;

    public float Throttle { get; set; }

    public float EngineTorque
    {
        get
        {
            return Throttle * torqueCurve.Evaluate(EngineHelpers.SpeedToRPM(EngineSpeed));
        }
    }

    public float EngineSpeed { get; private set; }
    public float TransmissionInputSpeed {get; private set; }

    public float engineMOI = 1f;
    public float transmissionInputMOI = 1f;
    public float transmissionOuputMOI = 1000f;

    public float engineDamping = 0.2f;
    public float transmissionInputDamping = 0.01f;
    public float transmissionOutputDamping = 2f;

    public bool ClutchLocked {get; private set; }

    public float TorqueOnTransmissionOutput { get; set; }

    public float clutchKineticFrictionConstant = 1000f;
    public float clutchStaticFrictionConstant = 1200f;

    public float ClutchAmount { get; private set; }

    public int CurrentGear { get; private set; }
    public float[] gearRatios = { 5f, 2f, 1.2f, 0.8f, 0.4f };

    public HeatModule clutchHeatModule;
    public float clutchFrictionToHeat = 0.1f;

    public HeatModule engineHeatModule;
    public float engineHeatMultiplier = 0.1f;


    void FixedUpdate()
    {
        if (ClutchLocked)
        {
            UpdateClutchLocked();
        }
        else
        {
            UpdateClutchSlipping();
        }

        engineHeatModule.AddHeat((Mathf.Pow(4 * Throttle, 2f) * EngineSpeed * engineHeatMultiplier * Time.fixedDeltaTime) + 1f * Time.fixedDeltaTime);
    }


    private void UpdateClutchSlipping()
    {
        // Gear ratio is 1, clutch is slipping
        // IeWe. = Tin - BeWe - Tcl
        // ItWt. = Tout - BtWt + Tcl

        // Tfmaxk = (constant) * clutch friction force
        // Tcl = sign(We - Wt) * Tfmaxk

        // We. = (Tin - BeWe - Tcl) / Ie
        // Wt. = (Tout - BtWt + Tcl) / It

        // Torques on either side of the clutch (not including clutch)
        float engineTorqueWithoutClutch = EngineTorque - (engineDamping * EngineSpeed);
        float transmissionDamping = GetTotalTransmissionDampingOnInput() * TransmissionInputSpeed;
        float transmissionTorqueWithoutClutch = GetTransmissionTorqueOnInput() - transmissionDamping;

        // Torques on both sides (not clutch) into angular accelerations
        float engineAccelerationNoClutch = engineTorqueWithoutClutch / engineMOI;
        float transmissionInputAccelerationNoClutch = transmissionTorqueWithoutClutch / GetTotalTransmissionMOIOnInput();

        EngineSpeed += engineAccelerationNoClutch * Time.fixedDeltaTime;
        TransmissionInputSpeed += transmissionInputAccelerationNoClutch * Time.fixedDeltaTime;

        // Torque on the clutch
        float clutchForce = GetMaxKineticClutchForce();
        float torqueThroughClutch = 0f;
        torqueThroughClutch = Mathf.Sign(EngineSpeed - TransmissionInputSpeed) * clutchForce;

        // Torques on both sides (including clutch torque) into angular accelerations
        float engineAccelerationFromClutch = -torqueThroughClutch / engineMOI;
        float transmissionInputAccelerationFromClutch = torqueThroughClutch / GetTotalTransmissionMOIOnInput();

        float totalAttemptedDeltaSpeed = (Mathf.Abs(engineAccelerationFromClutch) + Mathf.Abs(transmissionInputAccelerationFromClutch)) * Time.fixedDeltaTime;
        float allowedDeltaSpeed = Mathf.Abs(EngineSpeed - TransmissionInputSpeed);

        float torqueScale = 1f;
        if (totalAttemptedDeltaSpeed > allowedDeltaSpeed)
        {
            torqueScale = allowedDeltaSpeed / totalAttemptedDeltaSpeed;
            ClutchLocked = true;
        }

        engineAccelerationFromClutch *= torqueScale;
        transmissionInputAccelerationFromClutch *= torqueScale;

        EngineSpeed += engineAccelerationFromClutch * Time.fixedDeltaTime;
        TransmissionInputSpeed += transmissionInputAccelerationFromClutch * Time.fixedDeltaTime;

        // Heat
        float actualTorqueThroughClutch = torqueThroughClutch * torqueScale;
        //float speedDifference = EngineSpeed - TransmissionInputSpeed;
        float energyGenerated = actualTorqueThroughClutch * clutchFrictionToHeat; // * speedDifference

        clutchHeatModule.AddHeat(energyGenerated * 0.000526565076466f * Time.fixedDeltaTime); 
    }


    private void UpdateClutchLocked()
    {
        // Gear ratio is 1, clutch is locked
        // IeWe. = Tin - BeWe - Tcl
        // ItWt. = Tout - BtWt + Tcl

        // We = Wt = W
        // We. = Wt. = W.

        // IeW. = Tin - BeW - Tcl
        // ItW. = Tout - BtW + Tcl

        // (Ie + It)W. = Tin + Tout - (Be + Bt)W
        // W. = Tin + Tout - (Be + Bt)W / (Ie + It)

        float currentLinkedSpeed = EngineSpeed;

        // Damping
        float currentEngineDampingTorque = engineDamping * currentLinkedSpeed;
        float currentTransmissionDampingTorque = GetTotalTransmissionDampingOnInput() * currentLinkedSpeed;
        float dampingTorque = currentEngineDampingTorque + currentTransmissionDampingTorque;

        // Torque to acceleration
        float linkedAcceleration = (EngineTorque + GetTransmissionTorqueOnInput() - dampingTorque) / (engineMOI + GetTotalTransmissionMOIOnInput());
        currentLinkedSpeed += linkedAcceleration * Time.fixedDeltaTime;

        EngineSpeed = currentLinkedSpeed;
        TransmissionInputSpeed = currentLinkedSpeed;

        // Calculate torque on clutch
        // Tc = -IeTout + ItTin - (ItBe - IeBt)W   /   (Ie + It)
        float part1 = engineMOI * GetTransmissionTorqueOnInput();
        float part2 = GetTotalTransmissionMOIOnInput() * EngineTorque;
        float part3 = -((GetTotalTransmissionMOIOnInput() * engineDamping) - (engineMOI * GetTotalTransmissionDampingOnInput())) * currentLinkedSpeed;
        float part4 = engineMOI + GetTotalTransmissionMOIOnInput();

        float torqueThroughClutch = (part1 + part2 + part3) / part4;

        if (Mathf.Abs(torqueThroughClutch) > GetMaxStaticClutchForce())
        {
            ClutchLocked = false;
        }

        float energyGenerated = torqueThroughClutch * clutchFrictionToHeat;
        clutchHeatModule.AddHeat(energyGenerated * 0.000526565076466f * Time.fixedDeltaTime); 
    }


    private float GetMaxKineticClutchForce()
    {
        return clutchKineticFrictionConstant * ClutchAmount;
    }


    private float GetMaxStaticClutchForce()
    {
        return clutchKineticFrictionConstant * ClutchAmount;
    }


    private float GetTotalTransmissionMOIOnInput()
    {
        return transmissionInputMOI + (transmissionOuputMOI / Mathf.Pow(GetGearRatio(), 2f));
    }


    private float GetTotalTransmissionDampingOnInput()
    {
        return transmissionInputDamping + (transmissionOutputDamping / Mathf.Pow(GetGearRatio(), 2f));
    }


    private float GetTransmissionTorqueOnInput()
    {
        return TorqueOnTransmissionOutput * GetGearRatio();
    }


    public void OnThrottleChanged(float value)
    {
        Throttle = value;
    }


    public void OnClutchChanged(float value)
    {
        ClutchAmount = value;
    }


    public float GetGearRatio()
    {
        return gearRatios[CurrentGear];
    }


    public float GetTransmissionOutputSpeed()
    {
        return TransmissionInputSpeed / GetGearRatio();
    }


    public bool SetGear(int newGear)
    {
        if (ClutchAmount >= float.Epsilon)
        {
            return false;
        }

        float outputSpeed = TransmissionInputSpeed / GetGearRatio();

        CurrentGear = newGear;

        TransmissionInputSpeed = outputSpeed * GetGearRatio();

        return true;
    }


    public void Downshift()
    {
        int newGear = CurrentGear - 1;
        if (newGear >= 0)
        {
            SetGear(newGear);
        }
    }


    public void Upshift()
    {
        int newGear = CurrentGear + 1;
        if (newGear < gearRatios.Length)
        {
            SetGear(newGear);
        }
    }
}
