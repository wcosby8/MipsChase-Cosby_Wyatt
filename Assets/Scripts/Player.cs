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
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
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
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;
        m_fMouseMagnitude = fMouseMagnitude; // Store for Update() to use

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
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
        //handling the driving state 
        if(m_nState == eState.kDiving){
            //calculating the correct dive end position based on movement angle
            float diveAngle = m_fAngle + 180f;
            Vector3 diveDirection = new Vector3(Mathf.Cos(diveAngle * Mathf.Deg2Rad), Mathf.Sin(diveAngle * Mathf.Deg2Rad), 0);
            Vector3 correctedDiveEndPos = m_vDiveStartPos + diveDirection * m_fDiveDistance;
            
            float elapsed = Time.time - m_fDiveStartTime;
            float t = Mathf.Clamp01(elapsed / m_fDiveTime);
            transform.position = Vector3.Lerp(m_vDiveStartPos, correctedDiveEndPos, t);
            if(t >= 1.0f){
                m_nState = eState.kRecovering;
                m_fDiveStartTime = Time.time;
            }
            return;
        }

        //handling the recovery state, no movement possible
        if(m_nState == eState.kRecovering){
            if ((Time.time - m_fDiveStartTime) >= m_fDiveRecoveryTime){
               m_nState = eState.kMoveSlow;
           }
           return;
       }

        //checking for the dive input
        CheckForDive();

        //updating the direction and speed based on the mouse position
       UpdateDirectionAndSpeed();

        //handling the slow movement state
        if(m_nState == eState.kMoveSlow){
            //the angle can change immediately
           m_fAngle = m_fTargetAngle;
           m_fSpeed = m_fTargetSpeed;
            
            //transitioning to fast if the speed exceeds the threshold
            if (m_fSpeed > m_fSlowSpeed){
               m_nState = eState.kMoveFast;
           }
       }
        //handling the fast movement state
        else if(m_nState == eState.kMoveFast){
           float angleDiff = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle);
            // can turn if within small threshold
            if(Mathf.Abs(angleDiff) <= m_fFastRotateMax){
               m_fAngle = m_fTargetAngle;
               m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed);
           }
            else{
                //continuing in the original direction but slowing down
                m_fSpeed = Mathf.MoveTowards(m_fSpeed, 0.0f, m_fIncSpeed);
           }
            
            if(m_fMouseMagnitude <= m_fMagnitudeSlow){
               m_nState = eState.kMoveSlow;
           }
       }

        //applying the rotation and movement
        float movementAngle = m_fAngle + 180f;
        transform.rotation = Quaternion.Euler(0, 0, movementAngle + 90f);
        transform.position += new Vector3(Mathf.Cos(movementAngle * Mathf.Deg2Rad), Mathf.Sin(movementAngle * Mathf.Deg2Rad), 0) * m_fSpeed;
   }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
   }
}
