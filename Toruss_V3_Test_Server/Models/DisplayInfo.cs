using System;

namespace Toruss_V3_Test_Server.Models
{
    public class DisplayInfo
    {
        #region [DisplayInfo] 모델

        public string Description { get; set; }

        public string MessageListen { get; set; }

        public DateTime CurrentTime { get; set; }

        public string MessageByte { get; set; } // 현재는 HEX 문자열 표시 구조로 변경, string 타입으로 대체

        // public byte MessageByte { get; set; } // 기존 [Parser] 구조에서 단일 Byte 데이터 저장용으로 사용

        public byte[] MessageBytes { get; set; }

        #endregion
    }

}
