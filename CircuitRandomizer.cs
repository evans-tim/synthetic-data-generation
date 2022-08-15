using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

/// <summary>
/// Creates a 2D layer of Resistor and Wire GameObjects on a Breadbord GameObject to form a random connected circuit
/// </summary>

[Serializable]
[AddRandomizerMenu("Perception/Circuit Randomizer")]

public class CircuitRandomizer : Randomizer
{   
    System.Random random = new System.Random();  // to generate random numbers


    [Tooltip("The breadboard to be placed by this Randomizer.")]
    public GameObjectParameter breadboard;
    [Tooltip("The edge wires starting or terminating in the power rail")]
    public GameObjectParameter edgeWires;

    //GameObjects to be displayed
    GameObject breadboardContainer;
    GameObject edgeContainer;
    //used to transform and rotate GameObjects
    GameObjectOneWayCache breadboardCache;
    GameObjectOneWayCache edgeCache;
    

    float verticalHoleDistance = 0.635f; // y distance between each breadboard hole (3.532 - -3.461) / 11
    float horizontalHoleDistance =  0.635f;  // x distance between each breadboard hole (19.685 - -19.694) / 62 

    /// <inheritdoc/> 
    protected override void OnAwake()
    {
        breadboardContainer = new GameObject("Breadboard");
        breadboardContainer.transform.parent = scenario.transform;  // transform relative to the fixed length scenario
        breadboardCache = new GameObjectOneWayCache(
        breadboardContainer.transform, breadboard.categories.Select(element => element.Item1).ToArray());  

        edgeContainer = new GameObject("Edge Wires");
        edgeContainer.transform.parent = scenario.transform;
        edgeCache = new GameObjectOneWayCache(
            edgeContainer.transform, edgeWires.categories.Select(element => element.Item1).ToArray());
    }

    /// <summary>
    /// Generates a foreground layer of wire and resistor objects at the start of each scenario iteration
    /// The objects form a connected circuit on the breadboard 
    /// </summary>
    protected override void OnIterationStart()
    {  
        Debug.Log(breadboard.GetCategoryCount());
        //place and scale breadboard 
        var breadboardInstance = breadboardCache.GetOrInstantiate(breadboard.Sample()); 
        breadboardInstance.transform.position = new Vector3(0, 0, 0);
        breadboardInstance.transform.rotation = Quaternion.Euler(-180, 90, -90);
        breadboardInstance.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        

        int powerRailX = random.Next(50);  // randomly pick which part of the power rail to start the circuit

        var bottomRail = edgeCache.GetOrInstantiate(edgeWires.Sample());
        bottomRail.transform.position = new Vector3(18.4f - (((powerRailX/5)+powerRailX)*verticalHoleDistance), -5.74f , -0.26f);

        bottomRail.transform.rotation = Quaternion.Euler(0, 0, 0);
    } 

    /// <summary>
    /// Deletes generated foreground objects after each scenario iteration is complete
    /// </summary>
    protected override void OnIterationEnd()
    {
     breadboardCache.ResetAllObjects();
     edgeCache.ResetAllObjects();
    }
}