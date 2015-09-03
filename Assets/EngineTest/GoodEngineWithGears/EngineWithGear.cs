using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class EngineWithGear : MonoBehaviour
{
    public Text torqueText;
    public Text engineOutputRPMText;
    public Text transmissionInputRPMText;
    public Text clutchText;
    public Text gearText;
    public Text transmissionOutputRPMText;

    public GameObject engineOutputVisual;
    public GameObject transmissionInputVisual;
    public GameObject transmissionOutputVisual;

    public float maxTorque = 200f;
    public float engineTorque;

    public float engineSpeed;
    public float transmissionInputSpeed;

    public float engineMOI = 10f;
    public float transmissionInputMOI = 10f;
    public float transmissionOuputMOI = 1000f;

    public float engineDamping = 1f;
    public float transmissionInputDamping = 1f;
    public float transmissionOutputDamping = 10f;

    public bool clutchLocked = true;

    public float torqueOnTransmissionOutput = 0f;

    public float clutchKineticFrictionConstant = 500f;
    public float clutchStaticFrictionConstant = 600f;

    public float clutchAmount = 0f;

    public float engineOutputMinZ = 0.614f;
    public float engineOutputMaxZ = 0.685f;


    void FixedUpdate()
    {
        if (clutchLocked)
        {
            UpdateClutchLocked();
        }
        else
        {
            UpdateClutchSlipping();
        }


        UpdateEngineOutputVisuals();
        UpdateTransmissionInputVisuals();
        UpdateTransmissionOutputVisuals();

        engineOutputRPMText.text = "Engine RPM: " + SpeedToRPM(engineSpeed).ToString("0");
        transmissionInputRPMText.text = "In RPM: " + SpeedToRPM(transmissionInputSpeed).ToString("0");
        transmissionOutputRPMText.text = "Out RPM: " + SpeedToRPM(GetTransmissionOutputSpeed()).ToString("0");
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
        float engineTorqueWithoutClutch = engineTorque - (engineDamping * engineSpeed);
        float transmissionDamping = GetTotalTransmissionDampingOnInput() * transmissionInputSpeed;
        float transmissionTorqueWithoutClutch = GetTransmissionTorqueOnInput() - transmissionDamping;

        // Torques on both sides (not clutch) into angular accelerations
        float engineAccelerationNoClutch = engineTorqueWithoutClutch / engineMOI;
        float transmissionInputAccelerationNoClutch = transmissionTorqueWithoutClutch / GetTotalTransmissionMOIOnInput();

        engineSpeed += engineAccelerationNoClutch * Time.fixedDeltaTime;
        transmissionInputSpeed += transmissionInputAccelerationNoClutch * Time.fixedDeltaTime;

        // Torque on the clutch
        float clutchForce = GetMaxKineticClutchForce();
        float torqueThroughClutch = 0f;
        torqueThroughClutch = Mathf.Sign(engineSpeed - transmissionInputSpeed) * clutchForce;

        // Torques on both sides (including clutch torque) into angular accelerations
        float engineAccelerationFromClutch = -torqueThroughClutch / engineMOI;
        float transmissionInputAccelerationFromClutch = torqueThroughClutch / GetTotalTransmissionMOIOnInput();

        float totalAttemptedDeltaSpeed = (Mathf.Abs(engineAccelerationFromClutch) + Mathf.Abs(transmissionInputAccelerationFromClutch)) * Time.fixedDeltaTime;
        float allowedDeltaSpeed = Mathf.Abs(engineSpeed - transmissionInputSpeed);

        float torqueScale = 1f;
        if (totalAttemptedDeltaSpeed > allowedDeltaSpeed)
        {
            torqueScale = allowedDeltaSpeed / totalAttemptedDeltaSpeed;
            clutchLocked = true;
        }

        engineAccelerationFromClutch *= torqueScale;
        transmissionInputAccelerationFromClutch *= torqueScale;

        engineSpeed += engineAccelerationFromClutch * Time.fixedDeltaTime;
        transmissionInputSpeed += transmissionInputAccelerationFromClutch * Time.fixedDeltaTime;

        // Heat
        float actualTorqueThroughClutch = torqueThroughClutch * torqueScale;
        float speedDifference = engineSpeed - transmissionInputSpeed;
        float energyGenerated = actualTorqueThroughClutch * speedDifference * clutchFrictionToHeat;

        totalHeat += energyGenerated * 0.000526565076466f * Time.fixedDeltaTime; 
    }

    public float clutchFrictionToHeat = 0.1f;
    public float totalHeat = 20f;

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

        float currentLinkedSpeed = engineSpeed;

        // Damping
        float currentEngineDampingTorque = engineDamping * currentLinkedSpeed;
        float currentTransmissionDampingTorque = GetTotalTransmissionDampingOnInput() * currentLinkedSpeed;
        float dampingTorque = currentEngineDampingTorque + currentTransmissionDampingTorque;

        // Torque to acceleration
        float linkedAcceleration = (engineTorque + GetTransmissionTorqueOnInput() - dampingTorque) / (engineMOI + GetTotalTransmissionMOIOnInput());
        currentLinkedSpeed += linkedAcceleration * Time.fixedDeltaTime;

        engineSpeed = currentLinkedSpeed;
        transmissionInputSpeed = currentLinkedSpeed;

        // Calculate torque on clutch
        // Tc = -IeTout + ItTin - (ItBe - IeBt)W   /   (Ie + It)
        float part1 = engineMOI * GetTransmissionTorqueOnInput();
        float part2 = GetTotalTransmissionMOIOnInput() * engineTorque;
        float part3 = -((GetTotalTransmissionMOIOnInput() * engineDamping) - (engineMOI * GetTotalTransmissionDampingOnInput())) * currentLinkedSpeed;
        float part4 = engineMOI + GetTotalTransmissionMOIOnInput();

        float torqueThroughClutch = (part1 + part2 + part3) / part4;

        if (Mathf.Abs(torqueThroughClutch) > GetMaxStaticClutchForce())
        {
            clutchLocked = false;
        }
    }


    private float GetMaxKineticClutchForce()
    {
        return clutchKineticFrictionConstant * clutchAmount;
    }


    private float GetMaxStaticClutchForce()
    {
        return clutchKineticFrictionConstant * clutchAmount;
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
        return torqueOnTransmissionOutput * GetGearRatio();
    }


    private void UpdateEngineOutputVisuals()
    {
        float angle = engineOutputVisual.transform.localRotation.eulerAngles.z;
        angle += engineSpeed * Mathf.Rad2Deg * Time.deltaTime;
        engineOutputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }


    private void UpdateTransmissionInputVisuals()
    {
        float angle = transmissionInputVisual.transform.localRotation.eulerAngles.z;
        angle += transmissionInputSpeed * Mathf.Rad2Deg * Time.fixedDeltaTime;
        transmissionInputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }


    private void UpdateTransmissionOutputVisuals()
    {
        float angle = transmissionOutputVisual.transform.localRotation.eulerAngles.z;
        angle += GetTransmissionOutputSpeed() * Mathf.Rad2Deg * Time.fixedDeltaTime;
        transmissionOutputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }


    public void OnThrottleChanged(float value)
    {
        engineTorque = value * maxTorque;

        torqueText.text = "Torque: " + engineTorque;
    }


    public void OnClutchChanged(float value)
    {
        clutchAmount = value;

        Vector3 p = engineOutputVisual.transform.localPosition;
        p.z = Mathf.Lerp(engineOutputMinZ, engineOutputMaxZ, clutchAmount);
        engineOutputVisual.transform.localPosition = p;

        clutchText.text = "Clutch: " + clutchAmount;
    }


    public float SpeedToRPM(float speed)
    {
        return 60f * speed / (2f * Mathf.PI);
    }


    public float GetGearRatio()
    {
        return gearRatios[currentGear];
    }


    public float GetTransmissionOutputSpeed()
    {
        return transmissionInputSpeed / GetGearRatio();
    }


    public int currentGear = 0;
    public float[] gearRatios = {5f, 2f, 1.2f, 0.8f, 0.4f};

    public bool SetGear(int newGear)
    {
        if (clutchAmount >= float.Epsilon)
        {
            return false;
        }

        float outputSpeed = transmissionInputSpeed / GetGearRatio();

        currentGear = newGear;

        transmissionInputSpeed = outputSpeed * GetGearRatio();

        gearText.text = "" + (currentGear + 1);

        return true;
    }


    public void Downshift()
    {
        int newGear = currentGear - 1;
        if (newGear >= 0)
        {
            SetGear(newGear);
        }
    }


    public void Upshift()
    {
        int newGear = currentGear + 1;
        if (newGear < gearRatios.Length)
        {
            SetGear(newGear);
        }
    }
}
