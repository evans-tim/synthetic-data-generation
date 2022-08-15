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

    //GameObjects to be displayed
    GameObject breadboardContainer;

    //used to transform and rotate GameObjects
    GameObjectOneWayCache breadboardCache;

    

    float verticalHoleDistance = 0.63572f; // y distance between each breadboard hole (3.532 - -3.461) / 11
    float horizontalHoleDistance =  0.63514516129f;  // x distance between each breadboard hole (19.685 - -19.694) / 62 

    /// <inheritdoc/> 
    protected override void OnAwake()
    {
        breadboardContainer = new GameObject("Breadboard");
        breadboardContainer.transform.parent = scenario.transform;  // transform relative to the fixed length scenario
        breadboardCache = new GameObjectOneWayCache(
        breadboardContainer.transform, breadboard.categories.Select(element => element.Item1).ToArray());  
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
    }

    /// <summary>
    /// Deletes generated foreground objects after each scenario iteration is complete
    /// </summary>
    protected override void OnIterationEnd()
    {
     breadboardCache.ResetAllObjects();
    }
}