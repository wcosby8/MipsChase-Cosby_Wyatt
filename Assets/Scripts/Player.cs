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


   public enum eState : int{
       kMoveSlow,
       kMoveFast,
       kDiving,
       kRecovering,
       kNumStates
   }


   private Color[] stateColors = new Color[(int)eState.kNumStates]{
       new Color(0,     0,   0),
       new Color(255, 255, 255),
       new Color(0,     0, 255),
       new Color(0,   255,   0),
   };


   public bool IsDiving(){
       return (m_nState == eState.kDiving);
   }


   Vector3 GetMovementDirection(){
       float rad = m_fAngle * Mathf.Deg2Rad;
       return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
   }


   void CheckForDive(){
       if((Input.GetMouseButtonDown(0)) && (m_nState != eState.kDiving && m_nState != eState.kRecovering)){
           m_nState = eState.kDiving;
           m_fSpeed = 0.0f;
           m_vDiveStartPos = transform.position;
           m_vDiveEndPos = m_vDiveStartPos + GetMovementDirection() * m_fDiveDistance;
           m_fDiveStartTime = Time.time;
       }
   }


   void Start(){
       m_fAngle = 0;
       m_fSpeed = 0;
       m_nState = eState.kMoveSlow;
   }


   void UpdateDirectionAndSpeed(){
       if (Camera.main == null) return;
       float camDist = Mathf.Abs(Camera.main.transform.position.z);
       Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, camDist));
       Vector3 vScreenMax = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camDist));
       Vector2 vScreenSize = new Vector2(vScreenMax.x, vScreenMax.y);
       Vector2 vOffset = new Vector2(vScreenPos.x - transform.position.x, vScreenPos.y - transform.position.y);


       m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;


       float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;


       if(fMouseMagnitude > m_fMagnitudeFast){
           m_fTargetSpeed = m_fMaxSpeed;
       }
       else if(fMouseMagnitude > m_fMagnitudeSlow){
           m_fTargetSpeed = m_fSlowSpeed;
       }
       else{
           m_fTargetSpeed = 0.0f;
       }
   }


   void Update(){
       if(m_nState == eState.kDiving){
           float elapsed = (Time.time - m_fDiveStartTime);
           float t = Mathf.Clamp01(elapsed / m_fDiveTime);
           transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, t);
           if(t >= 1f){
               m_nState = eState.kRecovering;
               m_fDiveStartTime = Time.time;
           }
           return;
       }


       if(m_nState == eState.kRecovering){
           if((Time.time - m_fDiveStartTime) >= m_fDiveRecoveryTime){
               m_nState = eState.kMoveSlow;
           }
           return;
       }


       CheckForDive();
       UpdateDirectionAndSpeed();


       if(m_nState == eState.kMoveSlow){
           m_fAngle = m_fTargetAngle;
           m_fSpeed = m_fTargetSpeed;
           if(m_fSpeed > m_fSlowSpeed){
               m_nState = eState.kMoveFast;
           }
       }
       else if(m_nState == eState.kMoveFast){
           float angleDiff = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle);
           if(Mathf.Abs(angleDiff) <= m_fFastRotateMax){
               m_fAngle = m_fTargetAngle;
               m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed);
           }
           else{
               m_fSpeed = Mathf.MoveTowards(m_fSpeed, 0f, m_fIncSpeed);
           }
           if(m_fSpeed <= m_fSlowSpeed){
               m_nState = eState.kMoveSlow;
           }
       }


       transform.rotation = Quaternion.Euler(0f, 0f, m_fAngle + 90f);
       transform.position += GetMovementDirection() * m_fSpeed;
   }


   void FixedUpdate(){
       Renderer r = GetComponentInChildren<Renderer>();
       if (r != null) r.material.color = stateColors[(int)m_nState];
   }
}
