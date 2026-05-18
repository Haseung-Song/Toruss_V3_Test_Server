using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Toruss_V3_Test_Server.Common;
using Toruss_V3_Test_Server.Models;
using Toruss_V3_Test_Server.Services;
using static Toruss_V3_Test_Server.Models.Parser;

namespace Toruss_V3_Test_Server.ViewModels
{
    public class MainVM : INotifyPropertyChanged
    {
        #region [프로퍼티]

        private TcpService _tcpService;

        private string _ipAddress;
        private int _port;

        private ObservableCollection<DisplayInfo> _displayInfo;

        private bool _IsStartBtnEnabled;
        private bool _isSendTestRunning;

        private Task _sendTask;

        #endregion

        #region [OnPropertyChanged]

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// [InitStop_BtnText]
        /// </summary>
        public string InitStop_BtnText => IsStartBtnEnabled ? "Init Server" : "Stop Server";

        /// <summary>
        /// [IsStartBtnEnabled]
        /// </summary>
        public bool IsStartBtnEnabled
        {
            get => _IsStartBtnEnabled;
            private set
            {
                if (_IsStartBtnEnabled != value)
                {
                    _IsStartBtnEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(InitStop_BtnText));
                }

            }

        }

        /// <summary>
        /// [IpAddress]
        /// </summary>
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [Port]
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                }

            }

        }

        /// <summary>
        /// [DisplayInfo]
        /// </summary>
        public ObservableCollection<DisplayInfo> DisplayInfo
        {
            get => _displayInfo;
            set
            {
                if (_displayInfo != value)
                {
                    _displayInfo = value;
                    OnPropertyChanged();
                }

            }

        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region [ICommand]

        public ICommand InitAndStopServerCommand { get; set; }

        public ICommand SendClientAndTestCommand { get; set; }

        #endregion

        #region 생성자 (Initialize)

        public MainVM()
        {
            IsStartBtnEnabled = true;
            _ipAddress = IPAddress.Loopback.ToString();
            _port = 2000;
            _displayInfo = new ObservableCollection<DisplayInfo>();

            InitAndStopServerCommand = new RelayCommand(InitAndStop);
            SendClientAndTestCommand = new RelayCommand(SendAndTest);
        }

        #endregion

        #region [버튼 및 기능]

        private void InitAndStop()
        {
            if (_tcpService == null || IsStartBtnEnabled)
            {
                IsStartBtnEnabled = false;
                _tcpService = new TcpService(Port);
                _tcpService.MessageReceived += OnMessageReceived; // 이벤트 구독
                _tcpService.TcpStart();
                Console.WriteLine("TCP Server Started...");
            }
            else
            {
                IsStartBtnEnabled = true;
                _tcpService.MessageReceived -= OnMessageReceived; // 이벤트 해제
                _tcpService.TcpStop();
                Console.WriteLine("TCP Server Stopped...");
            }

        }

        /// <summary>
        /// [Server -> Client] [TEST]: Packet 송신
        /// </summary>
        private void SendAndTest()
        {
            try
            {
                // 이미 실행 중이라면, 중지하기
                if (_isSendTestRunning)
                {
                    _isSendTestRunning = false;

                    Console.WriteLine("[TCP TEST SEND] STOP");
                    return;
                }

                // TCP Server 실행 여부 확인
                if (_tcpService == null)
                {
                    MessageBox.Show(
                    "TCP Server를 먼저 시작해주세요.",
                    "TCP Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                    return;
                }

                _isSendTestRunning = true;

                Console.WriteLine("[TCP TEST SEND] START");

#if false // [HEX Packet] [TEST] 데이터
                byte[] packet =
                {
                    0x02,
                    0x88,
                    0x36,
                    0xBB,
                    0xAA,
                    0xD8,
                    0x10,
                    0x0E,
                    0x54,
                    0x2D,
                    0x50,
                    0x03
                };
#endif
                // 반복 송신 Task 시작
                _sendTask = Task.Run(async () =>
                {
                    Random random = new Random();

                    while (_isSendTestRunning)
                    {
                        // 랜덤 Packet 생성
                        byte[] packet = new byte[12];

                        // STX(시작 Data)
                        packet[0] = 0x02;

                        // Random Data
                        for (int i = 1; i < packet.Length - 1; i++)
                        {
                            packet[i] = (byte)random.Next(0, 256);
                        }

                        // ETX(종료 Data)
                        packet[packet.Length - 1] = 0x03;

                        // [Client 측]으로 Packet 송신
                        bool result = _tcpService.SendToClient(packet);

                        if (!result)
                        {
                            // 반복 송신 종료 상태 변경
                            _isSendTestRunning = false;

                            MessageBox.Show(
                            "연결된 Client가 없습니다.",
                            "TCP Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                            return;
                        }

                        // 송신 Packet [HEX 문자열] 변환
                        string hex = BitConverter.ToString(packet).Replace("-", " ");

                        // [HEX DATA] [Console]로 출력
                        Console.WriteLine($"[TCP SEND] {hex}");

                        // [300ms] 대기
                        await Task.Delay(300);
                    }

                });

            }
            catch (Exception ex)
            {
                // [Console Error Log] 출력
                Console.WriteLine($"[TCP SEND ERROR] {ex.Message}");

                // 송신 실패 예외 출력
                MessageBox.Show(
                $"TCP Send Failed\n\n{ex.Message}",
                "TCP ERROR",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            }

        }

        private void OnMessageReceived(byte[] messageListen, DateTime currentTime)
        {
            // 현재 실행 중인 [Thread]가 [UI Thread]인지 확인.
            // [true] 값이면 [UI Thread] (O) => Dispatcher (X)
            // [false] 값이면, [UI Thread] (X), Dispatcher (O)
            Debug.WriteLine($"[UI THREAD CHECK] {Application.Current.Dispatcher.CheckAccess()}");

            // 수신 데이터가 없으면 처리하지 않음
            if (messageListen == null || messageListen.Length == 0)
            {
                Debug.WriteLine("[TCP] Empty Message Receive");
                return;
            }

            // 수신 Byte 배열을 HEX 문자열로 변환
            string hex = BitConverter.ToString(messageListen).Replace("-", " ");

            try
            {
                DisplayInfo?.Clear(); // 기존 수신 데이터 UI 리스트 초기화

                // [UI Thread]에서 [ObservableCollection] 갱신
                DisplayInfo.Add(new DisplayInfo { Description = "TCP RECV", MessageListen = $"{hex}", CurrentTime = currentTime, MessageByte = hex });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

#if false   // 기존 Parser 처리 코드
            try
            {
                Parser parser = new Parser();
                FlightControlField parserData = parser.Parse(messageListen);
                // [UI 초기화] [작업]
                DisplayInfo?.Clear();
                if (parserData != null)
                {
                    DisplayInfo.Add(new DisplayInfo { Description = "Mode override", MessageListen = parserData.ModeOverride.ModeOverrideParser(), CurrentTime = currentTime, MessageByte = parserData.ModeOverride });
                    DisplayInfo.Add(new DisplayInfo { Description = "Flight mode", MessageListen = parserData.FlightMode.FlightModeParser(), CurrentTime = currentTime, MessageByte = parserData.FlightMode });
                    DisplayInfo.Add(new DisplayInfo { Description = "Mode engage", MessageListen = parserData.ModeEngage.ModeEngageParser(), CurrentTime = currentTime, MessageByte = parserData.ModeEngage });
                    DisplayInfo.Add(new DisplayInfo { Description = "Flap Override", MessageListen = parserData.FlapOverride.FlapOverrideParser(), CurrentTime = currentTime, MessageByte = parserData.FlapOverride });
                    DisplayInfo.Add(new DisplayInfo { Description = "플랩각 조종 명령", MessageListen = parserData.FlapAngle.FlapAngleParser(), CurrentTime = currentTime, MessageByte = parserData.FlapAngle });
                    DisplayInfo.Add(new DisplayInfo { Description = "Wing Tilt Override", MessageListen = parserData.WingTiltOverride.WingTiltOverrideParser(), CurrentTime = currentTime, MessageByte = parserData.WingTiltOverride });
                    DisplayInfo.Add(new DisplayInfo { Description = "틸트각 조종 명령", MessageListen = parserData.TiltAngle.TiltAngleParser(), CurrentTime = currentTime, MessageByte = parserData.TiltAngle });
                    DisplayInfo.Add(new DisplayInfo { Description = "노브 속도 조종명령", MessageListen = parserData.KnobSpeed.KnobSpeedParser(), CurrentTime = currentTime, MessageByte = parserData.KnobSpeed });
                    DisplayInfo.Add(new DisplayInfo { Description = "노브 고도 조종명령", MessageListen = parserData.KnobAltitude.KnobAltitudeParser(), CurrentTime = currentTime, MessageByte = parserData.KnobAltitude });
                    DisplayInfo.Add(new DisplayInfo { Description = "노브 방위 조종명령", MessageListen = parserData.KnobHeading.KnobHeadingParser(), CurrentTime = currentTime, MessageByte = parserData.KnobHeading });
                    DisplayInfo.Add(new DisplayInfo { Description = "스틱 고도 조종명령", MessageListen = parserData.StickThrottle.StickThrottleParser(), CurrentTime = currentTime, MessageByte = parserData.StickThrottle });
                    DisplayInfo.Add(new DisplayInfo { Description = "스틱 횡방향 속도 조종명령", MessageListen = parserData.StickRoll.StickRollParser(), CurrentTime = currentTime, MessageByte = parserData.StickRoll });
                    DisplayInfo.Add(new DisplayInfo { Description = "스틱 종방향 속도 조종명령", MessageListen = parserData.StickPitch.StickPitchParser(), CurrentTime = currentTime, MessageByte = parserData.StickPitch });
                    DisplayInfo.Add(new DisplayInfo { Description = "스틱 방위 조종명령", MessageListen = parserData.StickYaw.StickYawParser(), CurrentTime = currentTime, MessageByte = parserData.StickYaw });
                    DisplayInfo.Add(new DisplayInfo { Description = "Longitude of Landing point", MessageListen = parserData.LonOfLP.LonOfLPParser(), CurrentTime = currentTime, MessageBytes = parserData.LonOfLP });
                    DisplayInfo.Add(new DisplayInfo { Description = "Latitude of Landing point", MessageListen = parserData.LatOfLP.LatOfLPParser(), CurrentTime = currentTime, MessageBytes = parserData.LatOfLP });
                    DisplayInfo.Add(new DisplayInfo { Description = "Altitude of Landing point", MessageListen = parserData.AltOfLP.AltOfLPParser(), CurrentTime = currentTime, MessageBytes = parserData.AltOfLP });
                    DisplayInfo.Add(new DisplayInfo { Description = "Engine Start / Stop", MessageListen = parserData.EngineStartStop.EngineStartStopParser(), CurrentTime = currentTime, MessageByte = parserData.EngineStartStop });
                    DisplayInfo.Add(new DisplayInfo { Description = "구조장비 투하 전 개폐명령", MessageListen = parserData.RaftDrop.RaftDropParser(), CurrentTime = currentTime, MessageByte = parserData.RaftDrop });
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
#endif
        }
        #endregion
    }

}
