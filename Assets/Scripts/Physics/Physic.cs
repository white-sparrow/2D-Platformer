﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

abstract public class Physic : MonoBehaviour
{
    public Action HitActionEffects;
    public Action HitAction;

    [SerializeField]
    private float weight;

    private float friction;
    protected Vector2 force;
    protected Vector2 speed;
    protected Vector2 impactForce;
    protected Vector2 systemSpeed;
    protected Vector2 size;
    protected Vector2 distance;
    protected List<RaycastHit2D> h_raycast_list_;
    protected List<RaycastHit2D> v_raycast_list_;
    protected List<ImpactProperty> impactProperties;
    protected List<GameObject> impacted_objects_;


    protected Vector2[] raycastPointsX;
    protected Vector2[] raycastPointsY;
    protected BoxCollider2D collider2d;

    protected int layerMask;
    protected Vector2 impactedSides;
    protected int self_layer_mask_;
    protected RaycastHit2D hitPoint;

    //Save
    protected Vector2 savedPostion;
    protected Vector2 savedForce;

    private void Awake()
    {
        GameManager.SaveSceneAction += Save;
        GameManager.LoadSceneAction += Load;
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        Function();
    }
    protected virtual void Init()
    {
        self_layer_mask_ = gameObject.layer;
        collider2d = GetComponent<BoxCollider2D>();
        size = transform.localScale * collider2d.size;
        layerMask += LayerMask.GetMask("Block" , "Enemy");
        CalculateRayCastPoints();
        impactProperties = new List<ImpactProperty>();
        impacted_objects_ = new List<GameObject>();
        v_raycast_list_ = new List<RaycastHit2D>();
        h_raycast_list_ = new List<RaycastHit2D>();
    }
    protected virtual void Function()
    {
        gameObject.layer = LayerMask.NameToLayer("Void");
        CapGravitySpeed();
        CalculateMovment();
        CalculateHit();
        HitActionFunction();
        ResetCalculate();
        gameObject.layer = self_layer_mask_;
    }
    private void CalculateRayCastPoints()
    {
        // calculate horizontal raycast point
        float cut = GameManager.MinSize;
        raycastPointsX = new Vector2[Mathf.CeilToInt(size.y / cut) + 1];
        raycastPointsX[0] = new Vector2(size.x / 2 , (-size.y / 2) + 0.01f);
        raycastPointsX[raycastPointsX.Length - 1] = new Vector2(size.x / 2 , (size.y / 2) - 0.01f);
        for (int i = 1 ; i < raycastPointsX.Length - 1 ; i++)
        {
            raycastPointsX[i] = raycastPointsX[i - 1] + Vector2.up * cut;
        }
        // calculate vertical raycast point
        raycastPointsY = new Vector2[Mathf.CeilToInt(size.x / cut) + 1];
        raycastPointsY[0] = new Vector2((-size.x / 2) + 0.01f , size.y / 2);
        raycastPointsY[raycastPointsY.Length - 1] = new Vector2(size.x / 2 - 0.01f , size.y / 2);
        for (int i = 1 ; i < raycastPointsY.Length - 1 ; i++)
        {
            raycastPointsY[i] = raycastPointsY[i - 1] + Vector2.right * cut;
        }
    }
    //Protected Functions
    protected virtual void CalculateMovment()
    {
        h_raycast_list_.Clear();
        v_raycast_list_.Clear();
        impactProperties.Clear();
        impacted_objects_.Clear();
        distance = ((force + speed) / weight) * Time.deltaTime;
        if (distance.x > 0)
        {
            MovementCheckRight();
        }
        else if (distance.x < 0)
        {
            MovementCheckLeft();
        }
        if (distance.y > 0)
        {
            MovementCheckUp();
        }
        else if (distance.y < 0)
        {
            MovementCheckDown();
        }
    }
    protected virtual void MovementCheckRight()
    {
        float leastDistance = distance.x;
        for (int i = 0 ; i < raycastPointsX.Length ; i++)
        {
            hitPoint = Physics2D.Raycast((Vector2)transform.position + raycastPointsX[i] , Vector2.right , leastDistance , layerMask , 0 , 0);
            if (hitPoint.collider != null && hitPoint.distance <= leastDistance)
            {
                impactedSides.x = 1;
                leastDistance = hitPoint.distance;
                h_raycast_list_.Add(hitPoint);
            }
        }
        h_raycast_list_.RemoveAll(delegate (RaycastHit2D ray)
        {
            return ray.distance > leastDistance;
        });
        UpdateImpactProperties(h_raycast_list_ , Vector2.right);
        ApplyMovement(Vector2.right * leastDistance);
    }
    protected virtual void MovementCheckLeft()
    {
        float leastDistance = -distance.x;
        for (int i = 0 ; i < raycastPointsX.Length ; i++)
        {
            hitPoint = Physics2D.Raycast((Vector2)transform.position - raycastPointsX[i] , Vector2.left , leastDistance , layerMask , 0 , 0);
            if (hitPoint.collider != null && !hitPoint.collider.Equals(collider2d) && hitPoint.distance <= leastDistance)
            {
                impactedSides.x = -1;
                leastDistance = hitPoint.distance;
                h_raycast_list_.Add(hitPoint);
            }
        }
        h_raycast_list_.RemoveAll(delegate (RaycastHit2D ray)
        {
            return ray.distance > leastDistance;
        });
        UpdateImpactProperties(h_raycast_list_ , Vector2.left);
        ApplyMovement(Vector2.left * leastDistance);
    }
    protected virtual void MovementCheckUp()
    {
        float leastDistance = distance.y;
        for (int i = 0 ; i < raycastPointsY.Length ; i++)
        {
            hitPoint = Physics2D.Raycast((Vector2)transform.position + raycastPointsY[i] , Vector2.up , leastDistance , layerMask , 0 , 0);
            if (hitPoint.collider != null && !hitPoint.collider.Equals(collider2d) && hitPoint.distance <= leastDistance)
            {
                impactedSides.y = 1;
                leastDistance = hitPoint.distance;
                v_raycast_list_.Add(hitPoint);
            }
        }
        v_raycast_list_.RemoveAll(delegate (RaycastHit2D ray)
        {
            return ray.distance > leastDistance;
        });
        UpdateImpactProperties(v_raycast_list_ , Vector2.up);
        ApplyMovement(Vector2.up * leastDistance);
    }
    protected virtual void MovementCheckDown()
    {
        float leastDistance = -distance.y;
        for (int i = 0 ; i < raycastPointsY.Length ; i++)
        {
            RaycastHit2D[] points = Physics2D.RaycastAll((Vector2)transform.position - raycastPointsY[i] , Vector2.down , leastDistance , layerMask , 0 , 0);
            foreach (RaycastHit2D hitPoint in points)
            {
                if (hitPoint.collider != null && !hitPoint.collider.Equals(collider2d) && hitPoint.distance <= leastDistance)
                {
                    impactedSides.y = -1;
                    leastDistance = hitPoint.distance;
                    v_raycast_list_.Add(hitPoint);
                }
            }
        }
        v_raycast_list_.RemoveAll(delegate (RaycastHit2D ray)
        {
            return ray.distance > leastDistance;
        });
        UpdateImpactProperties(v_raycast_list_ , Vector2.down);
        ApplyMovement(Vector2.down * leastDistance);
    }
    protected virtual void ApplyMovement(Vector2 distance)
    {
        transform.position += (Vector3)distance;
    }
    protected virtual void CalculateHit()
    {
        if (impactedSides.x != 0)
            force.x = 0;
        if (impactedSides.y != 0)
            force.y = 0;
    }
    protected virtual void ResetCalculate()
    {
        speed = Vector2.zero;
        impactForce = Vector2.zero;
        impactedSides = Vector2.zero;
        systemSpeed = Vector2.zero;
    }
    protected virtual void UpdateImpactProperties(List<RaycastHit2D> raycastList , Vector2 side)
    {
        foreach (RaycastHit2D hit in raycastList)
        {
            var impact_object = hit.collider.gameObject;
            if (!impacted_objects_.Contains(impact_object))
            {
                var impactEffects = impact_object.GetComponents<ImpactEffect>();
                foreach (ImpactEffect effect in impactEffects)
                {
                    impactProperties.Add(new ImpactProperty(effect , side));
                }
                impacted_objects_.Add(impact_object);
            }
        }
    }
    protected void CapGravitySpeed()
    {
        if (force.y < 0)
        {
            force.y = Mathf.Max(force.y , -(GameManager.instance.pMaxGravitySpeed) * weight);
        }
    }
    protected virtual void HitActionFunction()
    {
        if (impactedSides != Vector2.zero)
        {
            HitAction?.Invoke();
        }
        if (impactProperties.Count > 0)
        {
            HitActionEffects?.Invoke();
        }
    }
    protected virtual void Save()
    {
        savedPostion = transform.position;
        savedForce = force;
    }
    protected virtual void Load()
    {
        ResetForce();
        transform.position = savedPostion;
        force = savedForce;
    }
    //Public Functions
    public virtual void AddForce(Vector2 force)
    {
        this.force += force;
    }
    public virtual void AddSpeed(Vector2 speed)
    {
        this.speed += speed;
    }
    public virtual void AddImpactForce(Vector2 impactForce)
    {
        this.impactForce += impactForce;
    }
    public void ResetForce()
    {
        force = Vector2.zero;
    }
    //Public Get Attributes
    public Vector2 Force { get { return force; } }
    public Vector2 Speed { get { return speed; } }
    public Vector2 ImpactForce { get { return impactForce; } }
    public Vector2 Size { get { return size; } }
    public float Weight { get { return weight; } }
    public float Friction { get { return friction; } }
    public BoxCollider2D Collider2D { get { return collider2d; } }
    public List<ImpactProperty> ImpactProperties { get { return impactProperties; } }
    public Vector2 ImpactedSides { get { return impactedSides; } }
    public int Layer { get { return layerMask; } set { layerMask = value; } }
}



