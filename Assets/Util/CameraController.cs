using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CameraController : MonoBehaviour
{
    public float sensitivity = 1f;

    private Vector3 lastMousePos;

	void Update()
	{
        Vector3 currentMousePos = Input.mousePosition;

	    if (Input.GetMouseButton(1))
        {
            Vector3 delta = currentMousePos - lastMousePos;

            transform.Rotate(Vector3.up, delta.x * sensitivity, Space.World);
        }

        lastMousePos = currentMousePos;
	}
}
