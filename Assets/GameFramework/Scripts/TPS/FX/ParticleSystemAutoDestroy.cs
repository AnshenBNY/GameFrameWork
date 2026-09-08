using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace GameFramework.TPS.FX
{

// This class ensures that a particle's game object will auto-destroy after its lifetime.
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "ParticleSystemAutoDestroy")]
public class ParticleSystemAutoDestroy : MonoBehaviour
{
	private ParticleSystem ps;

	public void Start()
	{
		// Set up the references.
		ps = GetComponent<ParticleSystem>();
	}

	public void Update()
	{
		// Check if lifetime has ended to destroy it.
		if (ps)
		{
			if (!ps.IsAlive())
			{
				Destroy(gameObject);
			}
		}
	}
}
}
