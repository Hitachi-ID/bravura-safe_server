using System;
using System.Security.Cryptography;
using System.Text;

namespace Bit.Core.Utilities.Hypr
{
    public static class HyprWeb
    {
        private const string TxPrefix = "TX";
        private const string AuthPrefix = "AUTH";
        private const int HyprExpire = 300;
        private const int AKeyLength = 42;

        public static string ErrorUser = "ERR|The username passed to sign_request() is invalid.";
        public static string ErrorIKey = "ERR|The Hypr API Token key passed to sign_request() is invalid.";
        public static string ErrorSKey = "ERR|The Hypr secret key passed to sign_request() is invalid.";
        public static string ErrorAKey = "ERR|The application token passed must be 42 characters.";
        public static string ErrorUnknown = "ERR|An unknown error has occurred.";

        // throw on invalid bytes
        private static Encoding _encoding = new UTF8Encoding(false, true);
        private static DateTime _epoc = new DateTime(1970, 1, 1);


        public static string SignTxRequest(string skey, string username, string teamid, DateTime? currentTime = null)
        {
            return SignRequest(TxPrefix, skey, username, teamid, currentTime);
        }

        public static string SignAuthRequest(string skey, string username, string teamid, DateTime? currentTime = null)
        {
            return SignRequest(AuthPrefix, skey, username, teamid, currentTime);
        }

        private static string SignRequest(string prefix, string skey, string username, string teamid, DateTime? currentTime = null)
        {
            string hyprSig;
            var currentTimeValue = currentTime ?? DateTime.UtcNow;

            if (username == string.Empty)
            {
                return ErrorUser;
            }
            if (username.Contains("|"))
            {
                return ErrorUser;
            }

            try
            {
                hyprSig = SignVals(skey, username, teamid, prefix, HyprExpire, currentTimeValue);
            }
            catch
            {
                return ErrorUnknown;
            }

            return $"{hyprSig}";
        }

        public static (string, string) VerifyTxResponse(string skey, string sigResponse, DateTime? currentTime = null)
        {
            string txUser = null;
            string teamid = null;
            var currentTimeValue = currentTime ?? DateTime.UtcNow;

            try
            {
                (txUser, teamid) = ParseVals(skey, sigResponse, TxPrefix, currentTimeValue);
            }
            catch
            {
                return (null, null);
            }

            return (txUser, teamid);
        }

        public static string VerifyAuthResponse(string skey, string sigResponse, DateTime? currentTime = null)
        {
            string authUser = null;
            string txUser = null;
            string authTeam = null;
            string txTeam = null;
            var currentTimeValue = currentTime ?? DateTime.UtcNow;

            try
            {
                var sigs = sigResponse.Split(':');
                var authSig = sigs[0];
                var txSig = sigs[1];

                (authUser, authTeam) = ParseVals(skey, authSig, AuthPrefix, currentTimeValue);
                (txUser, txTeam) = ParseVals(skey, txSig, TxPrefix, currentTimeValue);
            }
            catch
            {
                return null;
            }

            if (authUser != txUser || authTeam != txTeam || 
                authUser is null || txUser is null ||
                authTeam is null || txTeam is null )
            {
                return null;
            }

            return authUser;
        }

        private static string SignVals(string key, string username, string teamid, string prefix, long expire,
            DateTime currentTime)
        {
            var ts = (long)(currentTime - _epoc).TotalSeconds;
            expire = ts + expire;
            var val = $"{username}|{teamid}|{expire.ToString()}";
            var cookie = $"{prefix}|{Encode64(val)}";
            var sig = Sign(key, cookie);
            return $"{cookie}|{sig}";
        }

        private static (string, string) ParseVals(string key, string val, string prefix, DateTime currentTime)
        {
            var ts = (long)(currentTime - _epoc).TotalSeconds;

            var parts = val.Split('|');
            if (parts.Length != 3)
            {
                return (null, null);
            }

            var uPrefix = parts[0];
            var uB64 = parts[1];
            var uSig = parts[2];

            var sig = Sign(key, $"{uPrefix}|{uB64}");
            if (Sign(key, sig) != Sign(key, uSig))
            {
                return (null, null);
            }

            if (uPrefix != prefix)
            {
                return (null, null);
            }

            var cookie = Decode64(uB64);
            var cookieParts = cookie.Split('|');
            if (cookieParts.Length != 3)
            {
                return (null, null);
            }

            var username = cookieParts[0];
            var teamid = cookieParts[1];
            var expire = cookieParts[2];

            var expireTs = Convert.ToInt32(expire);
            if (ts >= expireTs)
            {
                return (null, null);
            }

            return (username, teamid);
        }

        private static string Sign(string skey, string data)
        {
            var keyBytes = Encoding.ASCII.GetBytes(skey);
            var dataBytes = Encoding.ASCII.GetBytes(data);

            using (var hmac = new HMACSHA1(keyBytes))
            {
                var hash = hmac.ComputeHash(dataBytes);
                var hex = BitConverter.ToString(hash);
                return hex.Replace("-", "").ToLower();
            }
        }

        private static string Encode64(string plaintext)
        {
            var plaintextBytes = _encoding.GetBytes(plaintext);
            return Convert.ToBase64String(plaintextBytes);
        }

        private static string Decode64(string encoded)
        {
            var plaintextBytes = Convert.FromBase64String(encoded);
            return _encoding.GetString(plaintextBytes);
        }
    }
}
