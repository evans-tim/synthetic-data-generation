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

    // float railRightX = 18.4f; //furthest right rail position x value

    // float insideBottomRailY =  -5.08f;  // inside rail on the bottom y value
    float outsideBottomRailY = -5.74f;  // outside rail on the bottom y value
    // float insideTopRailY =  5.08f;  // inside rail on the bottom y value
    float outsideTopRailY = 5.70f;  // outside rail on the bottom y value
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

    Dictionary<string, int> middleWireLengthDict =  
              new Dictionary<string, int>(){
                                  {"2mm63", 2},
                                  {"5mm63", 3},
                                  {"7mm63", 4},
                                  {"resistor63v4", 5},
                                  {"12mm63", 6},
                                  {"15mm63", 7},
                                  {"17mm63", 8},
                                  {"20mm63", 9},
                                  {"22mm63", 10},
                                   };

    int[] lengthWeights = new int[35] {
        2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10,
    };

    protected bool isAlignedWithRail(int xPos){
        //7, 13, 19, 25
        return (xPos - 1) % 6 != 0 && xPos > 1 && xPos < 61;
    }

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
                int newYPos = yPos - 2 + length - 1 ; //there is a two space gap in the middle of the breadboard and the length is the number of holes the wire spans - 1 since a wire length of n starting at position x would terminate at position x + n - 1
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
		}
            
    }
    
    


    // returns ending x and y. if it overlapped, returns the original x and y
    protected static int[] visitHoles(int[,] visited, int startingX, int startingY, int wireLength, int direction){

        int endingX = startingX;
        int endingY = startingY;

        int[,] attemptVisit = new int[63, 10];

        Array.Copy(visited, attemptVisit, 630);

        bool wireOverlaps = false;

		if(direction == 0) { // South
			if(startingY - wireLength <= 4){ //offset for the center line of breadboard where there are no holes 
                wireLength = wireLength - 2;
            }
            for(int i = 0; i < wireLength; i++){
                if(attemptVisit[startingX, startingY - i] == 1){
                    wireOverlaps = true;
                }
				attemptVisit[startingX, startingY - i] = 1;
			}
            endingY = startingY - wireLength - 1;
		}
		else if(direction == 1){ //East
            for(int i = 0; i < wireLength; i++){
                if(attemptVisit[startingX + i, startingY] == 1){
                    wireOverlaps = true;
                }
				attemptVisit[startingX + i, startingY] = 1;
			}
            endingX = startingX + wireLength - 1;
		}
		else if(direction == 2){ //North
            if(startingY + wireLength >= 7){ //offset for the center line of breadboard where there are no holes 
                wireLength = wireLength - 2;
            }
            for(int i = 0; i < wireLength; i++){
                if(attemptVisit[startingX, startingY + i] == 1){
                    wireOverlaps = true;
                }
				attemptVisit[startingX, startingY + i] = 1;
			}
            endingY = startingY + wireLength - 1;

		}
		else if(direction == 3){ //West
            for(int i = 0; i < wireLength; i++){
                if(attemptVisit[startingX - i, startingY] == 1){
                    wireOverlaps = true;
                }
				attemptVisit[startingX - i, startingY] = 1;
			}
            endingX = startingX - wireLength + 1;
		}
		else{
			throw new Exception("Invalid direction was chosen");
		}

        int[] endingPlacement = new int[2];
        if(!wireOverlaps){
            // Debug.Log("WIRE DOESN'T OVERLAP");
            Array.Copy(attemptVisit, visited , 630);
            endingPlacement[0] = endingX;
            endingPlacement[1] = endingY;
        }
        else{
            // Debug.Log("WIRE OVERLAPS");
            endingPlacement[0] = startingX;
            endingPlacement[1] = startingY;
        }
        
    //    printVisited(visited);
        
		return endingPlacement;
	}

    protected static void printVisited(int[,] visited){
        string visitedStr = "";
		for(int i = visited.GetLength(1) - 1; i >= 0 ; i--){
			for(int j = 0; j < visited.GetLength(0); j++){
				//Debug.Log(visited[j,i]);
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

    protected void placeTopEdgeWire(int x, int wireLength){

        bool found = false;
        for(int i = 0; i < outsideRailList.Count; i++){
            if(outsideRailLengthDict[outsideRailList[i].name] == wireLength){
                var bottomEdgeWire = edgeCache.GetOrInstantiate(outsideRailList[i]);
                bottomEdgeWire.transform.position = new Vector3(middleBottomLeftX +  ((x)*verticalHoleDistance), outsideTopRailY , wireZ);
                bottomEdgeWire.transform.rotation = Quaternion.Euler(0, 0, 180);
                found = true;
            }
        }
        if(!found){
            throw new Exception("Edge wire with length " + wireLength.ToString() + " was not found");
        }
    }

    protected void placeBottomEdgeWire(int x, int wireLength){

        bool found = false;
        for(int i = 0; i < outsideRailList.Count; i++){
            if(outsideRailLengthDict[outsideRailList[i].name] == wireLength){
                var bottomEdgeWire = edgeCache.GetOrInstantiate(outsideRailList[i]);
                bottomEdgeWire.transform.position = new Vector3(middleBottomLeftX +  ((x)*verticalHoleDistance), outsideBottomRailY , wireZ);
                bottomEdgeWire.transform.rotation = Quaternion.Euler(0, 0, 0);
                found = true;
            }
        }
        if(!found){
            throw new Exception("Edge wire with length " + wireLength.ToString() + " was not found");
        }
    }

    protected void placeCenterWire(int x, int y, int wireLength, int direction){

        bool found = false;
        // Add the wires to the lists representing the wires allowed for certain positions
        for (var i = 0; i < middleWires.GetCategoryCount(); i++){
            GameObject wire = middleWires.GetCategory(i);
            
            if(middleWireLengthDict[wire.name] == wireLength){
                var middleWire = middleCache.GetOrInstantiate(wire);
                if(y > 4){ //offset for the center line of breadboard where there are no holes 
                    y = y + 2;
                }

                middleWire.transform.position = new Vector3(middleBottomLeftX + (horizontalHoleDistance*x), middleBottomLeftY + (verticalHoleDistance*y), wireZ);
                middleWire.transform.rotation = Quaternion.Euler(0, 0, 90*direction);  // ensure rotated properly
                found = true;
            }
        }
        if(!found){
            throw new Exception("Middle wire with length " + wireLength.ToString() + " was not found");
        }
    }

    /// <summary>
    /// Method <c>placeCircuit()</c> 
    /// places circuit components based on array created by findCircuit
    /// </summary>
    /// <returns> void </returns>
    protected void placeCircuit(List<int[]> placements){
        // for(int i = 0; i < placements.Count; i++){
        //    Debug.Log("x: " + placements[i][0].ToString() + ", y: " + placements[i][1].ToString() + ", length: " + placements[i][2].ToString() + ", direction: " + placements[i][3].ToString()); 
        // }

        //place first edge wire

        int[] edgeWire = placements[0];

        placeBottomEdgeWire(edgeWire[0], edgeWire[2]);


        //place middle wires
        for(int i = 1; i < placements.Count - 1; i++){
            placeCenterWire(placements[i][0], placements[i][1], placements[i][2], placements[i][3]);
        }

        //place top wire
        int index = placements.Count - 1;
        placeTopEdgeWire(placements[index][0],placements[index][2]);
        
    }

    /// <summary>
    /// Method <c>findCircuit()</c> 
    /// generates the wire placements for a connected circuit
    /// </summary>
    /// <returns> int[,] an array of arrays in the format [x, y, length, direction] </returns>
    protected List<int[]> findCircuit(){
        Debug.Log("Finding Circuit");
        int[,] visited = new int[63, 10];
        int[,] cleared = new int[63, 10];
        // int[,] circuit = new int[1,4];
        List<int[]> circuit = new List<int[]>();

        // random edge wire.
        int holeNumber = random.Next(50);
        int edgeX = ((holeNumber/5)+holeNumber) + 2;
        int edgeY = 0;
        int edgeDirection = 2; //North
        int edgeWireLength = random.Next(4) + 1; //fill up 1-4 of holes
        int[] edgeWire = new int[4] {edgeX, edgeY, edgeWireLength, edgeDirection};
        



        int attempts = 0;
        bool foundPath = false;

        while(!foundPath){
            // initialize
            circuit.Clear();
            circuit.Add(edgeWire);
            Array.Copy(cleared, visited, 630);
            visitHoles(visited, edgeX, edgeY, edgeWireLength, edgeDirection);

            //begin search
            int x = edgeX;
            int y = 4 - random.Next(5 - edgeWireLength);
            int wireLength = lengthWeights[random.Next(35)];
            int direction = 1 + 2 * random.Next(2);


            int[] placement = new int[2];
            placement[0] = x;
            placement[1] = y;

            
            for(int i = 0; i < 14; i++){
                // get length and direction thats inside boundary
                bool overlaps = true;
                attempts = 0;
                Debug.Log("Ending Placement: " + placement[0].ToString() + ", " + placement[1].ToString());
    

                //final wire
                if(placement[1] > 4 && direction == 2 && isAlignedWithRail(placement[0])){ // wire going north in top half should end up in top rail
                    int randLength = 1 + random.Next(8 - placement[1]);
                    int originalX = placement[0];
                    int[] finalWire = new int[4] {originalX, 9, randLength, 2};
                    placement = visitHoles(visited, originalX, 9, randLength, 0);

                    if(!(placement[0] == originalX && placement[1] == 9)){  //new placement was found, place the wire
                        overlaps = false;  
                        circuit.Add(finalWire);
                        Debug.Log("Placing final wire");
                        foundPath = true;
                        break;
                    }
                    else{
                        overlaps = true;
                    }
                    
                }

                while(overlaps && attempts < 5){
                    Debug.Log("Attempt " + attempts.ToString());
                    while(!isInsideBoundary(x, y, wireLength, direction)){
                        direction =  1 + random.Next(3); 
                        wireLength = lengthWeights[random.Next(35)];
                        x = placement[0];
                        if(placement[1] < 5){
                            while(y == placement[1]){
                                y = random.Next(5);
                            }
                        }
                        else{
                            while(y == placement[1]){
                                y = 5 + random.Next(4);
                            }
                        }
                    }
                    Debug.Log("Attempting x: " + x.ToString() + ", y: " + y.ToString() + ", length: " + wireLength.ToString() + ", direction: " + direction.ToString() + "Termination: " +placement[0].ToString() + ", " + placement[1].ToString()); 

                    // if it overlaps try again
                    // check to see if placement overlaps
                    placement = visitHoles(visited, x, y, wireLength, direction);
                    attempts = attempts + 1;

                    if(!(placement[0] == x && placement[1] == y)){  //new placement was found, place the wire
                        overlaps = false;  

                    }
                    else{
                        direction =  1 + random.Next(3); 
                        wireLength = lengthWeights[random.Next(35)];
                        x = placement[0];
                        if(placement[1] < 5){
                            y = random.Next(5);
                            while(y == placement[1]){
                                y = random.Next(5);
                            }
                        }
                        else{
                            y = 5 + random.Next(4);
                            while(y == placement[1]){
                                y = 5 + random.Next(4);
                            }
                        }
                    }
                }
                
                if(!overlaps){
                    int[] middleWire = new int[4] {x, y, wireLength, direction};
                    Debug.Log("Placing x: " + x.ToString() + ", y: " + y.ToString() + ", length: " + wireLength.ToString() + ", direction: " + direction.ToString() + "Termination: " +placement[0].ToString() + ", " + placement[1].ToString()); 

                    circuit.Add(middleWire);
                }
                

                // new random wire placement from last termination point
                x = placement[0];
                if(placement[1] < 5){
                    y = random.Next(5);
                    while(y == placement[1]){
                        y = random.Next(5);
                    }
                }
                else{
                    y = 5 + random.Next(4);
                    while(y == placement[1]){
                        y = 5 + random.Next(4);
                    }
                }
                direction =  1 + random.Next(3); 
                wireLength = 2 + random.Next(9);

            }
        }
        

        

       
        Debug.Log("FOUND PATH?");
        Debug.Log(foundPath);

        printVisited(visited);
        return circuit;
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
        List<int[]> circuit = findCircuit();
        placeCircuit(circuit);
        
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