using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class FeedbackReactorVisuals : MonoBehaviour
{
    private FeedbackReactor reactor;

    public float glowLightMaxIntensity = 8f;
    public Light glowLight;

    public GameObject graphRoot;
    public GameObject graphBlockPrefab;
    public GameObject indicatorBlockPrefab;

    private GameObject indicatorBlock;

    float xScale = 0.005f;
    float yScale = 0.001f;
    float zScale = 0.01f;


	void Awake()
	{
        reactor = GetComponentInParent<FeedbackReactor>();

        for (int i = 0; i < reactor.maxPositionReadout; i++)
        {
            float height = reactor.GetFluxFromPositionWithoutModifiers(i) * yScale;

            GameObject block = GameObject.Instantiate(graphBlockPrefab);
            block.transform.parent = graphRoot.transform;
            block.transform.rotation = Quaternion.identity;
            block.transform.localPosition = new Vector3(i * xScale, height * 0.5f, 0f);
            block.transform.localScale = new Vector3(xScale, height, zScale);
        }

        indicatorBlock = GameObject.Instantiate(indicatorBlockPrefab);
        indicatorBlock.transform.parent = graphRoot.transform;
        indicatorBlock.transform.localRotation = Quaternion.identity;
        indicatorBlock.transform.localScale = new Vector3(0.01f, 0.02f, 0.01f);
	}


	void Update()
	{
        glowLight.intensity = glowLightMaxIntensity * reactor.currentPosition / reactor.maxPositionReadout;

        indicatorBlock.transform.localPosition = new Vector3(reactor.currentPosition * xScale, 0.01f, -zScale);
	}
}
