using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public static List<SphereCollider> WildPokemons = new List<SphereCollider>();

    public int _amount = 5;
    public GameObject[] _picks;
    public GameObject _spawningParticle;
    public GameObject _recallingParticle;

    void Start()
    {
        if (_amount <= 0)
        {
            Debug.Log("Amount of Pokemons to spawn = 0");
            DestroyImmediate(this);
            return;
        }

        if (_picks.Length <= 0)
        {
            Debug.LogError("Number of Pokemons to pick = 0");
            DestroyImmediate(this);
            return;
        }

        WildPokemons = new List<SphereCollider>();

        for(int i = 0; i < this.transform.childCount; i++)
        {
            Transform child = this.transform.GetChild(i);
            Pokemon pkmn = child.GetComponent<Pokemon>();
            SphereCollider coll = child.GetComponent<SphereCollider>();

            if (pkmn != null && coll != null)
            {
                WildPokemons.Add(coll);
            }
        }

        for (int i = 0; i < _amount; i++)
        {
            int chosen = Random.Range(0, _picks.Length);
            Vector3 position = new Vector3(Random.Range(2f, 5f), 0f, Random.Range(-4f, 4f));
            GameObject go = (GameObject)GameObject.Instantiate(_picks[chosen], position, Quaternion.identity);
            go.transform.SetParent(this.transform, true);

            Pokemon pkmn = go.GetComponent<Pokemon>();
            if (pkmn == null)
            {
                pkmn = go.AddComponent<Pokemon>();
            }

            if (pkmn._deformation == null)
            {
                Deformation deformation = go.GetComponentInChildren<Deformation>();
                if (deformation == null)
                {

                    Transform armature = go.transform.Find("Armature");
                    if (armature != null)
                    {
                        deformation = armature.gameObject.AddComponent<Deformation>();
                    }
                }

                if (deformation != null)
                {
                    pkmn._deformation = deformation;
                }
            }

            SkinnedMeshRenderer skinnedMesh = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                Transform spawningParticleObj = go.transform.Find("SpawningParticle");
                if (spawningParticleObj == null)
                {
                    GameObject obj = (GameObject)GameObject.Instantiate(_spawningParticle, this.transform.position, Quaternion.identity);
                    spawningParticleObj = obj.transform;
                    spawningParticleObj.SetParent(go.transform);
                }

                ParticleSystem particleSystem = spawningParticleObj.GetComponent<ParticleSystem>();
                UnityEngine.ParticleSystem.ShapeModule module = particleSystem.shape;
                module.skinnedMeshRenderer = skinnedMesh;
                pkmn._spawningParticle = particleSystem;



                Transform recallingParticleObj = go.transform.Find("RecallingParticle");
                if (recallingParticleObj == null)
                {
                    GameObject obj = (GameObject)GameObject.Instantiate(_recallingParticle, this.transform.position, Quaternion.identity);
                    recallingParticleObj = obj.transform;
                    recallingParticleObj.SetParent(go.transform);
                }

                particleSystem = recallingParticleObj.GetComponent<ParticleSystem>();
                module = particleSystem.shape;
                module.skinnedMeshRenderer = skinnedMesh;
                pkmn._recallingParticle = particleSystem;
            }

            SphereCollider coll = go.GetComponent<SphereCollider>();
            if (coll != null)
            {
                WildPokemons.Add(coll);
            }
        }
	}
	
	void Update()
    {
	
	}
}
