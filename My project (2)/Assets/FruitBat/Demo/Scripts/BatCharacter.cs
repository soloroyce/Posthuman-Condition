using UnityEngine;
using System.Collections;

public class BatCharacter : MonoBehaviour {
    public Animator batAnimator;
    public float batSpeed = 1f;
    Rigidbody batRigid;
    public bool isFlying = false;
    public float upDown = 0f;
    public float forwardAcceleration = 0f;
    public float yawVelocity = 0f;


    public float forwardSpeed = 0f;
    public float maxForwardSpeed = 3f;
    public float meanForwardSpeed = 1.5f;
    public float speedDumpingTime = .1f;

    float soaringTime = 0f;
    public bool isLived = true;

    void Start()
    {
        batAnimator = GetComponent<Animator>();
        batAnimator.speed = batSpeed;
        batRigid = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Move();
        soaringTime = soaringTime + Time.deltaTime;

    }


    public void SpeedSet(float animSpeed)
    {
        batAnimator.speed = animSpeed;
    }



    public void Soar()
    {

            soaringTime = 0f;


            batAnimator.SetTrigger("Soar");
            batRigid.useGravity = false;

            forwardAcceleration = 0f;
            forwardSpeed = 0f;
            upDown = 2f;
            batAnimator.applyRootMotion = false;
            isFlying = true;

    }


    public void Landing()
    {
        batAnimator.SetTrigger("Landing");
        batAnimator.applyRootMotion = true;
    }


    public void EatStart()
    {
        batAnimator.SetBool("IsEating", true);
    }

    public void EatEnd()
    {
        batAnimator.SetBool("IsEating", false);
    }


    public void Move()
    {
        batAnimator.SetFloat("Forward", forwardAcceleration);
        batAnimator.SetFloat("Turn", yawVelocity);
        batAnimator.SetFloat("UpDown", upDown);


        if (isFlying)
        {
            if (soaringTime < 2f)
            {
                forwardSpeed = soaringTime * meanForwardSpeed;
                upDown = soaringTime;

            }

            if (forwardAcceleration < 0f)
            {
                batRigid.velocity = transform.up * upDown + transform.forward * forwardSpeed;
            }
            else
            {
                batRigid.velocity = transform.up * (upDown + (forwardSpeed - meanForwardSpeed)) + transform.forward * forwardSpeed;
            }
            transform.RotateAround(transform.position, Vector3.up, Time.deltaTime * yawVelocity * 100f);

            forwardSpeed = Mathf.Lerp(forwardSpeed, 0f, Time.deltaTime * speedDumpingTime);
            forwardSpeed = Mathf.Clamp(forwardSpeed + forwardAcceleration * Time.deltaTime, 0f, maxForwardSpeed);
            upDown = Mathf.Lerp(upDown, 0, Time.deltaTime * 3f);

        }
    }
}
