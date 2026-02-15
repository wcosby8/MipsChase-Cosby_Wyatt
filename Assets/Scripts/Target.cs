using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Target : MonoBehaviour
{
   public Player m_player;
   public enum eState : int
   {
       kIdle,
       kHopStart,
       kHop,
       kCaught,
       kNumStates
   }


   private Color[] stateColors = new Color[(int)eState.kNumStates]
  {
       new Color(255, 0,   0),
       new Color(0,   255, 0),
       new Color(0,   0,   255),
       new Color(255, 255, 255)
  };


   // External tunables.
   public float m_fHopTime = 0.2f;
   public float m_fHopSpeed = 6.5f;
   public float m_fScaredDistance = 3.0f;
   public int m_nMaxMoveAttempts = 50;


   // Internal variables.
   public eState m_nState;
   public float m_fHopStart;
   public Vector3 m_vHopStartPos;
   public Vector3 m_vHopEndPos;


   public float m_fHopDistance = 1.5f;
   public float m_fScreenMargin = 0.5f;


   void Start(){
       m_nState=eState.kIdle;
       m_player=GameObject.FindObjectOfType(typeof(Player)) as Player;
   }


   void GetScreenBounds(out Vector2 min,out Vector2 max){
       min = Vector2.zero;
       max = Vector2.one * 10f;
       if (Camera.main == null) return;
       Camera cam = Camera.main;
       min = cam.ViewportToWorldPoint(new Vector3(0f,0f,-cam.transform.position.z));
       max = cam.ViewportToWorldPoint(new Vector3(1f, 1f,-cam.transform.position.z));
   }


   bool IsPositionOnScreen(Vector3 pos){
       GetScreenBounds(out Vector2 min, out Vector2 max);
       return (pos.x >= min.x + m_fScreenMargin) && (pos.x <= max.x - m_fScreenMargin) && (pos.y >= min.y + m_fScreenMargin) && (pos.y <= max.y - m_fScreenMargin);
   }


   Vector3 ChooseHopDirection(){
       if (m_player == null){
           return Vector3.right;
       }


       Vector3 away = (transform.position - m_player.transform.position);
       away.z = 0f;


       if (away.sqrMagnitude < 0.001f){
           away = Vector3.right;
       }
       away.Normalize();


       float hopDist = m_fHopDistance > 0f ? m_fHopDistance : m_fHopSpeed * m_fHopTime;
       Vector3 start = transform.position;


       for (int i = 0; i < m_nMaxMoveAttempts; i++){
           float angleRad = (i / (float)m_nMaxMoveAttempts) * 2f * Mathf.PI;
           float c = Mathf.Cos(angleRad);
           float s = Mathf.Sin(angleRad);
           Vector3 dir = new Vector3(away.x * c - away.y * s, away.x * s + away.y * c, 0f);
           Vector3 candidate = start + dir * hopDist;
           if (IsPositionOnScreen(candidate)){
               return dir;
           }
       }
       return away;
   }


   void Update(){
       if(m_nState==eState.kCaught){
           return;
       }
       if(m_nState == eState.kIdle){
           if(m_player != null && !m_player.IsDiving()){
               float dist = Vector3.Distance(transform.position,m_player.transform.position);
               if(dist < m_fScaredDistance){
                   m_vHopStartPos = transform.position;
                   Vector3 hopDir = ChooseHopDirection();
                   float hopDist = m_fHopDistance > 0f ? m_fHopDistance : m_fHopSpeed * m_fHopTime;
                   m_vHopEndPos = m_vHopStartPos + hopDir * hopDist;
                   m_fHopStart = Time.time;
                   m_nState = eState.kHop;
               }
           }
           return;
       }


       if (m_nState == eState.kHop){
           float elapsed = Time.time - m_fHopStart;
           float t = Mathf.Clamp01(elapsed / m_fHopTime);
           transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, t);
           if (t >= 1f){
               m_nState = eState.kIdle;
           }
       }
   }


   void FixedUpdate(){
       Renderer r = GetComponentInChildren<Renderer>();
       if (r != null) r.material.color = stateColors[(int)m_nState];
   }


   void OnTriggerStay2D(Collider2D collision){
       if (collision.gameObject == GameObject.Find("Player")){
           if (m_player != null && m_player.IsDiving()){
               m_nState = eState.kCaught;
               transform.parent = m_player.transform;
               transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
           }
       }
   }
}

