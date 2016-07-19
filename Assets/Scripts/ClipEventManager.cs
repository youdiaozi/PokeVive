using UnityEngine;
using System.Collections;

public class ClipEventManager : MonoBehaviour
{
	public static ClipEventManager singleton { get; private set; }

    public GameObject[] _cones;
    public Renderer[] _manikinHeadsParts;
    public Material[] _manikinHeadsRealMaterials;
    public Material _torsoMat;

    public Renderer[] _hands;
    public Material _guyMat;
    public Material _thomasMat;
    public Material _skinMat;

    void Awake()
    {
        if (singleton != null)
        {
            Debug.LogError("ClipEventManager is not unique.");
            DestroyImmediate(this);
            return;
        }

        singleton = this;
    }

    void Start()
	{
		
	}
	
	void Update()
	{
		
	}

    public void ClipEvent(string obj, string clip)
    {
        if (clip == "player head")
        {
            if (obj == "thomas")
            {
                foreach (Renderer rend in _hands)
                {
                    rend.material = _thomasMat;
                }
            }
            else if (obj == "guy")
            {
                foreach (Renderer rend in _hands)
                {
                    rend.material = _guyMat;
                }
            }
        }

        if (obj == "manikin head")
        {
            if (clip == "player head")
            {
                foreach (Renderer rend in _hands)
                {
                    rend.material = _skinMat;
                }

                foreach (GameObject cone in _cones)
                {
                    cone.SetActive(false);
                }

                for (int i = 0; i < _manikinHeadsParts.Length; i++)
                {
                    _manikinHeadsParts[i].material = _manikinHeadsRealMaterials[i];
                }
            }
            else if (clip == "manikin head")
            {
                for (int i = 0; i < _manikinHeadsParts.Length; i++)
                {
                    _manikinHeadsParts[i].material = _torsoMat;
                }

                foreach (GameObject cone in _cones)
                {
                    cone.SetActive(false);
                }
            }
        }
    }

    public void UnclipEvent(string obj, string clip)
    {
        if (obj == "manikin head")
        {
            foreach (GameObject cone in _cones)
            {
                cone.SetActive(true);
            }

            if (clip == "player head")
            {
                for (int i = 0; i < _manikinHeadsParts.Length && i < _manikinHeadsRealMaterials.Length; i++)
                {
                    _manikinHeadsParts[i].material = _torsoMat;
                }
            }
        }
    }
}