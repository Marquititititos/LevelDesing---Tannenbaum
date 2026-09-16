using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Implements game physics for some in game entity.
    /// </summary>
    public class KinematicObject : MonoBehaviour
    {
        /// <summary>
        /// The minimum normal (dot product) considered suitable for the entity sit on.
        /// </summary>
        public float minGroundNormalY = .65f;

        /// <summary>
        /// A custom gravity coefficient applied to this entity.
        /// </summary>
        public float gravityModifier = 1f;

        [Tooltip("If disabled, gravity is not applied and targetVelocity.y controls vertical movement. Useful for flying enemies.")]
        public bool useGravity = true;

        /// <summary>
        /// The current velocity of the entity.
        /// </summary>
        public Vector2 velocity;

        /// <summary>
        /// Is the entity currently sitting on a surface?
        /// </summary>
        /// <value></value>
        public bool IsGrounded { get; private set; }

        protected Vector2 targetVelocity;
        protected Vector2 externalMovement;
        protected Vector2 groundNormal;
        protected CircularMovingPlatform groundPlatform;

        protected Rigidbody2D body;
        protected ContactFilter2D contactFilter;
        protected RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

        protected const float minMoveDistance = 0.001f;
        protected const float shellRadius = 0.01f;


        /// <summary>
        /// Bounce the object's vertical velocity.
        /// </summary>
        /// <param name="value"></param>
        public void Bounce(float value)
        {
            velocity.y = value;
        }

        /// <summary>
        /// Bounce the objects velocity in a direction.
        /// </summary>
        /// <param name="dir"></param>
        public void Bounce(Vector2 dir)
        {
            velocity.y = dir.y;
            velocity.x = dir.x;
        }

        /// <summary>
        /// Teleport to some position.
        /// </summary>
        /// <param name="position"></param>
        public void Teleport(Vector3 position)
        {
            body.position = position;
            velocity *= 0;
            body.linearVelocity *= 0;
        }

        protected virtual void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
            body.isKinematic = true;
        }

        protected virtual void OnDisable()
        {
            body.isKinematic = false;
        }

        protected virtual void Start()
        {
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            contactFilter.useLayerMask = true;
        }

        protected virtual void Update()
        {
            targetVelocity = Vector2.zero;
            ComputeVelocity();
        }

        protected virtual void ComputeVelocity()
        {

        }

        protected virtual void FixedUpdate()
        {
            // Find moving platform directly below player.
            DetectMovingPlatform();

            // --------------------------------------------------
            // MOVE WITH PLATFORM FIRST
            // --------------------------------------------------

            if (groundPlatform != null)
            {
                Vector2 platformMove = groundPlatform.DeltaMovement;

                MoveWithPlatform(platformMove, groundPlatform);
            }

            // --------------------------------------------------
            // NORMAL PHYSICS
            // --------------------------------------------------

            if (useGravity)
            {
                if (groundPlatform != null && velocity.y <= 0)
                {
                    // While riding a moving platform, don't let gravity
                    // continuously create a gap between player and platform.
                    velocity.y = -1f;
                }
                else
                {
                    if (velocity.y < 0)
                        velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
                    else
                        velocity += Physics2D.gravity * Time.deltaTime;
                }
            }
            else
            {
                velocity.y = targetVelocity.y;
            }

            velocity.x = targetVelocity.x;

            IsGrounded = false;

            var deltaPosition = velocity * Time.deltaTime;

            var moveAlongGround =
                new Vector2(groundNormal.y, -groundNormal.x);

            var move =
                moveAlongGround * deltaPosition.x;

            PerformMovement(move, false);

            move =
                Vector2.up * deltaPosition.y;

            PerformMovement(move, true);
        }

        void MoveWithPlatform(Vector2 move, CircularMovingPlatform platform)
        {
            if (move.sqrMagnitude <= 0.000001f)
                return;

            // Move horizontally with platform
            if (Mathf.Abs(move.x) > 0.0001f)
            {
                PerformPlatformMovement(
                    new Vector2(move.x, 0),
                    platform
                );
            }

            // Move vertically with platform
            if (Mathf.Abs(move.y) > 0.0001f)
            {
                PerformPlatformMovement(
                    new Vector2(0, move.y),
                    platform
                );
            }
        }

        void DetectMovingPlatform()
        {
            // If we are already riding a platform, keep it unless we've
            // clearly moved away from its top.
            if (groundPlatform != null)
            {
                Collider2D platformCollider =
                    groundPlatform.GetComponent<Collider2D>();

                if (platformCollider != null)
                {
                    Bounds playerBounds = GetComponent<Collider2D>().bounds;
                    Bounds platformBounds = platformCollider.bounds;

                    float playerBottom = playerBounds.min.y;
                    float platformTop = platformBounds.max.y;

                    // Still close enough vertically to be considered riding it.
                    bool closeVertically =
                        Mathf.Abs(playerBottom - platformTop) < 0.20f;

                    // Still horizontally above some part of the platform.
                    bool horizontallyOverlapping =
                        playerBounds.max.x > platformBounds.min.x &&
                        playerBounds.min.x < platformBounds.max.x;

                    if (closeVertically && horizontallyOverlapping)
                    {
                        return;
                    }
                }

                groundPlatform = null;
            }

            // Otherwise try to find a platform underneath us.
            RaycastHit2D[] results = new RaycastHit2D[8];

            int count = body.Cast(
                Vector2.down,
                contactFilter,
                results,
                0.12f
            );

            for (int i = 0; i < count; i++)
            {
                if (results[i].normal.y <= minGroundNormalY)
                    continue;

                CircularMovingPlatform platform =
                    results[i].collider.GetComponentInParent<CircularMovingPlatform>();

                if (platform != null)
                {
                    groundPlatform = platform;
                    return;
                }
            }
        }

        void PerformMovement(Vector2 move, bool yMovement)
        {
            var distance = move.magnitude;

            if (distance > minMoveDistance)
            {
                //check if we hit anything in current direction of travel
                var count = body.Cast(move, contactFilter, hitBuffer, distance + shellRadius);
                for (var i = 0; i < count; i++)
                {
                    // ONE-WAY PLATFORM
                    if (hitBuffer[i].collider.gameObject.layer ==
                        LayerMask.NameToLayer("OneWayPlatform"))
                    {
                        // Don't collide while moving upward.
                        if (move.y > 0)
                            continue;

                        // Don't collide with the underside or sides.
                        if (hitBuffer[i].normal.y < 0.5f)
                            continue;
                    }

                    var currentNormal = hitBuffer[i].normal;

                    // Is this surface flat enough to land on?
                    if (currentNormal.y > minGroundNormalY)
                    {
                        IsGrounded = true;

                        // Remember which moving platform we're standing on.
                        CircularMovingPlatform platform =
                            hitBuffer[i].collider.GetComponentInParent<CircularMovingPlatform>();

                        if (platform != null)
                        {
                            groundPlatform = platform;
                        }

                        if (yMovement)
                        {
                            groundNormal = currentNormal;
                            currentNormal.x = 0;
                        }
                    }
                    if (IsGrounded)
                    {
                        //how much of our velocity aligns with surface normal?
                        var projection = Vector2.Dot(velocity, currentNormal);
                        if (projection < 0)
                        {
                            //slower velocity if moving against the normal (up a hill).
                            velocity = velocity - projection * currentNormal;
                        }
                    }
                    else
                    {
                        // Hit a wall: stop horizontal movement only.
                        if (Mathf.Abs(currentNormal.x) > 0.5f)
                        {
                            velocity.x = 0;
                        }

                        // Hit a ceiling while moving upward: stop vertical movement.
                        if (currentNormal.y < -0.5f && velocity.y > 0)
                        {
                            velocity.y = 0;
                        }
                    }
                    //remove shellDistance from actual move distance.
                    var modifiedDistance = hitBuffer[i].distance - shellRadius;
                    distance = modifiedDistance < distance ? modifiedDistance : distance;
                }
            }
            body.position = body.position + move.normalized * distance;
        }

        void PerformPlatformMovement(
    Vector2 move,
    CircularMovingPlatform platform)
        {
            float distance = move.magnitude;

            if (distance <= minMoveDistance)
                return;

            int count = body.Cast(
                move,
                contactFilter,
                hitBuffer,
                distance + shellRadius
            );

            for (int i = 0; i < count; i++)
            {
                Collider2D hitCollider = hitBuffer[i].collider;

                // -------------------------------------------------
                // IMPORTANT:
                // Ignore the platform currently carrying the player.
                // -------------------------------------------------

                CircularMovingPlatform hitPlatform =
                    hitCollider.GetComponentInParent<CircularMovingPlatform>();

                if (hitPlatform == platform)
                {
                    continue;
                }

                // -------------------------------------------------
                // Still respect every OTHER collision.
                // -------------------------------------------------

                // Other one-way platforms
                if (hitCollider.gameObject.layer ==
                    LayerMask.NameToLayer("OneWayPlatform"))
                {
                    // Ignore them while moving upward
                    if (move.y > 0)
                        continue;

                    // Ignore underside / sides
                    if (hitBuffer[i].normal.y < 0.5f)
                        continue;
                }

                float modifiedDistance =
                    hitBuffer[i].distance - shellRadius;

                if (modifiedDistance < distance)
                {
                    distance = Mathf.Max(modifiedDistance, 0);
                }
            }

            if (distance > 0)
            {
                body.position += move.normalized * distance;
            }
        }

        protected void LeaveMovingPlatform()
        {
            groundPlatform = null;
        }

        public void AddExternalMovement(Vector2 movement)
        {
            externalMovement += movement;
        }

    }
}