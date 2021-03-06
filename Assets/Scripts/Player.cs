using UnityEngine;

namespace TanksMP
{
	public class Player : MonoBehaviour
	{
		/// <summary>
		/// Current turret rotation and shooting direction.
		/// </summary>
		[HideInInspector]
		public int turretRotation;
        
		/// <summary>
		/// Delay between shots.
		/// </summary>
		public float fireRate = 0.75f;

		/// <summary>
		/// Movement speed in all directions.
		/// </summary>
		public float moveSpeed = 8f;

		/// <summary>
		/// Clip to play when a shot has been fired.
		/// </summary>
		public AudioClip shotClip;
        
		/// <summary>
		/// Object to spawn on shooting.
		/// </summary>
		public GameObject shotFX;

		/// <summary>
		/// Turret to rotate with look direction.
		/// </summary>
		public Transform turret;
        
		/// <summary>
		/// Position to spawn new bullets at.
		/// </summary>
		public Transform shotPos;
      
		/// <summary>
		/// Bullet object for shooting.
		/// </summary>
		public GameObject bullet;
        
		/// <summary>
		/// Reference to the camera following component.
		/// </summary>
		[HideInInspector]
		public FollowTarget camFollow;

		//timestamp when next shot should happen
		private float nextFire;

		private Rigidbody rb;

		public void Awake ()
		{
			//get components and set camera target
			rb = GetComponent<Rigidbody> ();

            camFollow = Camera.main.GetComponent<FollowTarget>();
            camFollow.target = turret;
        }

        void FixedUpdate ()
        {
            if ((rb.constraints & RigidbodyConstraints.FreezePositionY) != RigidbodyConstraints.FreezePositionY)
            {
                if (transform.position.y > 0)
                    rb.AddForce(Physics.gravity * 2f, ForceMode.Acceleration);
            }

            //movement variables
            Vector2 moveDir;
            Vector2 turnDir;

            //reset moving input when no arrow keys are pressed down 
            if (Input.GetAxisRaw ("Horizontal") == 0 && Input.GetAxisRaw ("Vertical") == 0)
            {
                moveDir.x = 0;
                moveDir.y = 0;

            }else
            {
                //read out moving directions and calculate force
                moveDir.x = Input.GetAxis("Horizontal");
                moveDir.y = Input.GetAxis("Vertical");
                Move(moveDir);

            }

            //cast a ray on a plan at the mouse position for detecting where to shoot
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.up);
            float distance = 0f;
            Vector3 hitPos = Vector3.zero;
            //the hit position determines the mouse position in the scene
            if (plane.Raycast (ray, out distance))
            {
                hitPos = ray.GetPoint(distance) - transform.position;
            }

            //we've converted the mouse position to a direction 
            turnDir = new Vector2(hitPos.x, hitPos.z);

            //rotate turret to look at the mouse direction 
            RotateTurret (new Vector2(hitPos.x, hitPos.z));

            if (Input.GetButton("Fire1"))
                Shoot(); 
        }

        //moves rigidbody in the direction passed in 
        void Move (Vector2 direction = default(Vector2))
        {
            //if direction is not zero, rotate player in the moving direction relative to camera
            if (direction != Vector2.zero)
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y))
                * Quaternion.Euler(0, camFollow.camTransform.eulerAngles.y, 0);

            //create movement vector based on current rotation and speed 
            Vector3 movementDir = transform.forward * moveSpeed * Time.deltaTime;
            //apply vector to rigidbody position
            rb.MovePosition(rb.position + movementDir);

        }

        void RotateTurret (Vector2 direction = default(Vector2))
        {

            //dont rotate without values
            if (direction == Vector2.zero)
                return;

            //get rotatin value as angle out of the direction we received
            int newRotation = (int)(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y + camFollow.camTransform.eulerAngles.y);

            turretRotation = newRotation;

            turret.rotation = Quaternion.Euler(0, newRotation, 0);

        }

        // shoots a bullet in the direction passed in 
        void Shoot (Vector2 direction = default (Vector2))
        {
            //if shot delay is over 
            if (Time.time > nextFire)
            {

                //set next shot timestamp
                nextFire = Time.time + fireRate;

                //spawn bullet using pooling locally
                GameObject obj = PoolManager.Spawn(bullet, shotPos.position, turret.rotation);
                Bullet blt = obj.GetComponent<Bullet>();

                if (shotFX)
                    PoolManager.Spawn (shotFX, shotPos.position, Quaternion.identity);
                if (shotClip)
                    AudioManager.Play3D (shotClip, shotPos.position, 0.1f);
            }
        }

	}
}