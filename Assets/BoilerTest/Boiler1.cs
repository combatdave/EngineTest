using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class Boiler1 : MonoBehaviour
{
    public float fuelFlow;

    public float minFuelFlow = 2f;
    public float maxFuelFlow = 10f;

    public Text fuelFlowText;
    public Text pressureText;
    public Text pressureReleaseText;
    public Text waterLevelText;
    public ParticleSystem pressureReleaseParticles;
    public Button waterFlowButton;

    public AnimationCurve litersOfWaterToSteamPerSecondByTemperature;

    public float fireFuelToTemperatureMultiplier = 60f;
    public float fireplaceHeatUpRate = 0.1f;

    public float fireplaceToWaterHeatTransferRate = 0.1f;
    public float fireplaceToSteamHeatTransferRate = 0.1f;

    public float temperatureOfFire = 0f;

    public float tankCapacityInLitres = 1000f;
    public float waterInTank = 500f;
    public float steamInTank = 0f;

    public float tankPressure;

    public float maxPressureRelease = 1f;
    public float currentMaxPressureRelease;

    public HeatModule fireplace;
    public HeatModule water;
    public HeatModule steam;

    public bool waterFlowEnabled;
    public float waterFlowLitresPerSecond = 1f;
    public float waterFlowRate = 0f;


    void Awake()
    {
        OnFuelFlowChanged(0f);
    }


	void FixedUpdate()
	{
        // Fire: Temperature is proportional to fuel flow
        temperatureOfFire = fuelFlow * fireFuelToTemperatureMultiplier;
        fireplace.AddHeat(temperatureOfFire * fireplaceHeatUpRate * Time.fixedDeltaTime);

        // Movement of heat to water
        float temperatureDifferenceToWater = fireplace.CurrentTemperature - water.CurrentTemperature;
        if (temperatureDifferenceToWater > 0f)
        {
            float amountToTransfer = temperatureDifferenceToWater * fireplaceToWaterHeatTransferRate * Time.fixedDeltaTime;

            float requiredWater = 100f;
            float heatTransferAmountByWater = Mathf.Clamp01(waterInTank / requiredWater);

            fireplace.ConsumeHeat(amountToTransfer);
            water.AddHeat(amountToTransfer * 0.8f);
        }

        float temperatureDifferenceToSteam = fireplace.CurrentTemperature - steam.CurrentTemperature;
        if (temperatureDifferenceToSteam > 0f)
        {
            float waterToSteamTransfer = 0.1f;
            float amountToTransfer = temperatureDifferenceToSteam * fireplaceToSteamHeatTransferRate * Time.fixedDeltaTime * waterToSteamTransfer;

            fireplace.ConsumeHeat(amountToTransfer);
            steam.AddHeat(amountToTransfer * 0.7f);
        }

        float pressureModifier = 1f + (tankPressure / 10f);

        float waterToSteam = litersOfWaterToSteamPerSecondByTemperature.Evaluate(water.CurrentTemperature) * Time.fixedDeltaTime * pressureModifier;
        waterToSteam = Mathf.Clamp(waterToSteam, 0f, waterInTank);
        waterInTank -= waterToSteam;
        steamInTank += waterToSteam;

        steam.AddHeat(waterToSteam * 10f * Time.fixedDeltaTime);
        water.ConsumeHeat(waterToSteam * 10f * Time.fixedDeltaTime);

        // Of the steam:
	    // pressure * volume = const * temperature
        // pressure = const * temperature / volume

        float volumeOfGas = tankCapacityInLitres - waterInTank;
        tankPressure = steamInTank * steam.CurrentTemperature / volumeOfGas;


        // delta pressure = const * fluid viscosity * length of pipe (const) * flow rate / pi * pipe diameter ^ 4
        // fluid viscosity = 0.2

        // Dpressure * pi * pipe diameter ^ 4 / const * fluid viscosity * length of pipe = flow rate

        // Release some pressure
        float pressureToRelease = Mathf.Clamp(tankPressure, 0f, currentMaxPressureRelease);
        if (pressureToRelease > float.Epsilon)
        {
            ConsumePressure(pressureToRelease * Time.fixedDeltaTime, 5f);
        }

        float maxEmissionRate = 100f;
        pressureReleaseParticles.emissionRate = Mathf.Lerp(0f, maxEmissionRate, pressureToRelease / maxPressureRelease);

        if (waterFlowEnabled)
        {
            waterFlowRate += (1f - waterFlowRate) * Time.fixedDeltaTime;
        }
        else
        {
            waterFlowRate -= waterFlowRate * Time.fixedDeltaTime;
        }

        float waterToAdd = waterFlowLitresPerSecond * waterFlowRate * Time.fixedDeltaTime;
        waterInTank += waterToAdd;
        //water.ConsumeHeat(waterToAdd * Time.fixedDeltaTime);
	}


    // pressureToRelease should be in terms of fixedDeltaTime
    public void ConsumePressure(float pressureToRelease, float m3PerSecond)
    {
        float flowRate = pressureToRelease * m3PerSecond;

        if (flowRate > steamInTank)
        {
            flowRate = steamInTank;
        }

        steamInTank -= flowRate;

        steam.ConsumeHeat(pressureToRelease);
    }


    void Update()
    {
        fuelFlowText.text = "Fuel flow rate: " + fuelFlow.ToString("0.0");
        pressureText.text = "Pressure: " + tankPressure.ToString("0.0");
        pressureReleaseText.text = "Pressure release: " + currentMaxPressureRelease.ToString("0.0");
        waterLevelText.text = "Water level: " + waterInTank.ToString("0") +"l";
    }


    public void OnFuelFlowChanged(float value)
    {
        fuelFlow = Mathf.Lerp(minFuelFlow, maxFuelFlow, value);
    }


    public void OnPressureReleaseChanged(float value)
    {
        currentMaxPressureRelease = value * maxPressureRelease;
    }


    public void OnWaterFlowButtonPressed()
    {
        waterFlowEnabled = !waterFlowEnabled;

        string statusText = waterFlowEnabled ? "ON" : "OFF";
        waterFlowButton.GetComponentInChildren<Text>().text = "Water Flow: " + statusText;
    }
}
