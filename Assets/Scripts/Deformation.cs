using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deformation : MonoBehaviour
{
    public float _maxDeformationRatio = 1f;
    public float _decaySpeed = 4f;
    public Animator _animator;

    private Transform _tr;

    private List<Transform> _childrenList = new List<Transform>();
    private List<Vector3> _childrenOriginalLossyScaleList = new List<Vector3>();
    private List<Vector3> _childrenOriginalLocalScaleList = new List<Vector3>();

    private float _currentDeformationRatio;

    void Start()
	{
        _tr = this.transform;

        _childrenList.Clear();
        GetRecursiveChildrenList(_tr);
    }
	
	void Update()
	{
        if (_currentDeformationRatio > 0f)
        {
            _currentDeformationRatio -= _decaySpeed * Time.deltaTime;

            if (_currentDeformationRatio < 0f)
            {
                _currentDeformationRatio = 0f;
            }

            ApplyDeformation();

            if (_currentDeformationRatio == 0f)
            {
                EndDeformation();
            }
        }
	}

    void GetRecursiveChildrenList(Transform tr)
    {
        for (int i = 0; i < tr.childCount; i++)
        {
            Transform child = tr.GetChild(i);

            _childrenList.Add(child);
            _childrenOriginalLossyScaleList.Add(child.lossyScale);
            _childrenOriginalLocalScaleList.Add(child.localScale);

            if (child.childCount > 0)
            {
                GetRecursiveChildrenList(child);
            }
        }
    }

    public void LaunchDeformation()
    {
        if (_animator != null)
        {
            _animator.enabled = false;
        }

        _currentDeformationRatio = _maxDeformationRatio;
    }

    void ApplyDeformation()
    {
        // The order we picked the children was made from top to bottom of the tree so we can change the scales safely and the lossy scale are updated in the right order.

        for (int i = 0; i < _childrenList.Count; i++)
        {
            Transform child = _childrenList[i];

            Vector3 los = child.lossyScale;
            Vector3 loc = child.localScale;

            // We avoid dividing by zero.
            if (los.x == 0f || los.y == 0f || los.z == 0f)
            {
                continue;
            }

            Vector3 newLos = new Vector3();
            newLos.x = _childrenOriginalLossyScaleList[i].x * (1f + Random.Range(_currentDeformationRatio * -0.3f, _currentDeformationRatio));
            newLos.y = _childrenOriginalLossyScaleList[i].y * (1f + Random.Range(_currentDeformationRatio * -0.3f, _currentDeformationRatio));
            newLos.z = _childrenOriginalLossyScaleList[i].z * (1f + Random.Range(_currentDeformationRatio * -0.3f, _currentDeformationRatio));

            child.transform.localScale = new Vector3(loc.x * newLos.x / los.x, loc.y * newLos.y / los.y, loc.z * newLos.z / los.z);
        }
    }

    void EndDeformation()
    {
        for (int i = 0; i < _childrenList.Count; i++)
        {
            _childrenList[i].transform.localScale = _childrenOriginalLocalScaleList[i];
        }

        if (_animator != null)
        {
            _animator.enabled = true;
        }
    }

    public bool DeformationHasEnded()
    {
        return (_currentDeformationRatio == 0f);
    }
}