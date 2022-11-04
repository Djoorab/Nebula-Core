using System.Collections;
using UnityEngine;
using System.Threading;
using System;
using System.IO;

public class NebulaOdorDiffuser : MonoBehaviour
{
    //Define which odor you are going to diffuse. You can spread different smelling objects in the scene
    public enum AtomizerList { Left, Right, Other };
    public AtomizerList atomizer;
    public enum DiffusionMode { Progressive, Boolean, Other };
    public DiffusionMode diffusionMode;

    private string enterString;
    private string exitString;

    private Transform playerHead;

    //Define the distance (in m) between the player head and the object before the start of the diffusion
    private float distanceBetweenGameObjectAndPlayer;
    [HideInInspector]
    private float dutyCycle;
    private float previousDutyCycle;
    //Define min and max duty cycle used for the progressive diffusion
    public int minimumDutyCycle = 1;
    public int maximumDutyCycle = 30;
    //Adjust the PWM frequency of Nebula
    public string pwmFrequency = "100";
    public int booleanDutyCycle = 50;

    Vector3 originalPosition;
    Quaternion originalOrientation;

    private void Awake()
    {
        playerHead = GameObject.Find("NebulaManager").GetComponent<NebulaManager>().playerHead.transform;
        originalOrientation = gameObject.transform.rotation;
        originalPosition = gameObject.transform.position;
        //adjust instruction sent to the Arduino to dissociate left and right atomizer 
        switch (atomizer)
        {
            case AtomizerList.Left:
                enterString = "L\n";
                exitString = "l\n";
                break;
            case AtomizerList.Right:
                enterString = "R\n";
                exitString = "r\n";
                break;
            case AtomizerList.Other:
                break;
            default:
                Debug.Log("Incorrect atomizer location");
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            if (diffusionMode == DiffusionMode.Progressive)
            {
                ProgressiveDiffusionMode();
            }
            else if (diffusionMode == DiffusionMode.Boolean)
            {
                BooleanDiffusionMode();
            }
        }

        if (other.CompareTag("Floor"))
        {
            ResetGameObject();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            if (diffusionMode == DiffusionMode.Progressive)
            {
                NebulaManager.nebulaIsDiffusing = false;
                dutyCycle = 0;

                NebulaManager.NebulaSender(exitString);
                StopCoroutine(OdorDiffusionProgressiveMode());
                NebulaManager.currentDutyCycle = dutyCycle;
            }
            else if (diffusionMode == DiffusionMode.Boolean)
            {
                NebulaManager.nebulaIsDiffusing = false;
                dutyCycle = 0;
                NebulaManager.NebulaSender(exitString);
                NebulaManager.currentDutyCycle = dutyCycle;
            }
        }
    }

    private void ProgressiveDiffusionMode()
    {
        if (!NebulaGUI.manualOverride)
        {
            NebulaManager.nebulaIsDiffusing = true;
            NebulaManager.NebulaSender(enterString); //Send the start signal to Nebula 
            StartCoroutine(OdorDiffusionProgressiveMode()); //Start a coroutine in order to adjust odor strength in real time
        }
    }

    public float UpdateDistance()
    {
        //Calculate in real-time the distance between the object to smell and the player head
        distanceBetweenGameObjectAndPlayer = Vector3.Distance(playerHead.position, transform.position) - NebulaManager.playerHeadRadius/2;
        if (distanceBetweenGameObjectAndPlayer < 0) distanceBetweenGameObjectAndPlayer = 0;
        return distanceBetweenGameObjectAndPlayer;
    }

    //Coroutine for the linear diffusion mode
    private IEnumerator OdorDiffusionProgressiveMode()
    {
        while (!NebulaGUI.manualOverride)
        {
            yield return new WaitForSeconds(0.1f); //Avoid overflowding the Arduino
            dutyCycle = Mathf.Round((1 / UpdateDistance())); // Increase duty cycle according to distance
            if (dutyCycle <= minimumDutyCycle) dutyCycle = minimumDutyCycle;
            if (dutyCycle >= maximumDutyCycle) dutyCycle = maximumDutyCycle;
            if (dutyCycle != previousDutyCycle)
            {
                NebulaManager.NebulaSender("C" + pwmFrequency + ";" + dutyCycle);
                previousDutyCycle = dutyCycle;
                NebulaManager.currentDutyCycle = dutyCycle;
            }
        }
    }

    private void BooleanDiffusionMode()
    {
        dutyCycle = booleanDutyCycle;
        NebulaManager.currentDutyCycle = dutyCycle;
        NebulaManager.NebulaSender(enterString);
        NebulaManager.NebulaSender("C" + pwmFrequency + ";" + dutyCycle);
        NebulaManager.nebulaIsDiffusing = true;
    }

    public void ResetGameObject()
    {
        gameObject.transform.SetPositionAndRotation(originalPosition, originalOrientation);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }


    private void OnApplicationQuit()
    {
        StopAllCoroutines();
#if (!UNITY_ANDROID && UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        NebulaManager.NebulaSender("S");
        try
        {
            //Send stop value Nebula
            Thread.Sleep(200);
            NebulaManager.thread.Abort();
            NebulaManager.nebulaSerial.Close();
        }
        catch (Exception) {  }

#endif
    }
    }
