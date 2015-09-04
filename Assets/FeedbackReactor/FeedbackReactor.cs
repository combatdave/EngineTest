using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class FeedbackReactor : MonoBehaviour
{
    public AnimationCurve accelerationByPosition;
    public AnimationCurve dampingByPosition;
    public AnimationCurve fluxMultiplierByPosition;

    public float currentPosition;
    public float currentVelocity;

    public Slider positionReadoutSlider;
    public Text positionReadoutText;
    public float maxPositionReadout = 200f;

    public Text velocityReadoutText;

    public float accelerationFromCatalyst;
    public float maxAccelerationFromCatalyst = 3f;
    public Text catalystText;

    public Text dampingText;
    private float controlRodSetting = 0f;
    public float maxControlRodDamping = 0.2f;
    public float DampingFromControlRods {
        get
        {
            return controlRodSetting * maxControlRodDamping;
        }
    }

    private float noisePos;
    public float noiseRate = 1f;
    public float noiseScaleAtMinExtraction = 0.1f;
    public float noiseScaleAtMaxExtraction = 0.7f;

    public bool panicFluidFlowEnabled;
    public float currentPanicFluidAmount = 0f;
    public float panicFluidMaxFlowRate = 10f;
    private float currentPanicFluidRate = 0f;
    public float panicFluidDissipationRate = 1f;
    public float maxDissipationPerSecond = 1f;
    public float maxAccelerationFromPanicFluidFlow = -1f;
    public Text panicFlowButtonText;
    public Text panicFluidText;
    public float betaFluidSaturationLevel = 20f;
    public float betaFluidSaturationFluxDamping = 0.1f;

    public float extractorAmount = 0f;
    public Text extractorText;

    public float currentFlux = 0f;
    public Text currentFluxText;


	void FixedUpdate()
	{
        float noiseValue = 1f - (Mathf.PerlinNoise(noisePos, 0f) * 2f);
        float randomAcceleration = noiseValue * Mathf.Lerp(noiseScaleAtMinExtraction, noiseScaleAtMinExtraction, extractorAmount);
        noisePos += noiseRate * Time.fixedDeltaTime;

        float dampingAmountFromPosition = dampingByPosition.Evaluate(currentPosition);
        float dampingAcceleration = -currentVelocity * (DampingFromControlRods + dampingAmountFromPosition);

        float accelerationFromPanicFluid = maxAccelerationFromPanicFluidFlow * currentPanicFluidRate;

        float acceleration = accelerationByPosition.Evaluate(currentPosition) + accelerationFromCatalyst + dampingAcceleration + randomAcceleration + accelerationFromPanicFluid;

        currentVelocity += acceleration * Time.fixedDeltaTime;

        float deltaPos = currentVelocity * Time.fixedDeltaTime;

        if (currentPosition + deltaPos < 0f)
        {
            deltaPos = -currentPosition;
            currentVelocity = deltaPos / Time.fixedDeltaTime;
        }

        currentPosition += deltaPos;


        if (panicFluidFlowEnabled)
        {
            currentPanicFluidRate += (1f - currentPanicFluidRate) * Time.fixedDeltaTime;
        }
        else
        {
            currentPanicFluidRate -= currentPanicFluidRate * Time.fixedDeltaTime;
        }
        currentPanicFluidAmount += panicFluidMaxFlowRate * currentPanicFluidRate * Time.fixedDeltaTime;

        float amountToDissipate = Mathf.Clamp(currentPanicFluidAmount * panicFluidDissipationRate, 0f, maxDissipationPerSecond);
        currentPanicFluidAmount -= amountToDissipate * Time.fixedDeltaTime;

        float controlRodFluxModifier = 1f - (controlRodSetting * 0.5f);
        float bFluidFluxModifier = 1f - ((1f - betaFluidSaturationFluxDamping) * Mathf.Clamp(currentPanicFluidAmount, 0f, betaFluidSaturationLevel) / betaFluidSaturationLevel);

        currentFlux = GetFluxFromPositionWithoutModifiers(currentPosition) * controlRodFluxModifier * bFluidFluxModifier;
	}


    public float GetFluxFromPositionWithoutModifiers(float position)
    {
        float positionFluxModifier = fluxMultiplierByPosition.Evaluate(position);
        return position * positionFluxModifier;
    }


    void Update()
    {
        positionReadoutSlider.value = currentPosition / maxPositionReadout;
        positionReadoutText.text = "Position: " + currentPosition.ToString("0.0");
        velocityReadoutText.text = "Velocity: " + currentVelocity.ToString("0.0");
        catalystText.text = "Acceleration from catalyst: " + accelerationFromCatalyst.ToString("0.0");
        dampingText.text = "Damping: " + DampingFromControlRods.ToString("0.0");
        panicFluidText.text = "B-Fluid level: " + currentPanicFluidAmount.ToString("0");
        extractorText.text = "Extractor: " + (extractorAmount * 100f).ToString("0") + "%";
        currentFluxText.text = "CURRENT FLUX: " + currentFlux.ToString("0.0") + "T";
    }


    public void OnCatalystSliderChanged(float value)
    {
        accelerationFromCatalyst = value * maxAccelerationFromCatalyst;
    }


    public void OnDampingSliderChanged(float value)
    {
        controlRodSetting = value;
    }


    public void OnPanicFluidButtonClicked()
    {
        panicFluidFlowEnabled = !panicFluidFlowEnabled;

        string statusText = panicFluidFlowEnabled ? "ON" : "OFF";
        panicFlowButtonText.GetComponentInChildren<Text>().text = "A-Fluid flow : " + statusText;
    }


    public void OnExtractorSliderChanged(float value)
    {
        extractorAmount = value;
    }
}
