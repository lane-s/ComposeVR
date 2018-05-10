using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public enum InterpolationType { Linear, Exponential };

public class SnapToTargetPosition : MonoBehaviour
{

    public event EventHandler<EventArgs> TargetReached;
    public event EventHandler<EventArgs> MoveCancelled;

    public float closeEnoughDistance = 0.01f;

    [Tooltip("Useful when the snap position is constantly changing. If there was some time left over after the previous snap finished, the next snap will start from this time")]
    public bool carryTimeBetweenSnaps = false;

    public bool HasReachedTarget = false;
    public bool UseLocalPosition = false;

    private Vector3 targetPosition;
    private float speed;
    private InterpolationType interpolationType;

    private float t;
    private float startTime;
    private float finishTime;

    private float totalDistanceToTarget;
    private float totalMoveTime;

    private Vector3 startPosition;

    private bool fireEvent = false;
    private Rigidbody rb;

    void Awake()
    {
        targetPosition = transform.position;
        t = Mathf.Infinity;
        rb = GetComponent<Rigidbody>();
        interpolationType = InterpolationType.Exponential;
    }

    // Update is called once per frame
    void Update()
    {
        if (rb == null)
        {
            MoveToTarget();
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            MoveToTarget();
        }
    }

    private void MoveToTarget()
    {
        if (t <= 1)
        {
            Move();
        }

        if (t >= 1 && !HasReachedTarget)
        {
            finishTime = Time.time;
            HasReachedTarget = true;
            if (TargetReached != null)
            {
                TargetReached(this, new EventArgs());
            }
        }
    }

    private void Move()
    {
        float elapsedTime = Time.time - startTime;

        t = elapsedTime / totalMoveTime;

        if (interpolationType == InterpolationType.Exponential)
        {
            t = Mathf.Pow(t, 0.5f);
        }

        if (rb != null && !UseLocalPosition)
        {
            rb.MovePosition(Vector3.Lerp(startPosition, targetPosition, t));
        }
        else if (UseLocalPosition)
        {
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
        }
        else
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        }
    }

    public void SnapToTarget(Vector3 targetPosition, float speed, InterpolationType interpolationType)
    {
        this.targetPosition = targetPosition;
        this.speed = speed;
        this.interpolationType = interpolationType;

        if (UseLocalPosition)
        {
            startPosition = transform.localPosition;
        }
        else
        {
            startPosition = transform.position;
        }

        totalDistanceToTarget = Vector3.Distance(startPosition, targetPosition);

        if (totalDistanceToTarget < closeEnoughDistance)
        {
            t = 1;
            HasReachedTarget = true;
            Move();
            return;
        }
        else if (t <= 1)
        {
            Move();
        }

        t = 0;

        float lastStartTime = startTime;

        startTime = Time.time;

        totalMoveTime = totalDistanceToTarget / this.speed;
        HasReachedTarget = false;
    }

    public void SnapToTarget(Vector3 targetPosition, float speed)
    {
        SnapToTarget(targetPosition, speed, this.interpolationType);
    }

    public void SnapToTarget(Vector3 targetPosition)
    {
        SnapToTarget(targetPosition, this.speed, this.interpolationType);
    }

}
