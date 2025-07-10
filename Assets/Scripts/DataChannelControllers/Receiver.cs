using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using SimpleJSON;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using System;
using System.Linq;

public class Receiver : MonoBehaviour
{
    private WebSocket ws,ws_receiver;
    private string clientId;

    private RTCPeerConnection pc,pc_receiver;

    private List<AudioStreamTrack> audioStreamTrackList;
    private List<RTCRtpSender> sendingSenderList;

    private RTCDataChannel dataChannel;

    private DelegateOnIceConnectionChange pc1OnIceConnectionChange;
    private DelegateOnIceConnectionChange pc2OnIceConnectionChange;
    private DelegateOnIceCandidate pc1OnIceCandidate;

    private MediaStream _receiveStream;
    public AudioSource remoteAudioSource;
    //private DelegateOnNegotiationNeeded pcOnNegotiationNeeded;

    [SerializeField] private AudioSource audioObjectPrefab;
    //[SerializeField] private List<AudioClip> sourceAudioClips;
    private DelegateOnTrack pc2Ontrack;

    private readonly Queue<System.Action> executeOnMainThread = new Queue<System.Action>();

    private void Update()
    {
        lock (executeOnMainThread)
        {
            while (executeOnMainThread.Count > 0)
            {
                executeOnMainThread.Dequeue()?.Invoke();
            }
        }
    }

    private void Start()
    {
        if (remoteAudioSource == null)
        {
            remoteAudioSource = gameObject.AddComponent<AudioSource>();
            remoteAudioSource.playOnAwake = false;
        }
        pc2Ontrack = e =>
        {
            if (e.Track is AudioStreamTrack track)
            {
                //var outputAudioSource = receiveObjectList[audioIndex];
                var outputAudioSource = remoteAudioSource;
                outputAudioSource.SetTrack(track);
                outputAudioSource.loop = true;
                outputAudioSource.Play();
                //audioIndex++;
            }
        };
        StartCoroutine(WebRTC.Update());
        audioStreamTrackList = new List<AudioStreamTrack>();
        sendingSenderList = new List<RTCRtpSender>();
        pc1OnIceConnectionChange = state => { OnIceConnectionChange(pc, state); };
        pc1OnIceCandidate = candidate => { OnIceCandidate(pc, candidate); };
        InitClient("185.123.68.55",8300);
        InitReceiverClient("185.123.68.55",8300);
        StartCoroutine(InitWerbRTC());
    }
    void OnAddTrack(MediaStreamTrackEvent e)
    {
        var track = e.Track as AudioStreamTrack;
        remoteAudioSource.SetTrack(track);
        remoteAudioSource.loop = true;
        remoteAudioSource.Play();

    }

    private void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }

        if (pc != null)
        {
            pc.Close();
            pc = null;
        }
    }

    IEnumerator CreateOffer()
    {
        var offerOp = pc.CreateOffer();
        yield return offerOp;
        RTCSessionDescription offer = offerOp.Desc;
        
        var setLocalDescriptionOp = pc.SetLocalDescription(ref offer);
        yield return setLocalDescriptionOp;
        if (setLocalDescriptionOp.IsError)
        {
            Debug.LogError("SetLocalDescription error: " + setLocalDescriptionOp.Error.message);
            yield break;
        }
        //Debug.Log(pc.PendingLocalDescription.sdp);
        Debug.Log(pc.LocalDescription.sdp);
        Debug.Log(pc.SignalingState);
        var offerJson = new JSONObject();
        offerJson["type"] = "offer";
        offerJson["sdp"] = pc.LocalDescription.sdp;
        ws.Send(offerJson.ToString());
    }
    IEnumerator CreateAnswer(JSONObject data)
    {
        Debug.Log("creating Answers");
        var answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = data["sdp"]
        };
        var answerOP = pc.SetRemoteDescription(ref answer);
        yield return answerOP;
        if (answerOP.IsError)
        {
            Debug.LogError("SetRemoteDescription error: " + answerOP.Error.message);
            yield break;
        }
        Debug.Log("Remote description set successfully.");
        Debug.Log(pc.SignalingState);
        // Wait one frame to ensure RemoteDescription is set
        Debug.Log("Remote SDP: " + pc.RemoteDescription.sdp);
    }
    private void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.Log($"{nameof(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{nameof(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{nameof(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{nameof(pc)} IceConnectionState: Completed");
                Debug.Log(pc.LocalDescription.sdp);
                Debug.Log(pc.ConnectionState);
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{nameof(pc)} IceConnectionState: Connected");
                Debug.Log(pc.LocalDescription.sdp);
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{nameof(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{nameof(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{nameof(pc)} IceConnectionState: Max");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
    private void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
    {
        //GetOtherPc(pc).AddIceCandidate(candidate);
        pc.AddIceCandidate(candidate);
        Debug.Log($"{nameof(pc)} ICE candidate:\n {candidate.Candidate}");
    }
    float[] ConvertByteArrayToFloatArray(byte[] byteArray)
    {
        int floatCount = byteArray.Length / 2; // اگر هر نمونه 2 بایت است (پیش‌فرض PCM 16-bit)
        float[] floatArray = new float[floatCount];

        for (int i = 0; i < floatCount; i++)
        {
            // ترکیب دو بایت برای ساخت عدد صحیح 16 بیت
            short sample = BitConverter.ToInt16(byteArray, i * 2);
            // نرمال سازی بین -1 و 1
            floatArray[i] = sample / 32768f;
        }
        return floatArray;
    }
    public IEnumerator InitWerbRTC()
    {
        var config = new RTCConfiguration
        {
            iceServers = new RTCIceServer[]
            {
                new RTCIceServer
                {
                    urls = new string[] { "stun:185.123.68.55:3478" }
                },
                new RTCIceServer
                {
                    urls = new string[] { "turn:185.123.68.55:3478" },
                    username = "username1",
                    credential = "password1",
                    credentialType = RTCIceCredentialType.Password
                }
            }
        };
        _receiveStream = new MediaStream();
        _receiveStream.OnAddTrack += OnAddTrack;
        pc = new RTCPeerConnection(ref config);
        yield return pc;
        pc_receiver = new RTCPeerConnection(ref config)
        {
            OnTrack = e => _receiveStream.AddTrack(e.Track)
        };
        yield return pc_receiver;
        pc.OnIceConnectionChange = pc1OnIceConnectionChange;
        pc_receiver.OnIceConnectionChange = pc2OnIceConnectionChange;
        pc_receiver.OnTrack = e => _receiveStream.AddTrack(e.Track);

        //pc_receiver.OnTrack = pc2Ontrack;
        /*pc.OnIceCandidate = candidate =>
        {
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };
            ws.Send(candidateInit.ConvertToJson());
        };*/
        //pcOnIceConnectionChange = state => { OnIceConnectionChange(pc, state); };

        //pcOnNegotiationNeeded = () => { StartCoroutine(PeerNegotiationNeeded(_pc1)); };
        /*pc.OnIceCandidate = candidate =>
        {
            if (candidate == null) return;
            var candidateJson = new JSONObject();
            candidateJson["type"] = "candidate";
            candidateJson["candidate"] = candidate.Candidate;
            candidateJson["sdpMid"] = candidate.SdpMid;
            candidateJson["sdpMLineIndex"] = candidate.SdpMLineIndex ?? 0;
            ws.Send(candidateJson.ToString());
        };

        pc.OnIceConnectionChange = state =>
        {
            Debug.Log($"ICE Connection State: {state}");
        };*/

        pc.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                Debug.Log("ssssss");
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"DataChannel Message: {message}");

                float[] floatData = ConvertByteArrayToFloatArray(bytes);
                AudioClip clip = AudioClip.Create("RemoteAudio", floatData.Length, 1, 44100, false);
                clip.SetData(floatData, 0);
                remoteAudioSource.clip = clip;
                remoteAudioSource.Play();
                // Handle the received message
            };
            DelegateOnOpen onOpen = dataChannel.OnOpen;
            {
                Debug.Log("Salam");
            };
            dataChannel.OnOpen = onOpen;
        };
        pc_receiver.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                Debug.Log("ssssss");
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"DataChannel Message: {message}");

                float[] floatData = ConvertByteArrayToFloatArray(bytes);
                AudioClip clip = AudioClip.Create("RemoteAudio", floatData.Length, 1, 44100, false);
                clip.SetData(floatData, 0);
                remoteAudioSource.clip = clip;
                remoteAudioSource.Play();
                // Handle the received message
            };
            DelegateOnOpen onOpen = dataChannel.OnOpen;
            {
                Debug.Log("Salam");
            };
            dataChannel.OnOpen = onOpen;
        };
        var newSource = Instantiate(audioObjectPrefab, null, false);
        //newSource.name = $"SourceAudioObject{objectIndex}";
        newSource.loop = true;
        //newSource.clip = sourceAudioClips[0];
        string micDevice = Microphone.devices[0]; // Use the first available microphone
        newSource.clip = Microphone.Start(micDevice, true, 1, 44100);
        newSource.Play();
        audioStreamTrackList.Add(new AudioStreamTrack(newSource));
        AddTracks();
        yield return StartCoroutine(CreateOffer());
        Debug.Log(pc.LocalDescription.sdp);
    }
    private void AddTracks()
    {
        Debug.Log("Add not added tracks");
        /*foreach (var track in audioStreamTrackList.Where(x =>
            !sendingSenderList.Exists(y => y.Track.Id == x.Id)))
        {
            var sender = pc.AddTrack(track);
            sendingSenderList.Add(sender);
        }*/
        foreach(var track in audioStreamTrackList)
        {
            //if(sendingSenderList.Exists(y => y.Track.Id == track.Id))
            //{
                var sender = pc.AddTrack(track);
                sendingSenderList.Add(sender);
            //}
            //else
            //{
             //   pc_receiver.AddTrack(track);
            //}
        }
    }
    public void InitClient(string serverIp, int serverPort = 8080)
    {
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{serverPort}/ws/broadcast");
        /*ws.Connect();*/
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket connection closed.");
            ws = null;
        };
       
        ws.OnMessage += (sender, e) =>
        {
            var message = e.Data;
            Debug.Log($"Received message: {message}");
            
            JSONObject data = new JSONObject();
            data = JSON.Parse(message).AsObject;
            string type = data["type"];
            switch (type)
            {
                case "answer":
                    lock (executeOnMainThread)
                    {
                        executeOnMainThread.Enqueue(() =>
                        {
                            StartCoroutine(CreateAnswer(data));
                        });
                    }
                    break;

                case "candidate":
                    var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = data["candidate"],
                        sdpMid = data["sdpMid"],
                        sdpMLineIndex = data["sdpMLineIndex"].AsInt
                    });
                    pc.AddIceCandidate(candidate);
                    break;
            }
        };

        ws.Connect();
    }
    public void InitReceiverClient(string serverIp, int serverPort = 8080)
    {
        clientId = gameObject.name;

        ws_receiver = new WebSocket($"ws://{serverIp}:{serverPort}/ws/consumer");
        /*ws.Connect();*/
        ws_receiver.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket connection closed.");
            ws_receiver = null;
        };

        ws_receiver.OnMessage += (sender, e) =>
        {
            var message = e.Data;
            Debug.Log($"Received message: {message}");
            
            JSONObject data = new JSONObject();
            data = JSON.Parse(message).AsObject;
            string type = data["type"];
            switch (type)
            {
                case "answer":
                    lock (executeOnMainThread)
                    {
                        executeOnMainThread.Enqueue(() =>
                        {
                            StartCoroutine(CreateAnswer(data));
                        });
                    }
                    break;

                case "candidate":
                    var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = data["candidate"],
                        sdpMid = data["sdpMid"],
                        sdpMLineIndex = data["sdpMLineIndex"].AsInt
                    });
                    pc_receiver.AddIceCandidate(candidate);
                    break;

                case "new_track":
                    var transceiverInit = new RTCRtpTransceiverInit
                    {
                        direction = RTCRtpTransceiverDirection.RecvOnly,
                    };
                    var transceiver = pc_receiver.AddTransceiver(TrackKind.Audio, transceiverInit);

                    /*// اتصال رویداد دریافت داده‌های صوتی
                    transceiver.Receiver.Track.OnDataReceived += (audioData) =>
                    {
                        // فرض بر این است که audioData یک آرایه Byte است که می‌خواهید پخش کنید
                        // برای Playback، می‌تونید این داده‌ها رو تبدیل به فایل یا نمونه صوتی کنید
                        // در اینجا، نمونه ساده‌ای برای پلی کردن آسینک نیست و نیازمند پردازش است

                        // نمونه ساده: با فرض اینکه audioData قدیمی است و نیاز به تبدیل دارد
                        // در عمل، باید داده‌های صوتی را به فرمت مناسب تبدیل کنید
                        // مثلا: استفاده از AudioClip.Create و ثبت آن در AudioSource
                        // توجه کنید که این بخش نیاز به تبدیل و پردازش دارد، در ادامه نمونه کد:

                        // فرض بر این است که داده‌های صوتی در قالب مناسب است
                        // نمونه:
                        float[] floatData = ConvertByteArrayToFloatArray(audioData);
                        AudioClip clip = AudioClip.Create("RemoteAudio", floatData.Length, 1, 44100, false);
                        clip.SetData(floatData, 0);
                        remoteAudioSource.clip = clip;
                        remoteAudioSource.Play();
                    };*/

                    break;
            }
        };

        ws_receiver.Connect();
    }
}
