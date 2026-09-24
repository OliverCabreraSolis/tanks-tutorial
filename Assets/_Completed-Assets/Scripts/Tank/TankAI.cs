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
        }

        private void Update()
        {
            Transform target = GetNearestTarget();
            if (target == null)
            {
                m_Movement.SetBotInput(0f, 0f);
                m_Shooting.m_BotFire = false;
                return;
            }

            Vector3 directionToTarget = target.position - transform.position;
            float distance = directionToTarget.magnitude;
            
            // Movement logic
            float moveInput = 0f;
            if (distance > 4f) {
                moveInput = 1f;
            } else if (distance < 2f) {
                moveInput = -1f;
            }
            
            // Turning logic
            float turnInput = 0f;
            Vector3 forward = transform.forward;
            Vector3 targetDir = directionToTarget.normalized;
            float angle = Vector3.SignedAngle(forward, targetDir, Vector3.up);

            // Obstacle Avoidance
            bool blocked = false;
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, 6f))
            {
                if (hit.transform != target) 
                {
                    blocked = true;
                }
            }

            if (blocked)
            {
                moveInput = 1f; // Force forward
                turnInput = 1f; // Force turn right to avoid obstacle
            }
            else
            {
                if (angle > 2f) turnInput = 1f;
                else if (angle < -2f) turnInput = -1f;
            }
            
            m_Movement.SetBotInput(moveInput, turnInput);

            // Shooting logic
            bool fire = false;
            
            if (Mathf.Abs(angle) < 20f && distance < shootingRange)
            {
                if (currentState == 0)
                {
                    currentState = 1;
                    stateTimer = Random.Range(0.2f, 0.7f); // charge time
                }
                else if (currentState == 1)
                {
                    fire = true;
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) {
                        currentState = 2;
                        fireTimer = Random.Range(1f, 2.5f); // cooldown
                    }
                }
            }
            else
            {
                if (currentState == 1) {
                    currentState = 2;
                    fireTimer = Random.Range(1f, 2.5f);
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
            if (manager == null || manager.m_Tanks.Length == 0) return null;

            // Target the human player (Player 1) if they are alive
            TankManager playerTank = manager.m_Tanks[0];
            if (playerTank.m_Instance != null && playerTank.m_Instance.activeSelf)
            {
                return playerTank.m_Instance.transform;
            }

            // Fallback: if player is dead, find nearest target just to keep doing something
            Transform bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            foreach (TankManager tm in manager.m_Tanks)
            {
                if (tm.m_Instance != null && tm.m_Instance.activeSelf && tm.m_Instance != gameObject)
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
