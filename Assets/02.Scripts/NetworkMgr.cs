using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMgr : MonoBehaviour {
    private const string ip = "127.0.0.1";
    private const int port = 30000;
    private bool _useNat = false;

    public GameObject player; // 플레이어의 프리맵

    //네트워크 사용시 프레임마다 호출되는 함수인 듯 싶다.
    void OnGUI()
    {
        //현재 사용자의 네트워크 접속 여부 판단
        if(Network.peerType == NetworkPeerType.Disconnected)
        {
            //서버 생성 버튼 만들기
            if(GUI.Button(new Rect(20,20,200,25), "Start Server"))
            {
                //서버 생성 (접속자수, 포트번호, NAT사용여부)
                Network.InitializeServer(20, port, _useNat);
            }
            if (GUI.Button(new Rect(20, 50, 200, 25), "Connect to Server"))
            {
                Network.Connect(ip, port);
            }
        }
        else
        {
            if(Network.peerType == NetworkPeerType.Server) //내가 서버일 경우
            {
                GUI.Label(new Rect(20, 20, 200, 25), "Initialization Server...");
                GUI.Label(new Rect(20, 50, 200, 25), "Client Count = " + Network.connections.Length.ToString());
            }
            if(Network.peerType == NetworkPeerType.Client) //클라이언트의 경우
            {
                GUI.Label(new Rect(20, 20, 200, 25), "Connected to Server");
            }
        }
    }

    //네트워크 접속시 플레이어를 그려주는, 생성해주는 함수
    void CreatePlayer()
    {
        //무작위 위치로 생성
        Vector3 pos = new Vector3(Random.Range(-20.0f, 20.0f), 0.0f, Random.Range(-20.0f, 20.0f));

        Network.Instantiate(player, pos, Quaternion.identity, 0); //네트워크 상에 플레이어 생성
    }

    //서버를 구동시키고 초기화가 정상적으로 완료되었을 때 호출됨
    void OnServerInitialized()
    {
        CreatePlayer();
    }

    //클라이언트 입장에서 서버에 접속했을 때 호출
    void OnConnectedToServer()
    {
        CreatePlayer();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //접속을 종료하거나 끊어졌을 때 호출되는 콜백함수이다.
    void OnPlayerDisconnected(NetworkPlayer netPlayer)
    {
        //접속이 종료된 플레이어를 제거한다.
        Network.RemoveRPCs(netPlayer);//네트워크 관련만 없앤다.
        //종료된 플레이어의 모든 객체를 제거한다.
        Network.DestroyPlayerObjects(netPlayer);
    }
}
