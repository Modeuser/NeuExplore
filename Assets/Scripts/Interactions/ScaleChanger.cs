using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleChanger : MonoBehaviour
{
    public void ScaleSlider (float sliderValue)
    {
        gameObject.transform.localScale = new Vector3(sliderValue, sliderValue, sliderValue);
    }
}
