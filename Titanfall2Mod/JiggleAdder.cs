using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleAdder : MonoBehaviour
{
    private readonly List<DynamicBone> _jiggleBones = new List<DynamicBone>();

    private float _stiffness;

    public float Stiffness
    {
        get => _stiffness;
        set
        {
            _stiffness = value;
            foreach (var dynamicBone in _jiggleBones) dynamicBone.m_Stiffness = _stiffness;
        }
    }

    private Vector3 _gravity;
    public Vector3 Gravity
    {
        get => _gravity;
        set
        {
            _gravity = value;
            foreach (var dynamicBone in _jiggleBones) dynamicBone.m_Gravity = _gravity;
        }
    }
    void Start()
    {
        RecursiveFind(transform);
        Stiffness = 0.01f;
        Gravity = Vector3.down*100;
    }

    private void RecursiveFind(Transform transform1)
    {
        for (var i = 0; i < transform1.childCount; i++)
        {
            var child = transform1.GetChild(i);
            if (child.name.Contains("jiggle")) _jiggleBones.Add(child.gameObject.AddComponent<DynamicBone>());
            RecursiveFind(child);
        }
    }
}
