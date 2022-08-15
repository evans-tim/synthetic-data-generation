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

    /// <inheritdoc/> 
    protected override void OnAwake()
    {
        
    }

    /// <summary>
    /// Generates a foreground layer of wire and resistor objects at the start of each scenario iteration
    /// The objects form a connected circuit on the breadboard 
    /// </summary>
    protected override void OnIterationStart()
    { 
        
    }

    /// <summary>
    /// Deletes generated foreground objects after each scenario iteration is complete
    /// </summary>
    protected override void OnIterationEnd()
    {
     
    }
}