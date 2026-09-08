using System.Collections;
using UnityEngine;
using GameFramework.Core;
using UnityEngine.Scripting.APIUpdating;

namespace GameFramework.TPS.UI
{

// This class is created for the example scene. There is no support for this script.
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "HintManagement")]
public class HintManagement : MonoBehaviour
{
	public string message = "";
	public string message2 = "";
	public KeyCode changeMessageKey;

	private GameObject player;
	private bool used = false;
	private bool initialized;
	private bool initFailedLogged;

	void Awake()
	{
		// Awake 仅做轻量初始化，依赖解析放到 Start。
	}

	private void Start()
	{
		if (TryInitializeDependencies())
		{
			return;
		}

		StartCoroutine(RetryInitializeDependencies());
	}

	private bool TryInitializeDependencies()
	{
		player = RuntimeContext.Instance != null ? RuntimeContext.Instance.Player : null;
		if (player == null)
		{
			return false;
		}

		initialized = true;
		return true;
	}

	private IEnumerator RetryInitializeDependencies()
	{
		const float retryDuration = 2f;
		const float retryInterval = 0.2f;
		float deadline = Time.time + retryDuration;
		while (!initialized && Time.time < deadline)
		{
			yield return new WaitForSeconds(retryInterval);
			if (TryInitializeDependencies())
			{
				yield break;
			}
		}

		if (!initialized && !initFailedLogged)
		{
			initFailedLogged = true;
			Debug.LogError("HintManagement: initialization failed, Player not found in scene.", this);
			enabled = false;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (!initialized)
		{
			return;
		}

		if((other.gameObject == player) && !used)
		{
			UIEventChannel.RequestShowHint(message);
			used = true;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (!initialized)
		{
			return;
		}

		if(other.gameObject == player)
		{
			UIEventChannel.RequestHideHint();
			Destroy(gameObject);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!initialized)
		{
			return;
		}

		if(message2 != "" && other.gameObject == player && Input.GetKeyDown(changeMessageKey))
		{
			UIEventChannel.RequestShowHint(message2);
		}
	}
}
}
