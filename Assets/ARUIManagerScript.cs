using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ARUIManagerScript : MonoBehaviour
{
    Camera _mainCam = null;

    /// <summary>
	/// 마우스의 상태
	/// </summary>
	private bool _mouseState;

    /// <summary>
    /// 마우스가 다운된 오브젝트
    /// </summary>
    private GameObject target;
    /// <summary>
    /// 마우스 좌표
    /// </summary>
    private Vector3 MousePos;

    [SerializeField]
    public float moveSpeed = 1.0f; //이동속도
    public float moveRot = 0.5f;

    private float gravity = -9.81f;
    private Transform tr = null;
    private Vector3 moveDirection; //이동방향

    public Text UIText;
    float distance = 10;
    public GameObject MenuPanel;

    public Transform parent;
    public GameObject Furniture;

    public void CategoryPush()
    {
        MenuPanel.SetActive(true);
    }

    public void CategoryReturn()
    {
        MenuPanel.SetActive(false);
    }


    public void CameraShotPush()
    {
        //사진촬영
    }

    public void ReturnButtonPush()
    {
        SceneManager.LoadScene("IntroScene");
    }

    void Awake()
    {
        _mainCam = Camera.main;
    }

    public void SelectFurniture()
    {
        Furniture.SetActive(true);
        MenuPanel.SetActive(false);
        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z - Furniture.transform.position.z);
        Vector3 objPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Furniture.transform.position = objPosition;
    }
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        //마우스가 내려갔는지?
        if (true == Input.GetMouseButtonDown(0))
        {
            Debug.Log("드래구중");
            //내려갔다.

            //타겟을 받아온다.
            target = GetClickedObject();

            //타겟이 있나?
            if (target != null)
            {
                //있으면 마우스 정보를 바꾼다.
                _mouseState = true;
            }

        }
        else if (true == Input.GetMouseButtonUp(0))
        {
            UIText.text = "가구를 이동하거나 배치해보세요!"; Debug.Log("드래그완료");
            //마우스가 올라 갔다.
            //마우스 정보를 바꾼다.
            _mouseState = false;
        }

        //마우스가 눌렸나?
        if (true == _mouseState)
        {
            //눌렸다!
            UIText.text = "가구를 원하는 곳에 배치해보세요!"; 


            //마우스 좌표를 받아온다.
           // MousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, target.transform.position.z));
           // 위에껀 한번에 해서글너가 안됏는데 밑에는 된다 신기하다
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y,  target.transform.position.z);
            Vector3 objPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            target.transform.position = objPosition;

            //타겟의 위치 변경
            //target.transform.position = new Vector3(MousePos.x, MousePos.y, target.transform.position.z);
        }
    }

    private GameObject GetClickedObject()
    {
        //충돌이 감지된 영역
        RaycastHit hit;
        //찾은 오브젝트
        GameObject target = null;

        //마우스 포이트 근처 좌표를 만든다.
        Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);

        //마우스 근처에 오브젝트가 있는지 확인
        if (true == (Physics.Raycast(ray.origin, ray.direction * 15, out hit)))
        {
            //있다!

            //있으면 오브젝트를 저장한다.
            target = hit.collider.gameObject;
            Debug.Log(target.name);
        }

        return target;
    }
}
