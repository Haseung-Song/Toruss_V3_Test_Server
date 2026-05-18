using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace Toruss_V3_Test_Server.Services
{
    /// <summary>
    /// [TcpService]: [TCP Server] 역할을 담당하는 서비스 클래스
    /// </summary>
    public class TcpService
    {
        // TCP Client 접속을 대기하는 서버 소켓
        private TcpListener _tcpListener;

        // 현재 접속된 TCP Client
        private TcpClient _tcpClient;

        // TCP 송수신 Stream
        private NetworkStream _networkStream;

        // TCP Server 실행 상태
        private bool _isRunning;

        // 수신 데이터 전달 이벤트
        public event Action<byte[], DateTime> MessageReceived;

        // TCP Server Port
        private readonly int _port;

        // 생성자
        public TcpService(int port)
        {
            _port = port;
        }

        /// <summary>
        /// [TcpStart()]: TCP 서버 시작
        /// </summary>
        public async void TcpStart()
        {
            try
            {
                // TCP 서버 실행 상태 ON
                _isRunning = true;

                // 모든 IP에서 들어오는 TCP 접속을 지정 Port로 대기
                _tcpListener = new TcpListener(IPAddress.Any, _port);

                // TCP Listen 시작
                _tcpListener.Start();

                Debug.WriteLine("TCP Server Started...");

                // 서버 실행 중 계속 Client 접속 대기
                while (_isRunning)
                {
                    // Client 접속 대기
                    _tcpClient = await _tcpListener.AcceptTcpClientAsync();

                    Debug.WriteLine("TCP Client Connected...");

                    // Client와 통신할 NetworkStream 가져오기
                    _networkStream = _tcpClient.GetStream();

                    // Client 데이터 수신 처리
                    await ReceiveLoopAsync();
                }

            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("TCP 서버가 종료되었습니다.");
            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"TCP Socket Error : {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        /// <summary>
        /// [Server -> Client] Packet 송신
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SendToClient(byte[] data)
        {
            try
            {
                // Client 연결 여부 확인
                if (_tcpClient == null || !_tcpClient.Connected)
                {
                    return false;
                }

                NetworkStream stream = _tcpClient.GetStream(); // NetworkStream 가져오기

                if (stream == null || !stream.CanWrite)
                {
                    return false;
                }
                // data 배열의 0번 인덱스부터 data.Length 개수만큼
                // NetworkStream을 통해 상대방(Client)에게 전송함!
                // 1. [buffer]: 보낼 byte 배열
                // 2. [offset]: 몇 번째 인덱스부터 보낼지
                // 3. [count]: 몇 개의 byte를 보낼지
                // 즉, data 전체를 처음부터 끝까지 보내라
                stream.Write(data, 0, data.Length); // data 배열 전체를 Client에게 보냄.

                // NetworkStream에서는 Write 시점에 대부분 즉시 전송되지만,
                // 송신 처리 완료 의미를 명확히 하기 위해 Flush를 호출한다. 
                stream.Flush(); // 남은 버퍼가 있으면 즉시 밀어냄.

                return true;
            }
            catch (Exception ex)
            {
                // Console Error Log 출력
                Console.WriteLine($"[TCP SEND ERROR] {ex.Message}");

                // 사용자 MessageBox 출력
                MessageBox.Show(
                $"TCP Send Failed\n\n{ex.Message}",
                "TCP ERROR",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

                return false;
            }

        }

        /// <summary>
        /// [ReceiveLoopAsync()]
        /// TCP Client 데이터 수신 루프
        /// </summary>
        private async Task ReceiveLoopAsync()
        {
            // 수신 버퍼
            byte[] buffer = new byte[1024];
            try
            {
                // 서버 실행 중이고 Client가 연결된 동안 반복 수신
                while (_isRunning && _tcpClient != null && _tcpClient.Connected)
                {
                    // TCP 데이터 수신
                    int readSize = await _networkStream.ReadAsync(buffer, 0, buffer.Length);

                    // readSize가 0이면 Client가 연결 종료한 상태
                    if (readSize == 0)
                    {
                        Debug.WriteLine("TCP Client Disconnected...");
                        break;
                    }

                    // 실제 수신된 크기만큼 byte 배열 생성
                    byte[] messageListen = new byte[readSize];

                    // 수신 버퍼에서 실제 데이터만 복사
                    Array.Copy(buffer, messageListen, readSize);

                    // 수신 이벤트 호출
                    MessageReceived?.Invoke(messageListen, DateTime.Now);
                }

            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("TCP Stream이 닫혔습니다.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        /// <summary>
        /// [TcpStop()]: TCP 서버 종료
        /// </summary>
        public void TcpStop()
        {
            // TCP Server 실행 상태 OFF
            _isRunning = false;

            // NetworkStream 종료
            if (_networkStream != null)
            {
                _networkStream.Close();
                _networkStream.Dispose();
                _networkStream = null;
            }

            // TCP Client 종료
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }

            // TCP Listener 종료
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
                _tcpListener = null;
            }
            Debug.WriteLine("TCP Server Stopped...");
        }

    }

}