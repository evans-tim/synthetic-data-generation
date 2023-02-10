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
    float insideTopRailY =  5.08f;  // inside rail on the bottom y value
    float outsideTopRailY = 5.74f;  // outside rail on the bottom y value
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

    Dictionary<string, int> outsideRailLengthDict =  
              new Dictionary<string, int>(){
                                  {"Edge10mm", 1},
                                  {"Edge12mm", 2},
                                  {"Edge15mm", 3},
                                  {"Edge17mm", 4}, };

    Dictionary<string, int> middleWireLengthDict =  // the length is the number of holes the wire spans - 1 since a wire length of n starting at position x would terminate at position x + n - 1
              new Dictionary<string, int>(){
                                  {"2mm63", 1},
                                  {"5mm63", 2},
                                  {"7mm63", 3},
                                  {"10mm63", 4},
                                  {"12mm63", 5},
                                  {"15mm63", 6},
                                  {"17mm63", 7},
                                  {"20mm63", 8},
                                  {"22mm63", 9},
                                   };

    /// <summary>
    /// Method <c>checkBoundary()</c> 
    /// Checks to see if a wire will stay on breadboard or cross the middle gap correctly.
    /// </summary>
    protected bool isInsideBoundary(int xPos, int yPos, int length, int direction){
        if (direction == 0) { // South
           return false;
        }
        else if(direction == 1){ // East
            if(xPos + length > 62){
                return false;
            }
            else{
                return true;
            }
            
        }
        else if(direction == 2){ //North
            if(yPos >= 0 && yPos <= 4){
                int newYPos = yPos + length - 2 ; //there is a two space gap in the middle of the breadboard and
                bool reachesNextSection = (newYPos >=5 && newYPos <= 8); // needs to have at least 1 leftover space for another wire
                if(reachesNextSection){
                    return true;
                }
                else{
                    return false;
                }

            }
            else{
                return false;
            }
		}
		else if(direction == 3){ //West
            if(xPos - length < 0){
                return false;
            }
            else{
                return true;
            }
		}
		else{
			throw new Exception("Invalid direction was chosen");
            return false;
		}
            
    }

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

        int wireLength = middleWireLengthDict[wire.name.Replace("(Clone)", "")];
        int direction =  1 + random.Next(3); 

        Debug.Log("X: " + x.ToString());
        Debug.Log("Y: " + y.ToString());
        Debug.Log("Direction: " + direction.ToString());
        Debug.Log("Wire Length: " + wireLength.ToString());
        while(!isInsideBoundary(x, y, wireLength, direction)){
            Debug.Log("Outside");
            direction =  1 + random.Next(3); 
        }

        


        wire.transform.position = new Vector3(middleBottomLeftX + (horizontalHoleDistance*x), middleBottomLeftY + (verticalHoleDistance*y), wireZ);
        wire.transform.rotation = Quaternion.Euler(0, 0, 90*direction);  // ensure rotated properly
    }

    protected int[] placeRandomBottomEdgeWire(int[,] visited){
        int powerRailX = random.Next(50);  // randomly pick which part of the power rail to start the circuit

        int holeNumber = ((powerRailX/5)+powerRailX) + 2; // hole number out of 63 that aligns with the railNumber

        var bottomEdgeWire = edgeCache.GetOrInstantiate(outsideRailList[random.Next(4)]);
        bottomEdgeWire.transform.position = new Vector3(middleBottomLeftX +  ((holeNumber)*verticalHoleDistance), outsideBottomRailY , wireZ);
        bottomEdgeWire.transform.rotation = Quaternion.Euler(0, 0, 0);
       
        int numHolesOccupied = outsideRailLengthDict[bottomEdgeWire.name.Replace("(Clone)", "")];
        visitHoles(visited, holeNumber, 0, numHolesOccupied, 0);

        int[] placement = new int[2];
        placement[0] = holeNumber;
        placement[1] = numHolesOccupied;
		
		return placement;
    }


    protected static int[,] visitHoles(int[,] visited, int startingX, int startingY, int wireLength, int direction){
		visited[startingX,startingY] = 1;
		if(direction == 0) { // South
			
		}
		else if(direction == 1){ //East
            
		}
		else if(direction == 2){ //North
            for(int i = 0; i < wireLength; i++){
				visited[startingX, startingY + i] = 1;
			}
		}
		else if(direction == 3){ //West
		}
		else{
			throw new Exception("Invalid direction was chosen");
		}
		return visited;
	}

    protected static void printVisited(int[,] visited){
        string visitedStr = "";
		for(int i = visited.GetLength(1) - 1; i >= 0 ; i--){
			for(int j = 0; j < visited.GetLength(0); j++){
				// Debug.Log(visited[j,i]);
                int hole = visited[j,i];
                visitedStr += hole.ToString();
			}
            visitedStr += " end ";
			if(i == 5){
				visitedStr += " middle ";
			}
		}
        Debug.Log(visitedStr);
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
        int[,] visited = new int[63,10];
        Debug.Log(breadboard.GetCategoryCount());
        //place and scale breadboard 
        var breadboardInstance = breadboardCache.GetOrInstantiate(breadboard.Sample()); 
        breadboardInstance.transform.position = new Vector3(0, 0, 0);
        breadboardInstance.transform.rotation = Quaternion.Euler(0, 90, -90);
        breadboardInstance.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        
        int[] firstPlacement = placeRandomBottomEdgeWire(visited);
        
		printVisited(visited);

        int currentPosition = firstPlacement[0];
        int holesTaken = firstPlacement[1];
        int nextY = 4 - random.Next(5 - holesTaken);
        
        
        var middleWire = middleCache.GetOrInstantiate(middleWires.Sample());
        int x = random.Next(63);
        int y = random.Next(10);
        placeMiddleWire(middleWire, currentPosition, nextY);
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