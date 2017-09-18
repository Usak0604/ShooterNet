using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//SmoothFollow 스크립트를 사용하기 위한 네임스페이스
using UnityStandardAssets.Utility;

public class PlayerCtrl : MonoBehaviour {
    private Transform tr;
    //네트워크 컴포넌트 변수
    private NetworkView _networkView;
    
    //위치 정보를 송수신하기 위한 변수
    private Vector3 currPos = Vector3.zero;
    private Quaternion currRot = Quaternion.identity;

    public GameObject bullet;//총알 프리맵
    public Transform firePos;//발사위치

    private bool isDie = false;
    private int hp = 100;
    private float respawnTime = 3.0f;

    public enum AnimState //네트워크로 애니메이션을 동기화하기 위해 애니메이션 상태 변수를 만든다.
    {
        idle = 0,
        runForward,
        runBackward,
        runRight,
        runLeft
    }

    public AnimState animState = AnimState.idle;
    public AnimationClip[] animClips;

    //캐릭터 컨트롤러 변수
    private CharacterController controller;
    private Animation anim;

    void Awake()
    {
        tr = GetComponent<Transform>();
        _networkView = GetComponent<NetworkView>();

        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animation>();

        if (_networkView.isMine)
        {
            //카메라의 추적 대상 설정. 그러나 SmoothFollow 스크립트의 target변수의 경우 private설정이 되어있기 때문에 스크립트를 열어 public으로 수정해야 이 코드가 작동한다.
            Camera.main.GetComponent<SmoothFollow>().target = tr;
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (_networkView.isMine) {
            if (Input.GetMouseButtonDown(0))
            {
                if (isDie) return; // 죽었을 경우 로직을 빠져나간다.
                Fire();
                _networkView.RPC("Fire", RPCMode.Others); // 본인이외의 모든 사용자의 fire을 호출한다.
            }

            //캐릭터 컨트롤러의 속도를 로컬 벡터로 변경
            Vector3 localVelocity = tr.InverseTransformDirection(controller.velocity);
            if(localVelocity.z >= 0.1f)//벡터의 성분의 크기에 따라 애니메이션 상태 변경
            {
                animState = AnimState.runForward;
            }
            else if (localVelocity.z <= -0.1f)
            {
                animState = AnimState.runBackward;
            }
            else if (localVelocity.x >= 0.1f)
            {
                animState = AnimState.runRight;
            }
            else if (localVelocity.x <= -0.1f)
            {
                animState = AnimState.runLeft;
            }
            else
            {
                animState = AnimState.idle;
            }

            //애니메이션 실행
            anim.CrossFade(animClips[(int)animState].name, 0.2f);

        }//내 자신이라면 따로 업데이트 하지 않아도 된다. MoveCtrl 스크립트에서 해주므로
        else
        {
            if(Vector3.Distance(tr.position, currPos) >= 2.0f) //리스폰시 거리가 멀어지면 Lerp를 해서 부드럽게 움직이지않고 순간이동시킨다.
            {
                tr.position = currPos;
                tr.rotation = currRot;
            }

            else
            {
                //전송 받은 위치로 부드럽게 보간
                tr.position = Vector3.Lerp(tr.position, currPos, Time.deltaTime * 10.0f);
                //각도로 부드럽게 보간
                tr.rotation = Quaternion.Slerp(tr.rotation, currRot, Time.deltaTime * 10.0f);
            }

            anim.CrossFade(animClips[(int)animState].name, 0.1f);
        }
	}

    [RPC] //RPC 함수를 사용하기 위해서는 반드시 명시해줘야한다.
    void Fire()
    {
        GameObject.Instantiate(bullet, firePos.position, firePos.rotation);
    }

    //이 함수는 NetworkView 컴포넌트에서 호출해주는 콜백함수 이다. 네트워크 뷰 컴포넌트의 Observed하는 컴포넌트가 지금 이 PlayerCtrl 스크립트이므로 이 함수는 반드시 작성해야한다.
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        //위치 외전 정보 송신
        if (stream.isWriting)
        {
            Vector3 pos = tr.position;
            Quaternion rot = tr.rotation;
            int _animState = (int)animState;

            //송신
            stream.Serialize(ref pos);
            stream.Serialize(ref rot);
            stream.Serialize(ref _animState);
        }
        else //정보 수신
        {
            //수신 받을 변수를 초기화한다.
            Vector3 revPos = Vector3.zero;
            Quaternion revRot = Quaternion.identity;
            int _animState = 0;

            stream.Serialize(ref revPos);
            stream.Serialize(ref revRot);
            stream.Serialize(ref _animState);

            currPos = revPos;
            currRot = revRot; // 수신받은 데이터를 업데이트
            animState = (AnimState)_animState;
        }
    }

    void OnTriggerEnter(Collider coll)
    {
        if(coll.gameObject.tag == "BULLET")
        {
            Destroy(coll.gameObject);
            hp -= 20;

            if(hp <= 0)
            {
                StartCoroutine(this.RespawnPlayer(respawnTime));
            }
        }
    }

    IEnumerator RespawnPlayer(float waitTime)
    {
        isDie = true;

        //플레이어를 투명하게 처리하는 코루틴함수
        StartCoroutine(this.PlayerVisible(false, 0.0f));

        yield return new WaitForSeconds(waitTime);

        tr.position = new Vector3(Random.Range(-20.0f, 20.0f), 0.0f, Random.Range(-20.0f, 20.0f));

        hp = 100;

        isDie = false;
        //플레이어를 리스폰 시켜줌
        StartCoroutine(this.PlayerVisible(true, 0.5f));
    }

    //플레이어를 보이게 or 보이지 않게 하는 함수
    IEnumerator PlayerVisible(bool visibled, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        //플레이어 몸의 Mash 투명화
        GetComponentInChildren<SkinnedMeshRenderer>().enabled = visibled;
        //무기의 Maah 투명화
        GetComponentInChildren<MeshRenderer>().enabled = visibled;

        if (_networkView.isMine) // 나 자신일 경우 입력에 반응 하지 못하도록함
        {
            GetComponent<MoveCtrl>().enabled = visibled;
            controller.enabled = visibled;
        }
    }
}
