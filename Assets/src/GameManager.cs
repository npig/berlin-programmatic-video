using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Windows;
using Random = System.Random;

namespace Berlin
{
    public class GameManager : MonoBehaviour
    {
        private const string dataPath = "/berlinData.json";
        
        [SerializeField] private AudioClip _audioClip;
        [SerializeField] private RawImage _frame;
        [SerializeField] private Image _logo;
        [SerializeField] private TextMeshProUGUI _tmp;

        private TeamData _teamData;
        private VideoPlayer _videoPlayer;
        private AudioSource _audioSource;
        private AudioProcessor _audioProcessor;
        private Random _random;

        private TMP_ColorGradient[] _gradients;
        private TMP_FontAsset[] _englishFontAssets;
        private TMP_FontAsset[] _koreanFontAssets;
        private TMP_FontAsset[] _turkishFontAssets;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;  // VSync must be disabled
            Application.targetFrameRate = 115;
            
            _videoPlayer = gameObject.GetComponent<VideoPlayer>();
            _audioSource = gameObject.GetComponent<AudioSource>();
            _videoPlayer.renderMode = VideoRenderMode.APIOnly;
            _videoPlayer.prepareCompleted += OnPrepareCompleted;
            _videoPlayer.sendFrameReadyEvents = true;
            _videoPlayer.frameReady += OnFrameReady;
            _audioSource.clip = _audioClip;
            _audioProcessor = gameObject.AddComponent<AudioProcessor>();
            _audioProcessor.gThresh = 24f;
            _random = new Random();
            _englishFontAssets = Resources.LoadAll<TMP_FontAsset>("english/");
            _koreanFontAssets = Resources.LoadAll<TMP_FontAsset>("korean/");
            _turkishFontAssets = Resources.LoadAll<TMP_FontAsset>("turkish/"); 
            _gradients = Resources.LoadAll<TMP_ColorGradient>("gradients/"); 
            _audioProcessor.onBeat.AddListener(OnBeatDetected);
            _audioProcessor.onSpectrum.AddListener(OnSpectrum);
            
            //PrepareData
            string path = System.IO.File.ReadAllText(Application.streamingAssetsPath + dataPath);
            _teamData = JsonUtility.FromJson<TeamData>(path);
            
            _videoPlayer.Prepare();
        }

        private async UniTaskVoid UpdateTextColor()
        {
            while (true)
            {
                Color randomColor = UnityEngine.Random.ColorHSV();
                randomColor.a = 1;
                _tmp.color = randomColor;
                await UniTask.Delay(10);
            }
        }

        private bool _spawnSign = false;

        private void OnSpectrum(float[] spectrum)
        {
            //if (spectrum[0] * 1000 > .1)
            //{
                Vector3 rot = new Vector3(0,0,spectrum[0]);
                Vector3 scale = Vector3.one;
                int s = _spawnSign ? 1 : -1;
               _spawnSign = !_spawnSign;
               _logo.transform.localScale = Vector3.one + Vector3.one * (s * spectrum[0] * 10);
               //_logo.transform.rotation = Quaternion.Euler(i * rot * 100);
               //_frame.transform.localScale = scale * (spectrum[0] * 100);
               _frame.transform.localScale = Vector3.one + Vector3.one * ( spectrum[0] * 80);
            //}
           
            for (int i = 0; i < spectrum.Length; ++i)
            {
                Vector3 start = new Vector3(i, 0, 0);
                Vector3 end = new Vector3(i, spectrum[i] * 100, 0);
                Debug.DrawLine(start, end);
                
            }

            if (spectrum[1] * 1000 > 5)
            {
                //Color randomColor = UnityEngine.Random.ColorHSV();
                //randomColor.a = 1;
                //_tmp.color = randomColor;
            }
        }

        private void OnBeatDetected()
        {
            _videoPlayer.frame = _random.NextLong(0, (long) _videoPlayer.frameCount);
            _cancellationToken?.Cancel();
        }

        private void OnFrameReady(VideoPlayer source, long frameidx)
        {
            _frame.texture = source.texture;
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            _audioSource.Play();
            _ = PlaySequence();
            _ = UpdateTextColor();
        }

        private CancellationTokenSource _cancellationToken;

        private async UniTaskVoid PlaySequence()
        {
            foreach (Team team in _teamData.Teams)
            {
                _logo.enabled = true;
                _frame.enabled = false;
                _logo.sprite = Resources.Load<Sprite>($"sprites/{team.Logo}");
                SetTitle("");
                await PauseTitle(1000);
                _logo.enabled = false;
                _frame.enabled = true;

                foreach (Player player in team.Players)
                {
                    SetTitle(player.IGN);
                    Debug.Log(_tmp.font.name);
                    await PauseTitle(800);

                    if (!String.IsNullOrEmpty(player.Name))
                    {
                        Languages language = (Languages) Enum.Parse(typeof(Languages), player.Language, true);
                        SetTitle(player.Name, language);
                        Debug.Log(_tmp.font.name);
                        await PauseTitle(500);
                    }
                }
            }
        }

        private void SetTitle(string s)
        {
            _tmp.colorGradientPreset = _gradients[_random.Next(0, _gradients.Length - 1)];  
            _tmp.font = _englishFontAssets[_random.Next(0, _englishFontAssets.Length - 1)];
            _tmp.text = s;
        }

        private void SetTitle(string s, Languages language)
        {
            TMP_FontAsset font = _englishFontAssets[0];
            
            switch (language)
            {
                case Languages.ENGLISH:
                    font = _englishFontAssets[_random.Next(0, _englishFontAssets.Length - 1)];
                    break;
                case Languages.KOREAN:
                    font = _koreanFontAssets[_random.Next(0, _koreanFontAssets.Length - 1)];
                    break;
                case Languages.TURKISH:
                    font = _turkishFontAssets[_random.Next(0, _turkishFontAssets.Length - 1)];
                    break;
            }

            _tmp.colorGradientPreset = _gradients[_random.Next(0, _gradients.Length - 1)];  
            _tmp.font = font;
            _tmp.text = s;
        }

        private async UniTask PauseTitle(int m)
        {
            _cancellationToken = null;
            //realtime = 800
            await UniTask.Delay(800);
            _cancellationToken = new CancellationTokenSource();
            while (!_cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield();
            }
        }
    }
}