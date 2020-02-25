/*
*	Author(作者)：gzj
*	Description(描述)：
*	Version(版本):
*	LogicFlow(逻辑流程):
*	TODO:
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour {

    public float WalkSpeed;
    public Transform ldPos;
    public Transform ruPos;
    Vector2 input;

    private void Update()
    {
        input.x = Input.GetAxis("Horizontal");
        input.y = Input.GetAxis("Vertical");

        if(input.x != 0)
        input.x = input.x > 0 ? 1 : -1;

        if (input.y != 0)
            input.y = input.y > 0 ? 1 : -1;

        input *= WalkSpeed * Time.deltaTime;

        if (input.x != 0 || input.y != 0)
        {
            Vector3 newPos = transform.position + new Vector3(input.x, 0f, input.y);
            newPos.x = Mathf.Clamp(newPos.x, ldPos.position.x, ruPos.position.x);
            newPos.z = Mathf.Clamp(newPos.z, ldPos.position.z, ruPos.position.z);
            transform.position = newPos;
        }
    }
}