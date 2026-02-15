using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;
    public float m_fMouseMagnitude;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            Vector3 diveDirection = new Vector3(Mathf.Cos(m_fAngle * Mathf.Deg2Rad), Mathf.Sin(m_fAngle * Mathf.Deg2Rad), 0);
            m_vDiveEndPos = m_vDiveStartPos + diveDirection * m_fDiveDistance;
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(vScreenPos.x - transform.position.x, vScreenPos.y - transform.position.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        m_fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (m_fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (m_fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void Update()
    {
        // Handle diving state
        if (m_nState == eState.kDiving)
        {
            float elapsed = Time.time - m_fDiveStartTime;
            float t = Mathf.Clamp01(elapsed / m_fDiveTime);
            transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, t);
            if (t >= 1.0f)
            {
                m_nState = eState.kRecovering;
                m_fDiveStartTime = Time.time;
            }
            return;
        }

        // Handle recovery state - no movement possible
        if (m_nState == eState.kRecovering)
        {
            if ((Time.time - m_fDiveStartTime) >= m_fDiveRecoveryTime)
            {
                m_nState = eState.kMoveSlow;
            }
            return;
        }

        // Check for dive input
        CheckForDive();
        
        // Update direction and speed based on mouse position
        UpdateDirectionAndSpeed();

        // Handle slow movement state
        if (m_nState == eState.kMoveSlow)
        {
            // Angle can change immediately
            m_fAngle = m_fTargetAngle;
            m_fSpeed = m_fTargetSpeed;
            
            // Transition to fast if speed exceeds threshold
            if (m_fSpeed > m_fSlowSpeed)
            {
                m_nState = eState.kMoveFast;
            }
        }
        // Handle fast movement state
        else if (m_nState == eState.kMoveFast)
        {
            float angleDiff = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle);
            
            // Can turn if within small threshold
            if (Mathf.Abs(angleDiff) <= m_fFastRotateMax)
            {
                m_fAngle = m_fTargetAngle;
                m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed);
            }
            else
            {
                // Continue in original direction but slow down
                m_fSpeed = Mathf.MoveTowards(m_fSpeed, 0.0f, m_fIncSpeed);
            }
            
            // Drop back to slow only when mouse is very close (caught up)
            if (m_fMouseMagnitude <= m_fMagnitudeSlow)
            {
                m_nState = eState.kMoveSlow;
            }
        }

        // Apply rotation and movement
        transform.rotation = Quaternion.Euler(0, 0, m_fAngle + 90f);
        transform.position += new Vector3(Mathf.Cos(m_fAngle * Mathf.Deg2Rad), Mathf.Sin(m_fAngle * Mathf.Deg2Rad), 0) * m_fSpeed;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }
}
