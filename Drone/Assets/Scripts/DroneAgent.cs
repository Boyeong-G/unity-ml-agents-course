using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using PA_DronePack;

public class DroneAgent : Agent
{
    public DroneSetting area;
    public GameObject goal;
    public float DecisionWaitingTime = 5f;

    float preDist;
    float m_currentTime = 0f;

    private PA_DroneController dcoScript;
    private Transform agentTrans;
    private Transform goalTrans;
    private Rigidbody agent_Rigidbody;

    public override void Initialize()
    {
        dcoScript = gameObject.GetComponent<PA_DroneController>();
        agentTrans = gameObject.transform;
        goalTrans = goal.transform;
        agent_Rigidbody = gameObject.GetComponent<Rigidbody>();
        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 거리 벡터
        sensor.AddObservation(agentTrans.position - goalTrans.position);
        // 속도 벡터
        sensor.AddObservation(agent_Rigidbody.velocity);
        // 각속도 벡터
        sensor.AddObservation(agent_Rigidbody.angularVelocity);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.01f);
        var actions = actionBuffers.ContinuousActions;
        float moveX = Mathf.Clamp(actions[0], -1, 1f);
        float moveY = Mathf.Clamp(actions[1], -1, 1f);
        float moveZ = Mathf.Clamp(actions[2], -1, 1f);
        dcoScript.DriveInput(moveX);
        dcoScript.StrafeInput(moveY);
        dcoScript.LiftInput(moveZ);
        float distance = Vector3.Magnitude(goalTrans.position - agentTrans.position);

        if(distance <= 0.5f)
        {
            SetReward(1f);
            EndEpisode();
        }
        else if(distance > 10f)
        {
            SetReward(-1f);
            EndEpisode();
        }
        else
        {
            float reward = preDist - distance;
            // if (reward > 0f) reward += 0.9f;
            AddReward(reward);
            preDist = distance;
        }
    }

    public override void OnEpisodeBegin()
    {
        area.AreaSetting();
        preDist = Vector3.Magnitude(goalTrans.position - agentTrans.position);
    }

    public override void Heuristic(in ActionBuffers actionOut)
    {
        var actions = actionOut.ContinuousActions;
        actions[0] = Input.GetAxis("Horizontal");
        actions[1] = Input.GetAxis("Vertical");
        actions[2] = Input.GetAxis("Mouse ScrollWheel");
    }

    public void WaitTimeInference(int action)
    {
        if(Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if(m_currentTime >= DecisionWaitingTime)
            {
                m_currentTime = 0f;
                RequestDecision();
            }
            else{
                m_currentTime += Time.fixedDeltaTime;
            }
        }
    }
}
