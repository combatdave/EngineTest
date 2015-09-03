using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class Engine2 : MonoBehaviour
{
    public Text torqueText;
    public Text engineOutputRPMText;
    public Text transmissionInputRPMText;
    public Text clutchText;

    public GameObject engineOutputVisual;
    public GameObject transmissionInputVisual;

    public float maxTorque = 200f;
    public float engineTorque;

    public float engineSpeed;
    public float transmissionInputSpeed;

    public float engineMOI = 10f;
    public float transmissionMOI = 1000f;

    public float engineDamping;
    public float transmissionDamping;

    public bool clutchLocked = true;

    public float torqueOnTransmission = 0f;

    public float magicClutchConstant = 1000f;

    public float clutchAmount = 0f;

	void Update()
	{
        if (clutchLocked)
        {
            UpdateClutchLocked();
        }
        else
        {
            UpdateCluchSlipping();
        }


        UpdateEngineOutputVisuals();
        UpdateTransmissionInputVisuals();

        engineOutputRPMText.text = "Engine Output RPM: " + SpeedToRPM(engineSpeed);
        transmissionInputRPMText.text = "Transmission Input RPM: " + SpeedToRPM(transmissionInputSpeed);
	}


    private void UpdateCluchSlipping()
    {
        // Gear ratio is 1, clutch is slipping
        // IeWe. = Tin - BeWe - Tcl
        // ItWt. = Tout - BtWt + Tcl

        // Tfmaxk = (constant) * clutch friction force
        // Tcl = sign(We - Wt) * Tfmaxk

        // We. = (Tin - BeWe - Tcl) / Ie
        // Wt. = (Tout - BtWt + Tcl) / It

        float clutchForce = GetMaxClutchForce();

        float torqueThroughClutch = Mathf.Sign(engineSpeed - transmissionInputSpeed) * clutchForce;

        float engineTorqueWithoutClutch = engineTorque - (engineDamping * engineSpeed);
        float transmissionTorqueWithoutClutch = torqueOnTransmission - (transmissionDamping * transmissionInputSpeed);

        float engineAcceleration = (engineTorqueWithoutClutch - torqueThroughClutch) / engineMOI;
        float transmissionInputAcceleration = (transmissionTorqueWithoutClutch + torqueThroughClutch) / transmissionMOI;

        float deltaSpeedBefore = engineSpeed - transmissionInputSpeed;

        engineSpeed += engineAcceleration * Time.deltaTime;
        transmissionInputSpeed += transmissionInputAcceleration * Time.deltaTime;

        float deltaSpeedAfter = engineSpeed - transmissionInputSpeed;

        if (Mathf.Sign(deltaSpeedBefore) != Mathf.Sign(deltaSpeedAfter))
        {
            engineSpeed = transmissionInputSpeed = (engineSpeed + transmissionInputSpeed) * 0.5f;
            clutchLocked = true;
        }
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

        float currentLinkedSpeed = engineSpeed;

        float linkedAcceleration = (engineTorque + torqueOnTransmission - ((engineDamping + transmissionDamping) * currentLinkedSpeed)) / (engineMOI + transmissionMOI);

        currentLinkedSpeed += linkedAcceleration * Time.deltaTime;

        engineSpeed = currentLinkedSpeed;
        transmissionInputSpeed = currentLinkedSpeed;

        if (clutchAmount < 1f)
        {
            clutchLocked = false;
        }
    }


    private float GetMaxClutchForce()
    {
        float clutchForce = magicClutchConstant * clutchAmount;
        return clutchForce;
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
        angle += transmissionInputSpeed * Mathf.Rad2Deg * Time.deltaTime;
        transmissionInputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }


    public void OnThrottleChanged(float value)
    {
        engineTorque = value * maxTorque;

        torqueText.text = "Torque: " + engineTorque;
    }


    public void OnClutchChanged(float value)
    {
        clutchAmount = value;

        clutchText.text = "Clutch: " + clutchAmount;
    }


    public float SpeedToRPM(float speed)
    {
        return 60f * speed / (2f * Mathf.PI);
    }
}
