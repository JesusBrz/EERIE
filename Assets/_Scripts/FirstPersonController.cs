using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Important Systems")]
		private UIManager uimanager;


		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to fire again. Set to 0f to instantly fire again")]
		public float FireTimeout = 0.15f;


		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;
		public CapsuleCollider _collider;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		[Header("Animation Parameters")]
		public Animator _anim;
		private string _animAttackTrigger = "Attack";
		private string _animWeaponInt = "CurrentWeapon";
		private string _animChangeWeaponTrigger = "WeaponChange";
		private string _animSkillInt = "Skill";
		private string _animSmartWatchBool = "SmartWatch";

		[Header("AnimationsDictionary")]
		public string _animationIdle = "Anim_Arms_Idle";
		private string _animationBibleHit = "Anim_Arms_BibleHit";
		private string _animationHolyWater = "Anim_Arms_HolyWater";
		private string _animationBibloomerang = "Anim_Arms_Bibloomerang";
		private string _animationSmartWatch = "Anim_Arms_SmartWatch";

		[Header("Weapons")]
		public int _currentWeaponIndex =0;
		private float _previousWeaponIndex;
		public GameObject currentWeapon;
		public GameObject[] weapons;
		public bool _inBiblioomerang;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;
		private float _autoRecoveryAmount = 0.05f;

		// Weapons


		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		public  StarterAssetsInputs _input;
		private GameObject _mainCamera;
		public GameObject _arms;

		private const float _threshold = 0.01f;

		private AudioSource _backgroundMusic;
		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

			uimanager = UIManager.Instance;
			_backgroundMusic = GetComponent<AudioSource>();
			_backgroundMusic.enabled = false;

		}

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
			_collider = GetComponentInChildren<CapsuleCollider>();
			_arms = GameObject.FindGameObjectWithTag("Arms");
			_arms.transform.SetParent(_mainCamera.transform);
            foreach (GameObject w in weapons)
            {
                w.SetActive(false);
            }

			_anim = GameObject.FindGameObjectWithTag("Arms").GetComponent<Animator>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            currentWeapon = weapons[0];
            currentWeapon.SetActive(true);
            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

			
        }

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Move();
			ChangeWeapon();
			Fire();
			Fire2();
			//Pause();
			Map();


            if (GameManager.Instance._healthCount < 100)
            {
				AutoRecovering();
            }
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void InputTestsMethod()
        {
			
        }

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{

			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold  && !GameManager.Instance.Ispaused)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;
				
				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}
        #region Input Methods
        private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout

				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private void ChangeWeapon()
        {
			int  index = _input.currentWeapon;
			if  (index != _currentWeaponIndex && verifyAnimator()) 
            {
				StartCoroutine(ShowWeapon(index));
            }
        }

		public bool verifyAnimator()
        {
			AnimatorStateInfo animState = _anim.GetCurrentAnimatorStateInfo(0);

			if (animState.IsName(_animationBibleHit) || animState.IsName(_animationHolyWater) || _inBiblioomerang)
			{
				return false;

            }
            else
            {
				return true;
			}					
        }


		private void Fire()
        {
			InputAction _fire = _playerInput.actions["UseWeapon"];
			if (_fire.WasPressedThisFrame() && _anim.GetCurrentAnimatorStateInfo(0).IsName(_animationIdle) && !_inBiblioomerang)
			{
				doAttack(1);
			}
		}

		private void Fire2()
		{
			InputAction _fire2 = _playerInput.actions["Fire2"];
			if (_fire2.WasPressedThisFrame() && _anim.GetCurrentAnimatorStateInfo(0).IsName(_animationIdle) && !_inBiblioomerang)
			{
				doAttack(2);
			}
		}

		public void doAttack(int skillNumber)
        {
			_anim.SetBool(_animAttackTrigger, true);			
			_anim.SetInteger(_animWeaponInt, _currentWeaponIndex);
			_anim.SetInteger(_animSkillInt, skillNumber);

			if (currentWeapon.name == "Bible")
            {
				if(skillNumber == 1)
					StartCoroutine(BibleHit());
				if (skillNumber == 2)
					StartCoroutine(Bibloomerang());
			}
				
			if (currentWeapon.name == "HolyWater")
            {
				if (skillNumber == 1)
					StartCoroutine(HolyWaterHit());
				if (skillNumber == 2)
					StartCoroutine(HolyWaterHit());
			}				
		}
		#endregion

		private IEnumerator ShowWeapon(int index)
		{
			_anim.SetTrigger(_animChangeWeaponTrigger);
			yield return new WaitForSeconds(0.5f);
			_anim.ResetTrigger(_animChangeWeaponTrigger);

			currentWeapon.SetActive(false);
			currentWeapon = weapons[index];
			currentWeapon.SetActive(true);
			_currentWeaponIndex = index;
			UIManager.Instance.activateUiCon(index);
		}


		#region Weapons Hits
		public IEnumerator BibleHit()
        {

			#region Bible Movement

			StartCoroutine(AnimatorTriggersController(_animAttackTrigger));
			currentWeapon.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
			
			
			yield return null;

			#endregion

			#region  EnemyDetection

			//Aqui va la detección de enemigos y su deteccion
			#endregion
		}

		public IEnumerator Bibloomerang()
		{
			currentWeapon.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			StartCoroutine(AnimatorTriggersController(_animAttackTrigger));

			Vector3 OriginalScale = currentWeapon.transform.localScale;
			GameObject OriginalPosition = Instantiate(new GameObject("OriginalBiblePosition"), currentWeapon.transform.parent);
			OriginalPosition.transform.localPosition = currentWeapon.transform.localPosition ;
			OriginalPosition.transform.localRotation = currentWeapon.transform.localRotation ;

			currentWeapon.transform.SetParent(null);
			currentWeapon.layer = 12;

			float shotForce = 5f;
			Rigidbody bibleRigidbody = currentWeapon.GetComponent<Rigidbody>();
			bibleRigidbody.AddForce(-OriginalPosition.transform.up * shotForce);
			bibleRigidbody.AddTorque(OriginalPosition.transform.right * (shotForce*8), ForceMode.Impulse);
			
			_inBiblioomerang = true;

			yield return new WaitForSeconds(1f);

			bibleRigidbody.velocity = new Vector3(0f, 0f, 0f);
			
			currentWeapon.layer = 9;
			currentWeapon.transform.SetParent(OriginalPosition.transform.parent);
			while (currentWeapon.transform.position != OriginalPosition.transform.position)
            {
				currentWeapon.transform.position = Vector3.MoveTowards(currentWeapon.transform.position, OriginalPosition.transform.position, (shotForce*4)*Time.deltaTime);
				yield return new WaitForEndOfFrame();
			}

			currentWeapon.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
			bibleRigidbody.angularVelocity = new Vector3(0f, 0f, 0f);
			currentWeapon.transform.rotation = OriginalPosition.transform.rotation;
			currentWeapon.transform.localScale = OriginalScale;
			
			Destroy(OriginalPosition);
			Destroy(GameObject.Find("OriginalBiblePosition"));

			while(currentWeapon.GetComponent<Rigidbody>().constraints != RigidbodyConstraints.FreezeAll)
            {
				yield return new WaitForEndOfFrame();				
			}
			_inBiblioomerang = false;
		}

		public IEnumerator HolyWaterHit()
        {
			StartCoroutine(AnimatorTriggersController(_animAttackTrigger));
			yield return null;

			#region  EnemyDetection

			//Aqui va la detección de enemigos y su deteccion
			#endregion

		}
		private IEnumerator AnimatorTriggersController(string animatorTrigger)
		{
			_anim.SetTrigger(animatorTrigger);
			yield return new WaitForSeconds(0.5f);
			_anim.ResetTrigger(animatorTrigger);
			
		}
		#endregion

		#region UI Input

		private void Pause()
        {
			if (_input.PauseButtonDown)
            {
				_input.PauseButtonDown = false;
				UIManager.Instance.Pause();			
            }
        }

		private void Map()
        {
			InputAction _map = _playerInput.actions["Map"];
			if (_map.WasPressedThisFrame())
            {
			 _anim.SetBool(_animSmartWatchBool,	_anim.GetBool(_animSmartWatchBool) ? false : true);
			}			
        }

        #endregion



        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Enemy")
            {
                UIManager.Instance.UpdateHealth(-10);

            }
        }

		public void AutoRecovering()
        {
			UIManager.Instance.UpdateHealth(_autoRecoveryAmount);
		}


        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}