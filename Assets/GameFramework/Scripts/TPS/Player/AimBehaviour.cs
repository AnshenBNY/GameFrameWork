using UnityEngine;
using System.Collections;
using UnityEngine.Scripting.APIUpdating;

namespace GameFramework.TPS.Player
{

// AimBehaviour inherits from GenericBehaviour. This class corresponds to aim and strafe behaviour.
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "AimBehaviour")]
public class AimBehaviour : GenericBehaviour
{
	public string aimButton = "Aim", shoulderButton = "Aim Shoulder";     // Default aim and switch shoulders buttons.
	public bool toggleAimWithSingleClick = false;                         // True: right click toggles aim. False: hold-to-aim.
	public Texture2D crosshair;                                           // Crosshair texture.
	public float aimTurnSmoothing = 0.15f;                                // Speed of turn response when aiming to match camera facing.
	public Vector3 aimPivotOffset = new Vector3(0.5f, 1.2f,  0f);         // Offset to repoint the camera when aiming.
	public Vector3 aimCamOffset   = new Vector3(0f, 0.4f, -0.7f);         // Offset to relocate the camera when aiming.

	private int aimBool;                                                  // Animator variable related to aiming.
	private bool aim;                                                     // Boolean to determine whether or not the player is aiming.
	private int cornerBool;                                               // Animator variable related to cover corner..
	private bool peekCorner;                                              // Boolean to get whether or not the player is on a cover corner.
	private Vector3 initialRootRotation;                                  // Initial root bone local rotation.
	private Vector3 initialHipsRotation;                                  // Initial hips rotation related to the root bone.
	private Vector3 initialSpineRotation;                                 // Initial spine rotation related to the root bone.
	private bool wasAimInputPressed;                                      // Previous frame aim input state.
	private bool isAimTransitioning;                                      // Prevent duplicate aim on/off coroutines.

	// Start is always called after any Awake functions.
	void Start ()
	{
		// Set up the references.
		aimBool = Animator.StringToHash("Aim");

		cornerBool = Animator.StringToHash("Corner");

		// Get initial bone rotation values.
		Transform hips = behaviourManager.GetAnim.GetBoneTransform(HumanBodyBones.Hips);
		Transform spine = behaviourManager.GetAnim.GetBoneTransform(HumanBodyBones.Spine);
		Transform root = hips.parent;
		// Correctly set the hip and root bones.
		if (spine.parent != hips)
		{
			root = hips;
			hips = spine.parent;
		}
		initialRootRotation = (root == transform) ? Vector3.zero : root.localEulerAngles;
		initialHipsRotation = hips.localEulerAngles;
		initialSpineRotation = behaviourManager.GetAnim.GetBoneTransform(HumanBodyBones.Spine).localEulerAngles;
	}

	// Update is used to set features regardless the active behaviour.
	void Update ()
	{
		peekCorner = behaviourManager.GetAnim.GetBool(cornerBool);
		bool isAimInputPressed = Input.GetAxisRaw(aimButton) != 0f;

		// Toggle aim mode by a single click (input rising edge).
		if (toggleAimWithSingleClick && isAimInputPressed && !wasAimInputPressed && !isAimTransitioning)
		{
			if (!aim)
				StartCoroutine(ToggleAimOn());
			else
				StartCoroutine(ToggleAimOff());
		}
		// Default hold-to-aim mode.
		else if (!toggleAimWithSingleClick && !isAimTransitioning)
		{
			if (isAimInputPressed && !aim)
			{
				StartCoroutine(ToggleAimOn());
			}
			else if (aim && !isAimInputPressed)
			{
				StartCoroutine(ToggleAimOff());
			}
		}

		wasAimInputPressed = isAimInputPressed;

		// No sprinting while aiming.
		canSprint = !aim;

		// Toggle camera aim position left or right, switching shoulders.
		if (aim && Input.GetButtonDown (shoulderButton) && !peekCorner)
		{
			aimCamOffset.x = aimCamOffset.x * (-1);
			aimPivotOffset.x = aimPivotOffset.x * (-1);
		}

		// Set aim boolean on the Animator Controller.
		behaviourManager.GetAnim.SetBool (aimBool, aim);
	}

	// Co-routine to start aiming mode with delay.
	private IEnumerator ToggleAimOn()
	{
		isAimTransitioning = true;
		yield return new WaitForSeconds(0.05f);
		// Aiming is not possible.
		if (behaviourManager.GetTempLockStatus(this.behaviourCode) || behaviourManager.IsOverriding(this))
		{
			isAimTransitioning = false;
			yield break;
		}

		// Start aiming.
		else
		{
			aim = true;
			int signal = 1;
			if (peekCorner)
			{
				signal = (int)Mathf.Sign(behaviourManager.GetH);
			}
			aimCamOffset.x = Mathf.Abs(aimCamOffset.x) * signal;
			aimPivotOffset.x = Mathf.Abs(aimPivotOffset.x) * signal;
			yield return new WaitForSeconds(0.1f);
			behaviourManager.GetAnim.SetFloat(speedFloat, 0);
			// This state overrides the active one.
			behaviourManager.OverrideWithBehaviour(this);
		}
		isAimTransitioning = false;
	}

	// Co-routine to end aiming mode with delay.
	private IEnumerator ToggleAimOff()
	{
		isAimTransitioning = true;
		aim = false;
		yield return new WaitForSeconds(0.3f);
		behaviourManager.GetCamScript.ResetTargetOffsets();
		behaviourManager.GetCamScript.ResetMaxVerticalAngle();
		yield return new WaitForSeconds(0.05f);
		behaviourManager.RevokeOverridingBehaviour(this);
		isAimTransitioning = false;
	}

	// LocalFixedUpdate overrides the virtual function of the base class.
	public override void LocalFixedUpdate()
	{
		// Set camera position and orientation to the aim mode parameters.
		if(aim)
			behaviourManager.GetCamScript.SetTargetOffsets (aimPivotOffset, aimCamOffset);
	}

	// LocalLateUpdate: manager is called here to set player rotation after camera rotates, avoiding flickering.
	public override void LocalLateUpdate()
	{
		AimManagement();
	}

	// Handle aim parameters when aiming is active.
	void AimManagement()
	{
		// Deal with the player orientation when aiming.
		Rotating();
	}

	// Rotate the player to match correct orientation, according to camera.
	void Rotating()
	{
		Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);
		// Player is moving on ground, Y component of camera facing is not relevant.
		forward.y = 0.0f;
		forward = forward.normalized;

		// Always rotates the player according to the camera horizontal rotation in aim mode.
		Quaternion targetRotation =  Quaternion.Euler(0, behaviourManager.GetCamScript.GetH, 0);

		float minSpeed = Quaternion.Angle(transform.rotation, targetRotation) * aimTurnSmoothing;

		// Peeking corner situation.
		if (peekCorner)
		{
			// Rotate only player upper body when peeking a corner.
			transform.rotation = Quaternion.LookRotation(-behaviourManager.GetLastDirection());
			targetRotation *= Quaternion.Euler(initialRootRotation);
			targetRotation *= Quaternion.Euler(initialHipsRotation);
			targetRotation *= Quaternion.Euler(initialSpineRotation);
			Transform spine = behaviourManager.GetAnim.GetBoneTransform(HumanBodyBones.Spine);
			spine.rotation = targetRotation;
		}
		else
		{
			// Rotate entire player to face camera.
			behaviourManager.SetLastDirection(forward);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, minSpeed * Time.deltaTime);
		}
	}

 	// Draw the crosshair when aiming.
	void OnGUI () 
	{
		if (crosshair)
		{
			float mag = behaviourManager.GetCamScript.GetCurrentPivotMagnitude(aimPivotOffset);
			if (mag < 0.05f)
				GUI.DrawTexture(new Rect(Screen.width / 2 - (crosshair.width * 0.5f),
										 Screen.height / 2 - (crosshair.height * 0.5f),
										 crosshair.width, crosshair.height), crosshair);
		}
	}
}
}
