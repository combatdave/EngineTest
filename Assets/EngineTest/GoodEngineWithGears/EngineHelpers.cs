using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class EngineHelpers : MonoBehaviour
{
    public static float SpeedToRPM(float speed)
    {
        return 60f * speed / (2f * Mathf.PI);
    }
}
