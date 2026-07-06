using System.Diagnostics;

namespace JHLib.ChartManager.Report
{
    public static class SSE
    {
        public delegate void ErrorDelegate(string message);
        public static event ErrorDelegate? Invoked;

        public enum StandardError
        {
            UNKNOWN,
            ERROR_01,
            ERROR_02,
            ERROR_03,
            ERROR_04,
            ERROR_05,
            ERROR_06,
            ERROR_07,
            ERROR_08,
            ERROR_09,
            ERROR_10,
            ERROR_11,
            ERROR_12,
            ERROR_13,
            ERROR_14,
            ERROR_15,
            ERROR_16,
            ERROR_17,
            ERROR_18,
            ERROR_19,
            ERROR_20,
            ERROR_21,
            ERROR_22,
            ERROR_23,
            ERROR_24,
            ERROR_25,
            ERROR_26,
            ERROR_27,
        }



        public static string GetMessage(StandardError error, string? target)
        {
            return error switch {
                StandardError.ERROR_01 => "SSE 01 - Self Signed Key is invalid.",
                StandardError.ERROR_02 => "SSE 02 - Format of Self Signed Key file is incorrect.",
                StandardError.ERROR_03 => "SSE 03 - SA Signed Data Server Certificate is invalid.",
                StandardError.ERROR_04 => "SSE 04 - Format of SA Signed DS Certificate is incorrect.",
                StandardError.ERROR_05 => $"{target} : SSE 05 - SA Digital Certificate file is not available. A valid certificate can be obtained from the IHO website or from your data supplier.",
                StandardError.ERROR_06 => $"{target} : SSE 06 - The SA Signed Data Server Certificate is invalid. The SA may have issued a new public key or the ENC may originate from another service. A new SA public key can be obtained from the IHO website or from your data supplier.",
                StandardError.ERROR_07 => "SSE 07 - SA signed DS Certificate file is not available. A valid certificate can be obtained from the IHO website or your data supplier.",
                StandardError.ERROR_08 => $"{(string.IsNullOrEmpty(target) ? "" : $"{target} : ")}SSE 08 - SA Digital Certificate file incorrect format. A valid certificate can be obtained from the IHO website or your data supplier.",
                StandardError.ERROR_09 => $"{target} : SSE 09 - ENC Signature is invalid.",
                StandardError.ERROR_10 => $"{target} : SSE 10 - Permits not available for this data provider. Contact your data supplier to obtain the correct permits.",
                StandardError.ERROR_11 => "SSE 11 - Cell Permit file not found. Load the permit file provided by the data supplier.",
                StandardError.ERROR_12 => $"{target} : SSE 12 - Cell Permit format is incorrect. Contact your data supplier and obtain a new permit file.",
                StandardError.ERROR_13 => $"{target} : SSE 13 - Cell Permit is invalid (checksum is incorrect) or the Cell Permit is for a different system. Contact your data supplier and obtain a new permit file.",
                StandardError.ERROR_14 => "SSE 14 - Incorrect system date, check that the computer clock(if accessible) is set correctly or contact your system supplier.",
                StandardError.ERROR_15 => $"{target} : SSE 15 - Subscription service has expired. Please contact your data supplier to renew the subscription licence.",
                StandardError.ERROR_16 => $"{target} : SSE 16 - ENC CRC value is incorrect. Contact your data supplier as ENC(s) may be corrupted or missing data.",
                StandardError.ERROR_17 => "SSE 17 - Userpermit is invalid (checksum is incorrect). Check that the correct hardware device (dongle) is connected or contact your system supplier to obtain a valid userpermit.",
                StandardError.ERROR_18 => "SSE 18 - HW_ID is incorrect format.",
                StandardError.ERROR_19 => "SSE 19 - Permits are not valid for this system. Contact your data supplier to obtain the correct permits.",
                StandardError.ERROR_20 => $"{target} : SSE 20 - Subscription service will expire in less than 30 days. Please contact your data supplier to renew the subscription licence.",
                StandardError.ERROR_21 => $"{target} : SSE 21 - Decryption failed no valid cell permit found. Permits may be for another system or new permits may be required, please contact your supplier to obtain a new licence.",
                StandardError.ERROR_22 => $"{(string.IsNullOrEmpty(target) ? "" : $"{target} : ")}SSE 22 - SA Digital Certificate file has expired. A new SA public key(certificate) can be obtained from the IHO website or your data supplier.",
                StandardError.ERROR_23 => $"{target} : SSE 23 - Non sequential update, previous update(s) missing try reloading from the base media. If the problem persists contact your data supplier.",
                StandardError.ERROR_24 => $"{target} : SSE 24 - ENC Signature format is incorrect, contact your data supplier.",
                StandardError.ERROR_25 => $"{target} : SSE 25 - The ENC permit for this cell has expired. This cell may be out of date and MUST NOT be used for Primary NAVIGATION.",
                StandardError.ERROR_26 => $"{(string.IsNullOrEmpty(target) ? "" : $"{target} : ")}SSE 26 - This ENC is not authenticated by the IHO acting as the Scheme Administrator.",
                StandardError.ERROR_27 => $"SSE 27 - ENC <{target}> is not up to date. A New Edition, Re-issue or Update for this cell is missing and therefore MUST NOT be used for Primary NAVIGATION.",
                _ => $"{target} : UNKNOWN - Unknown Error.",
            };
        }


        public static void InvokeStandardError(StandardError error)
        {
            SSE.InvokeStandardError(error, null);
        }

        public static void InvokeStandardError(StandardError error, string? target)
        {
            Task.Run(() => {
                string errorMessage = SSE.GetMessage(error, target);

                Trace.WriteLine(errorMessage);
                SSE.Invoked?.Invoke(errorMessage);
            });
        }

        internal static void InvokeGenericError(string message)
        {
            Task.Run(() => {
                Trace.WriteLine(message);
                SSE.Invoked?.Invoke(message);
            });
        }
    }
}