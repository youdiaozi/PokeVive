using UnityEngine;
using System.Collections;

public class LaserEmitter : MonoBehaviour
{
    public Renderer _rend;
    public float _speed = 10f;

    private Transform _tr;
    private Rigidbody _rigid;
    private Pokeball _pokeball;
    private bool _isEmitting = false;
    private float _distance = 0f;
    private Vector3 _breakCoord;

    void Start()
    {
        _tr = this.transform;
        _rigid = this.GetComponent<Rigidbody>();
        Stop();
    }

    void Update()
    {
        if (!_rend.enabled)
        {
            return;
        }

        _distance += _speed * Time.deltaTime;
        _tr.localPosition = new Vector3(0, 0, _distance);

        if (_isEmitting)
        {
            _tr.localScale = new Vector3(0.01f, _distance, 0.01f);
        }

        // When the laser is too far away, we hide it.
        float closestDistanceFromPlayer = (_tr.position - Camera.main.transform.position).magnitude - _tr.localScale.y;
        if (!_isEmitting && (closestDistanceFromPlayer > 100f) || _distance <= 0f)
        {
            _rend.enabled = false;
        }
    }

    public void Play(Pokeball pokeball)
    {
        _isEmitting = true;

        _pokeball = pokeball;

        //_tr.SetParent(parent);
        //_rigid.velocity = Vector3.zero;
        _distance = 0f;
        _rend.enabled = true;
    }

    public void Stop()
    {
        _isEmitting = false;

        _rend.enabled = false;
        //_tr.SetParent(null);
        //_rigid.AddRelativeForce(new Vector3(0, _speed, 0), ForceMode.Acceleration);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!_isEmitting)
        {
            return;
        }

        if (_pokeball.IsMyPokemon(other.transform))
        {
            _pokeball.Recall();
            Stop();
        }
        else
        {
            // Obstacles block the laser expansion.
            // Note: the following line won't work, you stupid.
            //_distance = (_tr.position - other.ClosestPointOnBounds(_tr.position)).magnitude;
        }
    }
}