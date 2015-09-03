using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class SteamPipe : MonoBehaviour
{
    public float maxPressureThroughput = 2f;
    public float m3PerSecondMax = 0.5f;

    public float valveOpenness;

    public float currentPressureThroughput;

    public Boiler1 boiler;
    public EngineWithGear engine;

    public Text maxPressureThroughputText;
    public Text currentPressureThroughputText;


	void FixedUpdate()
	{
        float pressureToConsume = Mathf.Clamp(boiler.tankPressure, 0f, valveOpenness * maxPressureThroughput);

        boiler.ConsumePressure(pressureToConsume * Time.fixedDeltaTime, m3PerSecondMax);

        float throttleFromPressure = Mathf.Pow(pressureToConsume / maxPressureThroughput, 2f);

        engine.Throttle = throttleFromPressure;

        currentPressureThroughputText.text = "Current Pressure Throughput: " + pressureToConsume.ToString("0.0");
	}


    public void OnOpennessChanged(float value)
    {
        valveOpenness = value;

        maxPressureThroughputText.text = "Max Pressure Throughput: " + (valveOpenness * maxPressureThroughput).ToString("0.0");
    }
}
