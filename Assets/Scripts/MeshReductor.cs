using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public delegate void ReductionEventHandler();

public class MeshReductor : MonoBehaviour
{
    public float _shrinkTotalDuration = 6f;
    public float _shrinkingSpeed = 0.1f;
    public event ReductionEventHandler ReductionDone;

    private Transform _tr;
    private float _shrinkTimeCount = 0f;
    private MeshFilter _filter;
    private float _sphereRadius;
    private List<Vector3> _originalVertices;
    private List<Vector3> _vertices;
    private bool _isShrinking = false;
    private Transform _attractor; // In local space.
    private Bounds _originalBounds;
    private float _highestVertex = 0f;

    void Start()
	{
        _tr = this.transform;
        _filter = this.GetComponent<MeshFilter>();
        _filter.mesh.MarkDynamic();
        //SetMeshCenter();

        _sphereRadius = _filter.mesh.bounds.extents.magnitude;
    }
	
	void Update()
	{
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartShrinking();
        }

        if (_isShrinking)
        {
            bool stopShrinking = false;
            _shrinkTimeCount += Time.deltaTime;
            if (_shrinkTimeCount < _shrinkTotalDuration)
            {
                float farthestPointToDestination = ShrinkSphere();

                if (farthestPointToDestination < 0.05f)
                {
                    stopShrinking = true;
                }
            }

            if (_shrinkTimeCount >= _shrinkTotalDuration || stopShrinking)
            {
                _isShrinking = false;

                if (ReductionDone != null)
                {
                    ReductionDone.Invoke();
                }
            }
        }

	}

    public void StartShrinking(Transform attractor)
    {
        _attractor = attractor;
        StartShrinking();
    }

    public void StartShrinking()
    {
        /// What I want is:
        /// - a fixed duration
        /// - an attraction point that can be something else than the center of the mesh
        /// - a non-uniform attraction force (the closer to the attraction point, the faster it gets attracted).

        _isShrinking = true;
        _vertices = new List<Vector3>(_filter.mesh.vertices);
        _originalVertices = new List<Vector3>(_filter.mesh.vertices);
        _originalBounds = _filter.mesh.bounds;
        _shrinkTimeCount = 0f;

        List<Vector3> vertices = new List<Vector3>(_vertices);

        for (int i = 0; i < _vertices.Count; i++)
        {
            _highestVertex = Mathf.Max(_vertices[i].y, _highestVertex);
        }
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

    float ShrinkSphere()
    {
        float highestOriginToDestRatio = 0f;

        List<Vector3> vertices = new List<Vector3>(_vertices);
        List<Vector3> normals = new List<Vector3>(_filter.mesh.normals);

        for (int i = 0; i < _vertices.Count; i++)
        {
            Vector3 attractionPoint = Vector3.zero;
            if (_attractor != null)
            {
                attractionPoint = _tr.InverseTransformPoint(_attractor.position);
            }
            else
            {
                attractionPoint = _tr.InverseTransformPoint(Vector3.up * 2.5f);
            }

            float length = (_vertices[i] - attractionPoint).magnitude;
            float originToDestRatio = length / (attractionPoint - _originalVertices[i]).magnitude;
            float distanceRatio = _vertices[i].magnitude / _sphereRadius;
            float vertexHeightRatio = _vertices[i].y / _highestVertex;
            //distanceRatio = 1f - distanceRatio;
            float timeRatio = _shrinkTimeCount / _shrinkTotalDuration;
            float ratio = distanceRatio + timeRatio;

            if (false)
            {
                vertices[i] = Vector3.Lerp(vertices[i], vertices[i] * 0.5f * _sphereRadius / vertices[i].magnitude, _shrinkingSpeed * Time.deltaTime);
            }
            else if (false)
            {
                if (length == 0f)
                {
                    continue;
                }

                _vertices[i] = Vector3.Lerp(_vertices[i], attractionPoint, Time.deltaTime * (length * length) / _shrinkingSpeed);
                vertices[i] = _vertices[i];
            }
            else if (true)
            {
                if (length == 0f)
                {
                    continue;
                }

                float range = 0.2f;
                float acceleration = 2f;
                float finalRatio = Mathf.Pow(timeRatio, acceleration) + Mathf.Pow(timeRatio, 1f / acceleration) * Mathf.Pow(distanceRatio, acceleration) / 2f;

                finalRatio = Mathf.Clamp01(finalRatio);

                range *= Mathf.Clamp01(originToDestRatio);
                //vertices[i] += normals[i] * UnityEngine.Random.Range(-_sphereRadius * range, _sphereRadius * range);
                _vertices[i] = Vector3.Lerp(vertices[i], attractionPoint, finalRatio);
                vertices[i] = _vertices[i];
            }
            else
            {
                // Random noise. Looks great but makes holes. Try using a shader that displays back faces.

                float range = 0.4f;
                //vertices[i] += UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(-_sphereRadius * range, _sphereRadius * range);
                vertices[i] += normals[i] * UnityEngine.Random.Range(-_sphereRadius * range, _sphereRadius * range);
            }

            highestOriginToDestRatio = Mathf.Max(highestOriginToDestRatio, originToDestRatio);
        }

        _filter.mesh.SetVertices(vertices);

        return highestOriginToDestRatio;
    }
}