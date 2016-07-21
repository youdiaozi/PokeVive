using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MeshReductor : MonoBehaviour
{
    public float _shrinkTotalDuration = 4f;
    public float _shrinkingSpeed = 2f;

    private Transform _tr;
    private float _shrinkRemainingDuration = 0f;
    private MeshFilter _filter;
    private float _sphereRadius;
	
	void Start()
	{
        _tr = this.transform;
        _filter = this.GetComponent<MeshFilter>();
        _filter.mesh.MarkDynamic();
        SetMeshCenter();

        _sphereRadius = _filter.mesh.bounds.extents.magnitude / 2f;
    }
	
	void Update()
	{
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartShrinking();
        }

        if (_shrinkRemainingDuration > 0f)
        {
            ShrinkSphere();
        }
	}

    void StartShrinking()
    {
        _shrinkRemainingDuration = _shrinkTotalDuration;
    }

    void SetMeshCenter()
    {
        List<Vector3> vertices = new List<Vector3>(_filter.mesh.vertices);
        Vector3 center = _filter.mesh.bounds.center;

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] -= center;
        }

        _filter.mesh.SetVertices(vertices);
        _tr.position += center;
    }

    void Shrink()
    {
        List<Vector3> vertices = new List<Vector3>(_filter.mesh.vertices);

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] *= 1f - Time.deltaTime * _shrinkingSpeed;
        }

        _filter.mesh.SetVertices(vertices);
    }

    void ShrinkSphere()
    {
        List<Vector3> vertices = new List<Vector3>(_filter.mesh.vertices);

        for (int i = 0; i < vertices.Count; i++)
        {
            float length = (vertices[i] - 2f * Vector3.forward).magnitude;
            float ratio = _sphereRadius / vertices[i].magnitude;

            if (false)
            { 
                vertices[i] = Vector3.Lerp(vertices[i], vertices[i] * _sphereRadius / vertices[i].magnitude, _shrinkingSpeed * Time.deltaTime);
            }
            else
            {
                if (length == 0f)
                {
                    continue;
                }

                vertices[i] = Vector3.Lerp(vertices[i], -2f * Vector3.forward, Time.deltaTime * (length * length) / (_shrinkingSpeed));
            }
        }

        _filter.mesh.SetVertices(vertices);
    }
}