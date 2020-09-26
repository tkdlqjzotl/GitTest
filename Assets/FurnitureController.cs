using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FurnitureController : MonoBehaviour
{

    

    private void Awake()
    {
        // movement3D = GetComponent<Movement3D>();

        //characterController = GetComponent<CharacterController>();

    }


    // Start is called before the first frame update
    void Start()
    {
    }
    /*
    void OnMouseUp()
    {
        UIText.text = "가구를 이동하거나 배치해보세요!";
        Debug.Log("드래그완료");
    }

    void OnMouseDrag()
    {
        UIText.text = "가구를 원하는 곳에 배치해보세요!";
        Debug.Log("드래구중");
    }*/

    // Update is called once per frame
    void Update()
    {/*
        if (Input.GetMouseButton(0))
        {
            UIText.text = "가구를 원하는 곳에 배치해보세요!";
            Debug.Log("드래구중");
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z);
            Vector3 objPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            this.transform.position = objPosition;
            //this.transform.parent = null;
        }

        else if (Input.GetMouseButtonUp(0))
        {
            UIText.text = "가구를 이동하거나 배치해보세요!";
            Debug.Log("드래그완료");
            //this.transform.parent = parent;

            //moveDirection.y += gravity * Time.deltaTime;
            //characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        }*/





    }

    void OnMouseDrag()
    { 
    
    }
}
/*
 movement3D = GetComponent<Movement3D>();
 
 float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        movement3D.MoveTo(new Vector3(x, 0, z));
 
 
 */