using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class Engine1 : MonoBehaviour
{
    public Text throttleText;
    public Text torqueText;
    public Text rpmText;
    public Text clutchText;
    public Text loadRPMText;

    public AnimationCurve torqueCurve;

    public float throttle;

    public float internalMomentOfInertia;

    public float engineVelocity;

    public float internalFriction = 0.2f;

    public GameObject propVisual;
    public GameObject loadVisual;

    public float loadMomentOfInertia;
    public float transmissionOutputVelocity;
    public float transmissionGearRatio = 5f;
    public float loadFriction = 0.1f;
    public float clutch;

    public bool broken = false;

    public float engineTorqueMultiplier = 2f;


	void Update()
	{
        float rpm = 60f * engineVelocity / (2f * Mathf.PI);
        float engineTorque = torqueCurve.Evaluate(rpm) * throttle * engineTorqueMultiplier;

        if (broken)
        {
            OnClutchChanged(0f);
        }

        float MOIAsSeenByEngine = internalMomentOfInertia;
        float engineTorqueFromFriction = internalFriction * -Mathf.Sign(engineVelocity);

        float totalEngineTorque = engineTorque + engineTorqueFromFriction;

        float engineAcceleration = totalEngineTorque / MOIAsSeenByEngine;
        engineVelocity += engineAcceleration * Time.deltaTime;


        float loadTorqueFromFriction = (loadFriction * -Mathf.Sign(transmissionOutputVelocity));
        if (transmissionOutputVelocity <= 0f)
        {
            loadTorqueFromFriction = 0f;
        }
        float MOIAsSeenByLoad = loadMomentOfInertia + (clutch * internalMomentOfInertia);
        float loadAcceleration = loadTorqueFromFriction / MOIAsSeenByLoad;
        transmissionOutputVelocity += loadAcceleration * Time.deltaTime;


        // Assume the engine is faster
        float deltaV = engineVelocity - (transmissionOutputVelocity * transmissionGearRatio);


        //if (clutch > 0f && transmissionGearRatio > 0f)
        //{
        //    float velocityToBeAddedToLoad = -(transmissionOutputVelocity - (engineVelocity / transmissionGearRatio)) * 0.5f;
        //    float velocityToBeAddedToEngine = -(engineVelocity - (transmissionOutputVelocity * transmissionGearRatio)) * 0.5f;

        //    // 0 when clutch is 0, 1 when clutch is 1
        //    float changeMultiplier = Mathf.Pow(clutch, 3f);
        //    transmissionOutputVelocity += velocityToBeAddedToLoad * changeMultiplier;
        //    engineVelocity += velocityToBeAddedToEngine * changeMultiplier;
        //}

        //if (torqueOnEngineFromLoad < -3000f)
        //{
        //    broken = true;
        //}

        propVisual.SetActive(!broken);

        UpdateLoadVisuals();

        torqueText.text = "Torque: " + engineTorque;
        rpmText.text = "RPM: " + rpm;
        loadRPMText.text = "Load RPM: " + (60f * transmissionOutputVelocity / (2f * Mathf.PI));
        UpdatePropVisual();
	}

    private void UpdateLoadVisuals()
    {
        float loadAngle = loadVisual.transform.localRotation.eulerAngles.z;
        loadAngle += transmissionOutputVelocity * Mathf.Rad2Deg * Time.deltaTime;
        loadVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, loadAngle));
    }


    private void UpdatePropVisual()
    {
        float propAngle = propVisual.transform.localRotation.eulerAngles.z;
        propAngle += engineVelocity * Mathf.Rad2Deg * Time.deltaTime;
        propVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, propAngle));
    }


    public void OnThrottleChanged(float value)
    {
        throttle = value;

        throttleText.text = "Throttle: " + throttle;
    }


    public void OnClutchChanged(float value)
    {
        clutch = value;

        clutchText.text = "Clutch: " + clutch;
    }
}
