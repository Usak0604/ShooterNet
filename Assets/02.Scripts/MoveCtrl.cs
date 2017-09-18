using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCtrl : MonoBehaviour {
    private Transform tr;
    private CharacterController controller;

    private float h = 0.0f;
    private float v = 0.0f;

    public float movSpeed = 5.0f;
    public float rotSpeed = 50.0f;

    private Vector3 movDir = Vector3.zero;

	// Use this for initialization
	void Start () {
        //아래의 한줄이 없으면 네크워크를 통해 접속시 한명의 입력에 따라 나머지가 다 따라 움직인다. 똑같이 MoveCtrl 스크립트로 움직임을 제어하기 때문인데,
        //따라서 네트워크상의 본인 (.isMine)을 제외하고는 MoveCtrl스크립트(지금 클래스 this.enable)를 비활성화한다.
        this.enabled = GetComponent<NetworkView>().isMine;

        tr = GetComponent<Transform>();
        controller = GetComponent<CharacterController>();
	}
	
	// Update is called once per frame
	void Update () {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        tr.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotSpeed * Time.deltaTime);

        //이동 방향을 벡터 덧셈을 이용해 계산
        movDir = (tr.forward * v) + (tr.right * h);
        //중력을 영향을 받도록 y값을 지속적으로 떨어뜨림
        movDir.y -= 20f * Time.deltaTime;

        controller.Move(movDir * movSpeed * Time.deltaTime);
	}
}
