using UnityEngine;
using System.Collections;

public class BatUserController : MonoBehaviour {

    public BatCharacter batCharacter;
    public float upDownInputSpeed = 3f;


    void Start()
    {
        batCharacter = GetComponent<BatCharacter>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            batCharacter.Soar();
        }

        

        if (Input.GetKeyDown(KeyCode.L))
        {
            batCharacter.Landing();
        }

        
        if (Input.GetKeyDown(KeyCode.E))
        {
            batCharacter.EatStart();
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            batCharacter.EatEnd();
        }

        if (Input.GetKey(KeyCode.N))
        {
            batCharacter.upDown = Mathf.Clamp(batCharacter.upDown - Time.deltaTime * upDownInputSpeed, -1f, 1f);
        }
        if (Input.GetKey(KeyCode.U))
        {
            batCharacter.upDown = Mathf.Clamp(batCharacter.upDown + Time.deltaTime * upDownInputSpeed, -1f, 1f);
        }
    }

    void FixedUpdate()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        batCharacter.forwardAcceleration = v;
        batCharacter.yawVelocity = h;

    }
}
