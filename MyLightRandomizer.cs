using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;


/// <summary>
/// Randomizes the rotation of objects tagged with a LightRandomizerTag
/// </summary>
[Serializable]
[AddRandomizerMenu("Perception/My Light Randomizer")]
public class MyLightRandomizer : Randomizer
{
    public FloatParameter lightIntensityParameter;

    /// <summary>
    /// The range of random rotations to assign to target objects
    /// </summary>
    [Tooltip("The range of random rotations to assign to target objects.")]
    public Vector3Parameter rotation = new Vector3Parameter
    {
        x = new UniformSampler(0, 360),
        y = new UniformSampler(0, 360),
        z = new UniformSampler(0, 360)
    };

    protected override void OnIterationStart()
    {
        var tags = tagManager.Query<MyLightRandomizerTag>();

        foreach (var tag in tags)
        {
            var light = tag.GetComponent<Light>();
            light.intensity = lightIntensityParameter.Sample();
            light.transform.rotation = Quaternion.Euler(rotation.Sample());  //x,y: -80:80
        }
    }
}