using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnatomyName : MonoBehaviour
{
    public void NameChanged (string AnaName)
    {
        var textMPro = gameObject.GetComponent<TMPro.TMP_Text>();
        textMPro.text = AnaName;
    }
}
