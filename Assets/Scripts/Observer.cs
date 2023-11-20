using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Observer : MonoBehaviour
{
    public static Action UpdateBoids;
    private void OnDisable()
    {
        UpdateBoids = null;
    }
}
