using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Core;
using GameFramework.TPS.UI;
using GameFramework.TPS.Player;
using UnityEngine.Scripting.APIUpdating;

namespace GameFramework.TPS.Weapon
{

// This class corresponds to any in-game weapon interactions.
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "InteractiveWeapon")]
public class InteractiveWeapon : MonoBehaviour
{
	public string label;                                      // The weapon name. Same name will treat weapons as same regardless game object's name.
	public AudioClip shotSound, reloadSound,                  // Audio clips for shoot and reload.
		pickSound, dropSound, noBulletSound;                  // Audio clips for pick weapon , drop weapon, and no bullet shot try.
	public Sprite sprite;                                     // Weapon sprite to show on screen HUD.
	public Vector3 rightHandPosition;                         // Position offsets relative to the player's right hand.
	public Vector3 relativeRotation;                          // Rotation Offsets relative to the player's right hand.
	public float bulletDamage = 10f;                          // Damage of one shot.
	public float recoilAngle;                                 // Angle of weapon recoil.
	public enum WeaponType                                    // Weapon types, related to player's shooting animations.
	{
		NONE,
		SHORT,
		LONG
	}
	public enum WeaponMode                                    // Weapon shooting modes.
	{
		SEMI,
		BURST,
		AUTO
	}
	public WeaponType type = WeaponType.NONE;                 // Default weapon type, change in Inspector.
	public WeaponMode mode = WeaponMode.SEMI;                 // Default weapon mode, change in Inspector.
	public int burstSize = 0;                                 // How many shot are fired on burst mode.
	[SerializeField]
	private int mag, totalBullets;                            // Current mag capacity and total amount of bullets being carried.
	private int fullMag, maxBullets;                          // Default mag capacity and total bullets for reset purposes.
	private GameObject player;                                // Reference to the player.
	private ShootBehaviour playerInventory;                   // Player's inventory to store weapons.
	private SphereCollider interactiveRadius;                 // In-game radius of interaction with player.
	private BoxCollider col;                                  // Weapon collider.
	private Rigidbody rbody;                                  // Weapon rigidbody.
	private WeaponUIManager weaponHud;                        // Reference to on-screen weapon HUD.
	private bool pickable;                                    // Boolean to store whether or not the weapon is pickable (player within radius).
	private Transform pickupHUD;                              // Reference to the weapon pickup in-game label.
	private bool initialized;
	private bool initFailedLogged;

	void Awake()
	{
		CacheSelfState();
	}

	private void Start()
	{
		if (TryInitializeDependencies())
		{
			return;
		}

		StartCoroutine(RetryInitializeDependencies());
	}

	private void CacheSelfState()
	{
		// Awake 仅处理本对象内状态，避免跨对象初始化顺序问题。
		this.gameObject.name = this.label;
		this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		foreach (Transform t in this.transform)
		{
			t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		}

		// Assert that an weapon slot is set up.
		if (this.type == WeaponType.NONE)
		{
			Debug.LogWarning("Set correct weapon slot ( 1 - small/ 2- big)");
			type = WeaponType.SHORT;
		}

		// Assert that the gun muzzle is exists.
		if(!this.transform.Find("muzzle"))
		{
			Debug.LogError(this.name+" muzzle is not present. Create a game object named 'muzzle' as a child of this game object");
		}

		// Set default values.
		fullMag = mag;
		maxBullets = totalBullets;
	}

	private bool TryInitializeDependencies()
	{
		RuntimeContext context = RuntimeContext.Instance;
		if (context == null)
		{
			return false;
		}

		if (!context.TryGetPlayer(out player, out playerInventory))
		{
			return false;
		}

		weaponHud = context.WeaponUIManager;
		if (weaponHud == null)
		{
			return false;
		}

		pickupHUD = context.PickupHud;
		if (pickupHUD == null)
		{
			return false;
		}

		if (transform.childCount == 0)
		{
			return false;
		}

		Transform modelRoot = this.transform.GetChild(0);
		col = modelRoot.GetComponent<BoxCollider>();
		if (col == null)
		{
			col = modelRoot.gameObject.AddComponent<BoxCollider>();
		}

		EnsureInteractiveRadius(col.center);
		rbody = GetComponent<Rigidbody>();
		if (rbody == null)
		{
			rbody = gameObject.AddComponent<Rigidbody>();
		}

		pickupHUD.gameObject.SetActive(false);
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
			Debug.LogError("InteractiveWeapon: initialization failed, missing Player/ShootBehaviour/UI references in scene.", this);
			enabled = false;
		}
	}

	// Create the sphere of interaction with player.
	private void EnsureInteractiveRadius(Vector3 center)
	{
		interactiveRadius = GetComponent<SphereCollider>();
		if (interactiveRadius == null)
		{
			interactiveRadius = gameObject.AddComponent<SphereCollider>();
		}
		interactiveRadius.center = center;
		interactiveRadius.radius = 1f;
		interactiveRadius.isTrigger = true;
	}

	void Update()
	{
		if (!initialized)
		{
			return;
		}

		// Handle player pick weapon action.
		if (this.pickable && Input.GetButtonDown(playerInventory.pickButton))
		{
			// Disable weapon physics.
			rbody.isKinematic = true;
			this.col.enabled = false;
			
			// Setup weapon and add in player inventory.
			playerInventory.AddWeapon(this);
			Destroy(interactiveRadius);
			this.Toggle(true);
			this.pickable = false;

			// Change active weapon HUD.
			TogglePickupHUD(false);
		}
	}

	// Handle weapon collision with environment.
	private void OnCollisionEnter(Collision collision)
	{
		if(collision.collider.gameObject != player && Vector3.Distance(transform.position, player.transform.position) <= 5f)
		{
			AudioSource.PlayClipAtPoint(dropSound, transform.position, 0.5f);
		}
	}

	// Handle player exiting radius of interaction.
	private void OnTriggerExit(Collider other)
	{
		if (!initialized)
		{
			return;
		}

		if (other.gameObject == player)
		{
			pickable = false;
			TogglePickupHUD(false);
		}
	}

	// Handle player within radius of interaction.
	void OnTriggerStay(Collider other)
	{
		if (!initialized)
		{
			return;
		}

		if (other.gameObject == player && playerInventory && playerInventory.isActiveAndEnabled)
		{
			pickable = true;
			TogglePickupHUD(true);
		}
	}

	// Draw in-game weapon pickup label.
	private void TogglePickupHUD(bool toggle)
	{
		if (pickupHUD == null)
		{
			return;
		}

		pickupHUD.gameObject.SetActive(toggle);
		if (toggle)
		{
			pickupHUD.position = this.transform.position + Vector3.up * 0.5f;
			Vector3 direction = player.GetComponent<BasicBehaviour>().playerCamera.forward;
			direction.y = 0f;
			pickupHUD.rotation = Quaternion.LookRotation(direction);
			pickupHUD.Find("Label").GetComponent<Text>().text = "Pick "+this.gameObject.name;
		}
	}

	// Manage weapon active status.
	public void Toggle(bool active)
	{
		if (active)
			AudioSource.PlayClipAtPoint(pickSound, transform.position, 0.5f);
		weaponHud.Toggle(active);
		UpdateHud();
	}

	// Manage the drop action.
	public void Drop()
	{
		this.gameObject.SetActive(true);
		this.transform.position += Vector3.up;
		rbody.isKinematic = false;
		this.transform.parent = null;
		EnsureInteractiveRadius(col.center);
		this.col.enabled = true;
		weaponHud.Toggle(false);
	}

	// Start the reload action (called by shoot behaviour).
	public bool StartReload()
	{
		if (mag == fullMag || totalBullets == 0)
			return false;
		else if(totalBullets < fullMag - mag)
		{
			mag += totalBullets;
			totalBullets = 0; 
		}
		else
		{
			totalBullets -= fullMag - mag;
			mag = fullMag;
		}

		return true;
	}

	// End the reload action (called by shoot behaviour).
	public void EndReload()
	{
		UpdateHud();
	}

	// Manage shoot action.
	public bool Shoot(bool firstShot=true)
	{
		if (mag > 0)
		{
			mag--;
			UpdateHud();
			return true;
		}
		if (firstShot && noBulletSound)
			AudioSource.PlayClipAtPoint(noBulletSound, this.transform.Find("muzzle").position, 5f);
		return false;
	}

	// Reset the bullet parameters.
	public void ResetBullets()
	{
		mag = fullMag;
		totalBullets = maxBullets;
	}

	// Update weapon screen HUD.
	private void UpdateHud()
	{
		weaponHud.UpdateWeaponHUD(sprite, mag, fullMag, totalBullets);
	}
}
}
