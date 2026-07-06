using System;
using System.Collections.Generic;
using System.Text;

namespace JHLib.Util.Helper
{
    public static class SentenceHelper
    {
        /// <summary>
        /// NMEA 0183 Checksum 계산 (XOR)
        /// </summary>
        public static string CalculateChecksum(string sentence)
        {
            int checksum = 0;
            foreach (char c in sentence)
            {
                // '!' 와 '*' 사이의 문자들만 XOR 연산
                if (c == '!' || c == '$') continue;
                if (c == '*') break;
                checksum ^= Convert.ToByte(c);
            }
            return checksum.ToString("X2"); // 2자리 16진수 대문자 반환
        }
    }
}
