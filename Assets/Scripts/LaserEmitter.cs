using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaserEmitter : MonoBehaviour
{
    public Renderer _rend;
    public float _speed = 10f;

    private Transform _tr;
    private Rigidbody _rigid;
    private Pokeball _pokeball;
    private Collider _coll;
    private bool _isEmitting = false;
    private float _length = 0f;
    private bool _touchesOwnedPokemon = false;

    void Start()
    {
        _tr = this.transform;
        _rigid = this.GetComponent<Rigidbody>();
        _coll = this.GetComponent<Collider>();
        Stop();
    }

    void Update()
    {
        if (!_rend.enabled)
        {
            return;
        }

        _length += _speed * Time.deltaTime;

        // Stopping the laser on colliders.
        Ray ray = new Ray(_pokeball.transform.position, _tr.up);
        RaycastHit hitInfo = new RaycastHit();
        if (Physics.Raycast(ray, out hitInfo, 2f * _length))
        {
            _length = (hitInfo.point - ray.origin).magnitude / 2f;
        }

        _tr.localPosition = new Vector3(0, 0, _length);

        // If the laser is active, we make it grow and also check if it's touching the Pokémon.
        if (_isEmitting)
        {
            _tr.localScale = new Vector3(0.01f, _length, 0.01f);

            _pokeball._content.UpdateMeshCollider();

            //if (DoesLaserCrossesMesh())
            //{
            //    if (!_touchesOwnedPokemon)
            //    {
            //        _pokeball.ConvertToRedEnergy(true);
            //        _touchesOwnedPokemon = true;
            //    }
            //}
            //else if (_touchesOwnedPokemon)
            //{
            //    _pokeball.ConvertToRedEnergy(false);
            //    _touchesOwnedPokemon = false;
            //}
        }

        // When the laser is too far away, we hide it.
        float closestDistanceFromPlayer = (_tr.position - Camera.main.transform.position).magnitude - _tr.localScale.y;
        if (!_isEmitting && (closestDistanceFromPlayer > 100f) || _length <= 0f)
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
        _length = 0f;
        _rend.enabled = true;
    }

    public void Stop()
    {
        _isEmitting = false;

        _rend.enabled = false;
        _touchesOwnedPokemon = false;
        //_tr.SetParent(null);
        //_rigid.AddRelativeForce(new Vector3(0, _speed, 0), ForceMode.Acceleration);
    }
    
    public void OnTriggerStay(Collider other)
    {
        if (!_isEmitting || other.name != "SkinnedMeshCollider")
        {
            return;
        }

        if (!_touchesOwnedPokemon && _pokeball.IsMyPokemon(other.transform.parent))
        {
            _pokeball.ConvertToRedEnergy(true);
            _touchesOwnedPokemon = true;
            //Stop();
        }
        else
        {
            // Obstacles block the laser expansion.
            // Note: the following line won't work, you stupid.
            //_length = (_tr.position - other.ClosestPointOnBounds(_tr.position)).magnitude;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (!_isEmitting || other.name != "SkinnedMeshCollider")
        {
            return;
        }

        if (_pokeball.IsMyPokemon(other.transform))
        {
            _pokeball.ConvertToRedEnergy(false);
            _touchesOwnedPokemon = false;
            //Stop();
        }
        else
        {
            // Obstacles block the laser expansion.
            // Note: the following line won't work, you stupid.
            //_length = (_tr.position - other.ClosestPointOnBounds(_tr.position)).magnitude;
        }
    }

    List<Vector3> _verticesToDraw = new List<Vector3>();

    public bool DoesLaserCrossesMesh()
    {
        if (!_isEmitting)
        {
            return false;
        }

        Vector3 capsuleNormal = _tr.up;
        Vector3 capsuleOrigin = _tr.position - capsuleNormal * _length;
        float capsuleRadius = 0.01f;

        Mesh bakedMesh = new Mesh();
        _pokeball._content._skinnedMesh.BakeMesh(bakedMesh);




        Vector3[] vertex = new Vector3[3];
        Vector3[] vertexWorldPosition = new Vector3[3];
        Vector3 planeCenter, planeNormal, projection, projectionOnPlane, directionToPlaneCenter, finalPoint, lastCross, cross;
        int[] triangles = bakedMesh.triangles;
        Vector3[] vertices = bakedMesh.vertices;
        Ray ray = new Ray(capsuleOrigin, capsuleNormal);
        float distance;
        Plane plane = new Plane();

        //while(triangleId < triangles.Length - 3)
        {
            _verticesToDraw.Clear();
            // Checking a single triangle.
            for (int i = 0; i < triangles.Length - 3; i += 3)
            {
                /// Steps to check the collision between an length capsule (which is also a cylinder) and a triangle:
                /// 1) calculate the projection of the point on the triangle plane.
                /// 2) bring the point on the plane closer to the center of the triangle by the smallest value between the radius of the capsule and the distance between the projected point and the center of the triangle.
                /// 3) use the final point to check the cross product between one edge of the triangle and the vector between the first point of the edge and the final point. If all the dot products between the 3 cross products are of the same sign, then the final point lies within the triangle.
                /// 4) you now have confirmed the collision between the triangle and the infinite length capsule!

                vertex[0] = vertices[triangles[i]];
                vertex[1] = vertices[triangles[i + 1]];
                vertex[2] = vertices[triangles[i + 2]];

                // The coordinates of the vertices we find in the mesh are relative to the mesh pivot. So we make sure all coordinates are in the same referential which is world coordinates.
                // Are the mesh relative coordinates affected by the scale of the Pokemon? Yes, they are. No need to convert them.
                for (int x = 0; x < 3; x++)
                {
                    vertexWorldPosition[x] = _pokeball._content.transform.position + _pokeball._content.transform.rotation * vertex[x];
                }

                plane.Set3Points(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
                plane.Raycast(ray, out distance);

                /*planeCenter = (vertexWorldPosition[0] + vertexWorldPosition[1] + vertexWorldPosition[2]) / 3f;
                planeNormal = Vector3.Cross((vertexWorldPosition[1] - vertexWorldPosition[0]), (vertexWorldPosition[2] - vertexWorldPosition[1])).normalized;

                // Finding the orthogonal projection of the capsule onto the plane.
                projection = Vector3.Project((planeCenter - capsuleOrigin), planeNormal);
                */
                // If the projection and the capsule normal go the opposite way, that means the plane is behind the ray so we stop the calculations for this triangle.
                //if (Vector3.Dot(projection, capsuleNormal) < 0)
                if (distance < 0)
                {
                    continue;
                }

                // We calculate the projection of the capsule onto the plane.
                //projectionOnPlane = capsuleOrigin + projection;
                projectionOnPlane = capsuleOrigin + capsuleNormal * distance;

                _verticesToDraw.Add(projectionOnPlane);

                // Actually when I said the capsule had an infinite length, I lied. We can give it a length thanks to the following condition:
                //if ((projectionOnPlane - capsuleOrigin).magnitude > _length)
                //{
                //    continue;
                //}

                // We now move the point towards the center of the triangle in order to take into account the radius of the capsule.
                //directionToPlaneCenter = planeCenter - projectionOnPlane;
                //finalPoint = projectionOnPlane + directionToPlaneCenter * Mathf.Min(capsuleRadius, directionToPlaneCenter.magnitude);
                finalPoint = projectionOnPlane;

                // Now we check if the final point is within the triangle by comparing some cross products.
                lastCross = Vector3.zero;
                bool finalPointIsInside = true;
                for (int j = 0; j < 3; j++)
                {
                    cross = Vector3.Cross((vertexWorldPosition[(j + 1) % 3] - vertexWorldPosition[j]), (vertexWorldPosition[(j + 2) % 3] - vertexWorldPosition[(j + 1) % 3]));
                    lastCross = Vector3.Cross((vertexWorldPosition[(j + 1) % 3] - vertexWorldPosition[j]), (finalPoint - vertexWorldPosition[j]));  // A virer.

                    if (Vector3.Dot(lastCross, cross) <= 0)
                    {
                        finalPointIsInside = false;
                        break;
                    }

                    //if (j > 0 && Vector3.Dot(lastCross, cross) < 0)
                    //{
                    //    finalPointIsInside = false;
                    //    break;
                    //}

                    //lastCross = cross;
                }

                // If the final point is outside, we proceed to the next triangle.
                if (finalPointIsInside)
                {
                    Debug.Log("is inside !!!!!!!!!!");
                    return true;
                }
            }
        }

        return false;
    }

    public void OnDrawGizmos()
    {
        /*if (_verticesToDraw == null)
            return;*/

        Gizmos.color = Color.blue;
        foreach (Vector3 vert in _verticesToDraw)
        {
            Gizmos.DrawSphere(vert, 0.02f);
        }
    }
}