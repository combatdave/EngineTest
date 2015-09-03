using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class EngineVisuals : MonoBehaviour
{
    private EngineWithGear engine;

    public Text torqueText;
    public Text engineOutputRPMText;
    public Text transmissionInputRPMText;
    public Text clutchText;
    public Text gearText;
    public Text transmissionOutputRPMText;

    public GameObject engineOutputVisual;
    public GameObject transmissionInputVisual;
    public GameObject transmissionOutputVisual;

    public GameObject slipWarningIcon;
    public GameObject lockIcon;

    public float engineOutputClutchMinZ = 0.614f;
    public float engineOutputClutchMaxZ = 0.685f;


    void Awake()
    {
        if (engine == null)
        {
            engine = GetComponentInChildren<EngineWithGear>();
        }
    }


	void Update()
	{
        engineOutputRPMText.text = "Engine RPM: " + EngineHelpers.SpeedToRPM(engine.EngineSpeed).ToString("0");
        transmissionInputRPMText.text = "In RPM: " + EngineHelpers.SpeedToRPM(engine.TransmissionInputSpeed).ToString("0");
        transmissionOutputRPMText.text = "Out RPM: " + EngineHelpers.SpeedToRPM(engine.GetTransmissionOutputSpeed()).ToString("0");
        torqueText.text = "Torque: " + engine.EngineTorque;
        clutchText.text = "Clutch: " + engine.ClutchAmount;
        gearText.text = "" + (engine.CurrentGear + 1);

        Vector3 p = engineOutputVisual.transform.localPosition;
        p.z = Mathf.Lerp(engineOutputClutchMinZ, engineOutputClutchMaxZ, engine.ClutchAmount);
        engineOutputVisual.transform.localPosition = p;

        UpdateEngineOutputVisuals();
        UpdateTransmissionInputVisuals();
        UpdateTransmissionOutputVisuals();

        slipWarningIcon.SetActive(engine.ClutchAmount > 0f && !engine.ClutchLocked);
        lockIcon.SetActive(engine.ClutchLocked);
	}


    private void UpdateEngineOutputVisuals()
    {
        float angle = engineOutputVisual.transform.localRotation.eulerAngles.z;
        angle += engine.EngineSpeed * Mathf.Rad2Deg * Time.deltaTime;
        engineOutputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }


    private void UpdateTransmissionInputVisuals()
    {
        float angle = transmissionInputVisual.transform.localRotation.eulerAngles.z;
        angle += engine.TransmissionInputSpeed * Mathf.Rad2Deg * Time.deltaTime;
        transmissionInputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }


    private void UpdateTransmissionOutputVisuals()
    {
        float angle = transmissionOutputVisual.transform.localRotation.eulerAngles.z;
        angle += engine.GetTransmissionOutputSpeed() * Mathf.Rad2Deg * Time.deltaTime;
        transmissionOutputVisual.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
    }
}
