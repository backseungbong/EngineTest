using Legacy.ECM_Core.Enumeration;
using System.Diagnostics;

namespace Legacy.ECM_Core
{
    public static class StandardError
    {
        public delegate void ErrorDelegate(string message);
        public static event ErrorDelegate? Invoked;



        public static string Get_Message(SSE sse, string? origin)
        {
            return sse switch {
                SSE.ERROR_01 => "SSE 01 - Self Signed Key is invalid.",
                SSE.ERROR_02 => "SSE 02 - Format of Self Signed Key file is incorrect.",
                SSE.ERROR_03 => "SSE 03 - SA Signed Data Server Certificate is invalid.",
                SSE.ERROR_04 => "SSE 04 - Format of SA Signed DS Certificate is incorrect.",
                SSE.ERROR_05 => $"{origin} : SSE 05 - SA Digital Certificate file is not available. A valid certificate can be obtained from the IHO website or from your data supplier.",
                SSE.ERROR_06 => $"{origin} : SSE 06 - The SA Signed Data Server Certificate is invalid. The SA may have issued a new public key or the ENC may originate from another service. A new SA public key can be obtained from the IHO website or from your data supplier.",
                SSE.ERROR_07 => "SSE 07 - SA signed DS Certificate file is not available. A valid certificate can be obtained from the IHO website or your data supplier.",
                SSE.ERROR_08 => $"{(string.IsNullOrEmpty(origin) ? "" : $"{origin} : ")}SSE 08 - SA Digital Certificate file incorrect format. A valid certificate can be obtained from the IHO website or your data supplier.",
                SSE.ERROR_09 => $"{origin} : SSE 09 - ENC Signature is invalid.",
                SSE.ERROR_10 => $"{origin} : SSE 10 - Permits not available for this data provider. Contact your data supplier to obtain the correct permits.",
                SSE.ERROR_11 => "SSE 11 - Cell Permit file not found. Load the permit file provided by the data supplier.",
                SSE.ERROR_12 => $"{origin} : SSE 12 - Cell Permit format is incorrect. Contact your data supplier and obtain a new permit file.",
                SSE.ERROR_13 => $"{origin} : SSE 13 - Cell Permit is invalid (checksum is incorrect) or the Cell Permit is for a different system. Contact your data supplier and obtain a new permit file.",
                SSE.ERROR_14 => "SSE 14 - Incorrect system date, check that the computer clock(if accessible) is set correctly or contact your system supplier.",
                SSE.ERROR_15 => $"{origin} : SSE 15 - Subscription service has expired. Please contact your data supplier to renew the subscription licence.",
                SSE.ERROR_16 => $"{origin} : SSE 16 - ENC CRC value is incorrect. Contact your data supplier as ENC(s) may be corrupted or missing data.",
                SSE.ERROR_17 => "SSE 17 - Userpermit is invalid (checksum is incorrect). Check that the correct hardware device (dongle) is connected or contact your system supplier to obtain a valid userpermit.",
                SSE.ERROR_18 => "SSE 18 - HW_ID is incorrect format.",
                SSE.ERROR_19 => "SSE 19 - Permits are not valid for this system. Contact your data supplier to obtain the correct permits.",
                SSE.ERROR_20 => $"{origin} : SSE 20 - Subscription service will expire in less than 30 days. Please contact your data supplier to renew the subscription licence.",
                SSE.ERROR_21 => $"{origin} : SSE 21 - Decryption failed no valid cell permit found. Permits may be for another system or new permits may be required, please contact your supplier to obtain a new licence.",
                SSE.ERROR_22 => $"{(string.IsNullOrEmpty(origin) ? "" : $"{origin} : ")}SSE 22 - SA Digital Certificate file has expired. A new SA public key(certificate) can be obtained from the IHO website or your data supplier.",
                SSE.ERROR_23 => $"{origin} : SSE 23 - Non sequential update, previous update(s) missing try reloading from the base media. If the problem persists contact your data supplier.",
                SSE.ERROR_24 => $"{origin} : SSE 24 - ENC Signature format is incorrect, contact your data supplier.",
                SSE.ERROR_25 => $"{origin} : SSE 25 - The ENC permit for this cell has expired. This cell may be out of date and MUST NOT be used for Primary NAVIGATION.",
                SSE.ERROR_26 => $"{(string.IsNullOrEmpty(origin) ? "" : $"{origin} : ")}SSE 26 - This ENC is not authenticated by the IHO acting as the Scheme Administrator.",
                SSE.ERROR_27 => $"SSE 27 - ENC <{origin}> is not up to date. A New Edition, Re-issue or Update for this cell is missing and therefore MUST NOT be used for Primary NAVIGATION.",
                _ => $"{origin} : UNKNOWN - Unknown Error.",
            };
        }

        internal static void Invoke_Message(SSE sse)
        {
            Task.Run(() => {
                string Error_Message = StandardError.Get_Message(sse, null);

                Trace.WriteLine(Error_Message);
                Invoked?.Invoke(Error_Message);
            });
        }

        internal static void Invoke_Message(SSE sse, string origin)
        {
            Task.Run(() => {
                string Error_Message = StandardError.Get_Message(sse, origin);

                Trace.WriteLine(Error_Message);
                Invoked?.Invoke(Error_Message);
            });
        }
    }
}