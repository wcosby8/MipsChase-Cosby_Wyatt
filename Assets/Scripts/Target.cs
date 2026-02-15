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
        new Color(1f, 0f,   0f),    // Red - Idle
        new Color(0f, 1f,  0f),    // Green - HopStart (not used)
        new Color(0f, 0f,  1f),    // Blue - Hop (evading)
        new Color(1f, 1f,  1f)     // White - Caught
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

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void Update()
    {
        // If caught, don't do anything
        if (m_nState == eState.kCaught)
        {
            return;
        }

        // If idle, check if player is close
        if (m_nState == eState.kIdle)
        {
            if (m_player != null && !m_player.IsDiving())
            {
                float dist = Vector3.Distance(transform.position, m_player.transform.position);
                if (dist < m_fScaredDistance)
                {
                    // Start hopping - find a direction away from player that stays on screen
                    m_vHopStartPos = transform.position;
                    
                    // Calculate direction away from player
                    Vector3 awayFromPlayer = (transform.position - m_player.transform.position).normalized;
                    if (awayFromPlayer.magnitude < 0.001f)
                    {
                        awayFromPlayer = Vector3.right;
                    }
                    
                    // Try to find a valid hop position that stays on screen
                    Vector3 hopDirection = awayFromPlayer;
                    float hopDistance = m_fHopSpeed * m_fHopTime;
                    Vector3 hopEndPos = m_vHopStartPos + hopDirection * hopDistance;
                    
                    // Check screen bounds
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 screenMin = cam.ViewportToWorldPoint(new Vector3(0, 0, -cam.transform.position.z));
                        Vector3 screenMax = cam.ViewportToWorldPoint(new Vector3(1, 1, -cam.transform.position.z));
                        
                        // Try different angles if initial direction goes off screen
                        bool foundValid = (hopEndPos.x >= screenMin.x && hopEndPos.x <= screenMax.x &&
                                         hopEndPos.y >= screenMin.y && hopEndPos.y <= screenMax.y);
                        
                        if (!foundValid)
                        {
                            // Try rotating the direction to find a valid position
                            for (int i = 0; i < m_nMaxMoveAttempts; i++)
                            {
                                float angle = (i / (float)m_nMaxMoveAttempts) * 360.0f * Mathf.Deg2Rad;
                                Vector3 testDir = new Vector3(
                                    awayFromPlayer.x * Mathf.Cos(angle) - awayFromPlayer.y * Mathf.Sin(angle),
                                    awayFromPlayer.x * Mathf.Sin(angle) + awayFromPlayer.y * Mathf.Cos(angle),
                                    0
                                );
                                Vector3 testPos = m_vHopStartPos + testDir * hopDistance;
                                
                                if (testPos.x >= screenMin.x && testPos.x <= screenMax.x &&
                                    testPos.y >= screenMin.y && testPos.y <= screenMax.y)
                                {
                                    hopDirection = testDir;
                                    hopEndPos = testPos;
                                    foundValid = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    m_vHopEndPos = hopEndPos;
                    m_fHopStart = Time.time;
                    m_nState = eState.kHop;
                }
            }
        }
        // If hopping, lerp to end position
        else if (m_nState == eState.kHop)
        {
            float elapsed = Time.time - m_fHopStart;
            float t = Mathf.Clamp01(elapsed / m_fHopTime);
            transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, t);
            
            if (t >= 1.0f)
            {
                m_nState = eState.kIdle;
            }
        }
    }

    void FixedUpdate()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = stateColors[(int)m_nState];
        }
        else
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null) r.material.color = stateColors[(int)m_nState];
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving() && m_nState != eState.kCaught)
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.6f, 0.6f, 0.0f);
                // Ensure the target stays visible
                gameObject.SetActive(true);
            }
        }
    }
}
