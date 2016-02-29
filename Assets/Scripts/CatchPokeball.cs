using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;

public class CatchPokeball : MonoBehaviour
{
    private const float _pokeballCatchRadius = 0.1f; // The real pokeball radius is 0.05.

    private Transform _tr;
    private Rigidbody _rigid;
    private List<Vector3> _verticesToDraw = new List<Vector3>(); // A virer.

    void Start()
    {
        _tr = this.transform;
        _rigid = GetComponent<Rigidbody>();

        if (_rigid == null)
        {
            Debug.LogError("No rigidbody attached to the pokeball");
            DestroyImmediate(this);
        }
    }

    void Update()
    {
        foreach (NVRHand hand in NVRPlayer.Instance.Hands)
        {
            if (hand.UseButtonPressed)
            {
                Recall(hand.transform.position);
            }
        }

        CheckCapture();
    }

    private void Recall(Vector3 pos)
    {
        // Stopping the current movement.
        _rigid.velocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        // New movement.
        var vectorDiff = pos - _tr.position;
        if (vectorDiff.magnitude > 0.1f)
        {
            _rigid.velocity = Vector3.Normalize(vectorDiff) * 5.0f;
        }
    }

    private bool CheckCapture()
    {
        // We check if the pokeball hit a pokemon.
        Vector3 pos = _tr.position;
        foreach (SphereCollider pk in Spawner.WildPokemons)
        {
            // To reduce the amount of calculation, we first check if the pokeball is close enough to the pokemon.
            Transform pkTr = pk.transform;
            float maxScale = Mathf.Max(pk.transform.lossyScale.x, pk.transform.lossyScale.y, pk.transform.lossyScale.z);
            if ((pos - (pkTr.TransformPoint(pk.center))).magnitude < pk.radius * maxScale)
            {
                //Debug.Break();
                SkinnedMeshRenderer[] renderers = pk.GetComponentsInChildren<SkinnedMeshRenderer>();

                // A pokemon is supposed to have one mesh. This script has been built on that hypothesis, so we better check.
                if (renderers.Length > 1)
                {
                    Debug.LogError("More than one skin renderers found on " + pk.name + ":");

                    foreach (Renderer rend in renderers)
                    {
                        Debug.LogError("- " + rend.name);
                    }

                    Debug.Break();
                    DestroyImmediate(this);
                    return false;
                }
                else if (renderers.Length <= 0)
                {
                    Debug.LogError("No skin renderer found for " + pk.name + ":");

                    Debug.Break();
                    DestroyImmediate(this);
                    return false;
                }
                else if (renderers.Length == 1)
                {
                    Mesh mesh = new Mesh();
                    renderers[0].BakeMesh(mesh); // This may take a lot of time and shouldn't be called every frame! Especially at 90 FPS!

                    //Vector3 posBis = pos - pkTr.position; // This represents the position of the pokeball relative to the pokemon space (so we don't have to convert every vertex of the pokemon to world space).
                    _verticesToDraw.Clear();
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        // If a vertex of a pokemon in within the ball radius, then it is captured! Watch out though: a pokemon with wide space between its vertices (like big wings) may be missed by the pokeball.
                        Vector3 vertexWorldPos = pkTr.position + pkTr.rotation * vertex; // pkTr.TransformPoint(vertex); // 
                        _verticesToDraw.Add(vertexWorldPos); // A virer.
                        Vector3 distance = (pos - vertexWorldPos);
                        if (distance.magnitude < _pokeballCatchRadius)
                        {
                            // Gotcha! 
                            Debug.Log(pk.name + " was caught!");
                            pk.gameObject.SendMessage("Captured", this, SendMessageOptions.RequireReceiver);
                            _rigid.velocity = Vector3.zero;
                            _rigid.angularVelocity = Vector3.zero;

                            Spawner.WildPokemons.Remove(pk);

                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public void OnDrawGizmos()
    {
        /*if (_verticesToDraw == null)
            return;*/

        Gizmos.color = Color.yellow;
        foreach (Vector3 vert in _verticesToDraw)
        {
            Gizmos.DrawSphere(vert, 0.02f);
        }
    }
}
