using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public static List<SphereCollider> WildPokemons = new List<SphereCollider>();

    public int _amount = 5;
    public GameObject[] _picks;

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

	    for (int i = 0; i <= _amount; i++)
        {
            int chosen = Random.Range(0, _picks.Length);
            Vector3 position = new Vector3(Random.Range(2f, 5f), 0f, Random.Range(-4f, 4f));
            GameObject go = (GameObject)GameObject.Instantiate(_picks[chosen], position, Quaternion.identity);
            go.transform.SetParent(this.transform, true);

            if (go.GetComponent<Pokemon>() == null)
            {
                go.AddComponent<Pokemon>();
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
