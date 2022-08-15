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
    [Tooltip("The middle wires starting and terminating in the middle section of breadboard")]
    public GameObjectParameter middleWires;

    //GameObjects to be displayed
    GameObject breadboardContainer;
    GameObject edgeContainer;
    GameObject middleContainer;
    //used to transform and rotate GameObjects
    GameObjectOneWayCache breadboardCache;
    GameObjectOneWayCache edgeCache;
    GameObjectOneWayCache middleCache;
    

    float verticalHoleDistance = 0.635f; // y distance between each breadboard hole (3.532 - -3.461) / 11
    float horizontalHoleDistance =  0.635f;  // x distance between each breadboard hole (19.685 - -19.694) / 62 

    float railRightX = 18.4f; //furthest right rail position x value

    float insideBottomRailY =  -5.08f;  // inside rail on the bottom y value
    float outsideBottomRailY = -5.74f;  // outside rail on the bottom y value
    float wireZ = -0.26f;  //z value for all wires and resistors

    // lower left reference for placing middle wires
    float  middleBottomLeftX = -19.67f;  
    float middleBottomLeftY = -3.49f;  

    //List to hold wires that fit from the inside power rail
    List<GameObject> insideRailList = new List<GameObject>
            {};
    //List to hold wires that fit from the outside power rail
    List<GameObject> outsideRailList = new List<GameObject>
            {};


    /// <summary>
    /// Method <c>placeMiddleWire()</c> 
    /// Places a wire in one of the options in the 63x10 breadboard grid
    /// </summary>
    /// <param name="x"> integer between 0-62</param>
    /// <param name="y"> integer between 0-9</param>
    /// <returns> void </returns>
    protected void placeMiddleWire(GameObject wire, int x, int y){
        Debug.Log(wire.name);
        Debug.Log(x);
        Debug.Log(y);
        if(y > 4){ //offset for the center line of breadboard where there are no holes 
            y = y + 2;
        }
        wire.transform.position = new Vector3(middleBottomLeftX + (horizontalHoleDistance*x), middleBottomLeftY + (verticalHoleDistance*y), wireZ);
        wire.transform.rotation = Quaternion.Euler(0, 0, -180);  // ensure rotated properly
    }

    protected void placeRandomEdgeWire(){
        int powerRailX = random.Next(50);  // randomly pick which part of the power rail to start the circuit

        int powerRailY = random.Next(2); // randomly pick to start on outside or inside rail

        if(powerRailY == 0){
            var edgeWire = edgeCache.GetOrInstantiate(outsideRailList[random.Next(4)]);
            edgeWire.transform.position = new Vector3(railRightX - (((powerRailX/5)+powerRailX)*verticalHoleDistance), outsideBottomRailY , wireZ);
        }
        else if(powerRailY == 1){
            var edgeWire = edgeCache.GetOrInstantiate(insideRailList[random.Next(4)]);
            edgeWire.transform.position = new Vector3(railRightX - (((powerRailX/5)+powerRailX)*verticalHoleDistance), insideBottomRailY , wireZ);
        }
    }

    /// <inheritdoc/> 
    protected override void OnAwake()
    {
        breadboardContainer = new GameObject("Breadboard");
        breadboardContainer.transform.parent = scenario.transform;  // transform relative to the fixed length scenario
        breadboardCache = new GameObjectOneWayCache(
        breadboardContainer.transform, breadboard.categories.Select(element => element.Item1).ToArray());  

        edgeContainer = new GameObject("Edge Wires");
        edgeContainer.transform.parent = scenario.transform;  // transform relative to the fixed length scenario
        edgeCache = new GameObjectOneWayCache(
            edgeContainer.transform, edgeWires.categories.Select(element => element.Item1).ToArray());

        middleContainer = new GameObject("Middle Wires");
        middleContainer.transform.parent = scenario.transform;  // transform relative to the fixed length scenario
        middleCache = new GameObjectOneWayCache(
            middleContainer.transform, middleWires.categories.Select(element => element.Item1).ToArray()); 

        // Add the wires to the lists representing the wires allowed for certain positions
        for (var i = 0; i < edgeWires.GetCategoryCount(); i++){
            GameObject wire = edgeWires.GetCategory(i);
            
            if(wire.name != "Edge7mm"){  //7mm wire doesn't reach the middle holes from the outside power rail
                outsideRailList.Add(wire);
            }
            if(wire.name != "Edge17mm"){ //17mm wire doesn't leave room on the middle vertical bus 
                insideRailList.Add(wire);
            }
        }
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
        
        placeRandomEdgeWire();
        
        var middleWire = middleCache.GetOrInstantiate(middleWires.Sample());
        int x = random.Next(63);
        int y = random.Next(10);
        placeMiddleWire(middleWire, x, y);
    } 

    /// <summary>
    /// Deletes generated foreground objects after each scenario iteration is complete
    /// </summary>
    protected override void OnIterationEnd()
    {
     breadboardCache.ResetAllObjects();
     edgeCache.ResetAllObjects();
     middleCache.ResetAllObjects();
    }
}