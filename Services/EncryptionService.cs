using KickOffEvent.Interface;
using System.Security.Cryptography;
using System.Text;

namespace KickOffEvent.Services
{
    public class EncryptionService : IEncryptionService
    {

        public string Encrypt(string strText)
        {
            string strEncrKey = "simpleaccounts";
            byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            try
            {
                byte[] bykey = System.Text.Encoding.UTF8.GetBytes(strEncrKey.Substring(0, 8));
                byte[] inputByteArray = System.Text.Encoding.UTF8.GetBytes(strText);
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(bykey, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                string strEncrypt = Convert.ToBase64String(ms.ToArray());
                ms.Close();
                cs.Close();
                des.Clear();
                return strEncrypt;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public string Decrypt(string strText)
        {
            byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            string sDecrKey = "simpleaccounts";
            byte[] inputByteArray = new byte[strText.Length];

            try
            {
                byte[] byKey = Encoding.UTF8.GetBytes(sDecrKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(strText);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(byKey, IV), CryptoStreamMode.Write))
                    {
                        cs.Write(inputByteArray, 0, inputByteArray.Length);
                        cs.FlushFinalBlock();

                        Encoding encoding = Encoding.UTF8;
                        string strDecrypt = encoding.GetString(ms.ToArray());
                        return strDecrypt;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
