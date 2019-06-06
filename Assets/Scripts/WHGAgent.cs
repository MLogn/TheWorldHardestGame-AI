using System.Collections;
using System.Collections.Generic;
using MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class WHGAgent : Agent
{
    [Header("Agent Settings")]
    public float speed = 0.055f;
    [Range(1.0f, 120.0f)]
    public float timeToDie = 30.0f;
    public bool trainWithCamera;
    public bool debugMode;

    [Header("References")]
    public GameObject goalArea;
    public EnemyManager enemyManager;
    public Text progressText;
    public Text failCounterText;

    int failCounter;
    float lastProgress;
    float highestDist;
    float timeToLive;
    
    Vector3 startPosition;

    Vector2[] directions = {
            Vector2.left,
            Vector2.right,
            Vector2.up,
            Vector2.down,
            new Vector2(0.5f, 0.5f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, -0.5f),
            new Vector2(-0.5f, -0.5f)};

    LayerMask rayObservationMask;


    public override void InitializeAgent()
    {
        timeToLive = timeToDie;
        highestDist = GetProgress();

        startPosition = transform.position;

        rayObservationMask = ~(LayerMask.GetMask("Player") + LayerMask.GetMask("Enemy"));
    }

    public override void AgentReset()
    {
        highestDist = 10000.0f;
        timeToLive = Time.time + timeToDie;
        transform.position = startPosition;
    }

    public override void CollectObservations()
    {
        AddVectorObs(CollectRayInformation());
        AddVectorObs((transform.localPosition.x) / 14.0f);
        AddVectorObs((transform.localPosition.y) / 14.0f);
        AddVectorObs(Vector2.Distance(transform.localPosition, goalArea.transform.localPosition) / 14.0f);

        if(!trainWithCamera)
        {
            for (int i = 0; i < enemyManager.GetLength(); i++){
                AddVectorObs(Vector2.Distance(transform.position, enemyManager.GetPositionOfEnemy(i)) / 13.0f);
                AddVectorObs(Vector2.Angle(transform.position, enemyManager.GetPositionOfEnemy(i)) / 180f);
                AddVectorObs(enemyManager.GetSpeedAndDirectionOfEnemy(i));        
            }
        }
    }

    
    public float[] CollectRayInformation()
    {
        float[] observations = new float[directions.Length];

        for (int i = 0; i < directions.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directions[i], Mathf.Infinity, rayObservationMask);
            observations[i] = hit.distance;
            if(debugMode)
                Debug.DrawLine(transform.position, new Vector3(hit.point.x, hit.point.y, 0));
        }

        return observations;
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (timeToLive < Time.time)
        {
            failCounter++;
            AddReward(-1.0f);
            AgentReset();
        }
        
        AddReward(-0.01f/timeToDie);

        float HorizontalInput = 0.0f, VerticalInput = 0.0f;

        switch ((int)vectorAction[0])
        {
            case 1:
                VerticalInput = 1.0f;
                break;
            case 2:
                VerticalInput = -1.0f;            
                break;
            case 0:
                VerticalInput = 0.0f;
                break;
        }

        switch ((int)vectorAction[1])
        {
            case 1:
                HorizontalInput = -1.0f;               
                break;
            case 2:
                HorizontalInput = 1.0f;
                break;
            case 0:
                HorizontalInput = 0.0f;
                break;
        }

        
        float movementX = HorizontalInput * speed;
        float movementY = VerticalInput * speed;
        transform.position = transform.position + new Vector3(movementX, movementY, 0);
        
        UpdateProgress();
    }

    void UpdateProgress()
    {
        if (GetProgress() > lastProgress)
        {
            lastProgress = GetProgress();
        }
        else if (GetProgress() < highestDist)
        {
            highestDist = GetProgress();
            lastProgress = GetProgress();
        }
        else
        {
            lastProgress = GetProgress();
        }

        failCounterText.text = failCounter.ToString();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            AddReward(-1.0f);
            print("collision with enemy");
            AgentReset();
            failCounter++;
        }

        if (collision.gameObject.tag == "Goal")
        {
            print("collision with goal");
            failCounter = 0;
            AddReward(1.0f);
            Done();
            //AgentReset();
        }
    }

    public float GetProgress()
    {
        return Vector2.Distance(transform.position, goalArea.transform.position);
    }

}
