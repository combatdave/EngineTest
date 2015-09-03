using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class HeatModule : MonoBehaviour
{
    public string moduleName;
    public Text temperatureReadoutText;

    public float ambientTemperature = 20f;

    public float maxSafeTemperature = 200f;

    public float temperatureDissapationRate = 0.01f;

    public float CurrentTemperature {get; private set;}


    void Awake()
    {
        CurrentTemperature = ambientTemperature;
    }


	void FixedUpdate()
	{
        float temperatureDifference = ambientTemperature - CurrentTemperature;
        CurrentTemperature += temperatureDifference * temperatureDissapationRate * Time.fixedDeltaTime;

        if (temperatureReadoutText != null)
        {
            temperatureReadoutText.color = HSBColor.Lerp(HSBColor.FromColor(Color.green), HSBColor.FromColor(Color.red), GetSafeness()).ToColor();

            string text = moduleName + ": " + CurrentTemperature.ToString("0") +"c";
            if (GetSafeness() >= 1f)
            {
                text += "!!!";
            }
            temperatureReadoutText.text = text;
        }
	}


    public void AddHeat(float amount)
    {
        CurrentTemperature += amount;
    }


    public float GetSafeness()
    {
        return Mathf.Clamp01((CurrentTemperature - ambientTemperature) / (maxSafeTemperature - ambientTemperature));
    }
}
