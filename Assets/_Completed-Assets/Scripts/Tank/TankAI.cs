using UnityEngine;
using System.Collections.Generic;

namespace Complete
{
    public class TankAI : MonoBehaviour
    {
        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        
        public float shootingRange = 25f;
        
        private float fireTimer = 0f;
        private float stateTimer = 0f;
        private int currentState = 0; // 0: tracking, 1: charging, 2: cooldown

        // Anti-stuck variables
        private Vector3 m_LastStuckCheckPos;
        private float m_StuckCheckTimer = 0f;
        private float m_UnstuckTimer = 0f;
        private float m_UnstuckDir = 1f;

        private void Awake()
        {
            m_Movement = GetComponent<TankMovement>();
            m_Shooting = GetComponent<TankShooting>();
        }

        private void OnEnable()
        {
            if (m_Movement != null) m_Movement.m_IsBot = true;
            if (m_Shooting != null) m_Shooting.m_IsBot = true;
            currentState = 0;
            m_LastStuckCheckPos = transform.position;
            m_StuckCheckTimer = 0f;
            m_UnstuckTimer = 0f;
        }

        private void Update()
        {
            Transform target = GetNearestTarget();
            if (target == null)
            {
                if (m_Movement != null) m_Movement.SetBotInput(0f, 0f);
                if (m_Shooting != null) m_Shooting.m_BotFire = false;
                return;
            }

            Vector3 directionToTarget = target.position - transform.position;
            float distance = directionToTarget.magnitude;

            // Handle unstuck routine if active
            if (m_UnstuckTimer > 0f)
            {
                m_UnstuckTimer -= Time.deltaTime;
                m_Movement.SetBotInput(-0.85f, m_UnstuckDir);
                m_Shooting.m_BotFire = false;
                return;
            }

            // Movement logic
            float moveInput = 0f;
            if (distance > 5f) {
                moveInput = 1f;
            } else if (distance < 2.5f) {
                moveInput = -0.7f;
            } else {
                moveInput = 0.25f; // keep tactical distance
            }

            // Turning logic
            float turnInput = 0f;
            Vector3 forward = transform.forward;
            Vector3 targetDir = directionToTarget.normalized;
            float angle = Vector3.SignedAngle(forward, targetDir, Vector3.up);

            // Multi-ray Obstacle Avoidance
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            RaycastHit hitFwd, hitLeft, hitRight;

            bool blockedFwd = Physics.Raycast(rayOrigin, forward, out hitFwd, 4.5f) && hitFwd.transform != target;
            bool blockedLeft = Physics.Raycast(rayOrigin, Quaternion.Euler(0f, -32f, 0f) * forward, out hitLeft, 3.5f) && hitLeft.transform != target;
            bool blockedRight = Physics.Raycast(rayOrigin, Quaternion.Euler(0f, 32f, 0f) * forward, out hitRight, 3.5f) && hitRight.transform != target;

            if (blockedFwd)
            {
                if (hitFwd.distance < 2.2f)
                {
                    // Too close: reverse and steer away from the obstacle
                    moveInput = -0.75f;
                    turnInput = blockedLeft ? 1f : -1f;
                }
                else
                {
                    // Approaching obstacle: slow down and steer around it
                    moveInput = 0.35f;
                    turnInput = blockedLeft ? 0.75f : -0.75f;
                }
            }
            else if (blockedLeft)
            {
                turnInput = 0.55f; // gently steer right
            }
            else if (blockedRight)
            {
                turnInput = -0.55f; // gently steer left
            }
            else
            {
                // Smooth proportional turning towards target with deadband
                if (Mathf.Abs(angle) > 5f)
                {
                    turnInput = Mathf.Clamp(angle / 35f, -1f, 1f);
                }
                else
                {
                    turnInput = 0f; // on target, don't jitter
                }
            }

            // Check if tank is physically stuck against a wall
            m_StuckCheckTimer += Time.deltaTime;
            if (m_StuckCheckTimer >= 1.0f)
            {
                m_StuckCheckTimer = 0f;
                if (Mathf.Abs(moveInput) > 0.2f && Vector3.Distance(transform.position, m_LastStuckCheckPos) < 0.35f)
                {
                    // Engage unstuck maneuver: back up and turn
                    m_UnstuckTimer = 0.85f;
                    m_UnstuckDir = Random.value > 0.5f ? 1f : -1f;
                }
                m_LastStuckCheckPos = transform.position;
            }

            m_Movement.SetBotInput(moveInput, turnInput);

            // Shooting logic
            bool fire = false;

            if (Mathf.Abs(angle) < 18f && distance < shootingRange)
            {
                if (currentState == 0)
                {
                    currentState = 1;
                    stateTimer = Random.Range(0.25f, 0.65f); // charge time
                }
                else if (currentState == 1)
                {
                    fire = true;
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) {
                        currentState = 2;
                        fireTimer = Random.Range(1.2f, 2.4f); // cooldown
                    }
                }
            }
            else
            {
                if (currentState == 1) {
                    currentState = 2;
                    fireTimer = Random.Range(1f, 2f);
                }
            }

            if (currentState == 2)
            {
                fireTimer -= Time.deltaTime;
                if (fireTimer <= 0f) {
                    currentState = 0;
                }
            }

            m_Shooting.m_BotFire = fire;
        }

        private Transform GetNearestTarget()
        {
            GameManager manager = FindObjectOfType<GameManager>();
            if (manager == null || manager.m_Tanks == null || manager.m_Tanks.Length == 0) return null;

            // Target the human player (Player 1) if they are alive
            TankManager playerTank = manager.m_Tanks[0];
            if (playerTank != null && playerTank.m_Instance != null && playerTank.m_Instance.activeSelf && playerTank.m_Instance != gameObject)
            {
                return playerTank.m_Instance.transform;
            }

            // Fallback: if player is dead, find nearest alive opponent
            Transform bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            for (int i = 0; i < manager.m_Tanks.Length; i++)
            {
                TankManager tm = manager.m_Tanks[i];
                if (tm != null && tm.m_Instance != null && tm.m_Instance.activeSelf && tm.m_Instance != gameObject)
                {
                    Vector3 directionToTarget = tm.m_Instance.transform.position - currentPosition;
                    float dSqrToTarget = directionToTarget.sqrMagnitude;
                    if (dSqrToTarget < closestDistanceSqr)
                    {
                        closestDistanceSqr = dSqrToTarget;
                        bestTarget = tm.m_Instance.transform;
                    }
                }
            }
            
            return bestTarget;
        }
    }
}
