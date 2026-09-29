using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DA.Seguridad
{
    public static class EncriptacionHelper
    {
        private const string SecretKey = "C0p17@2017";

        public static string EncryptPlainTextToCipherText(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            byte[] toEncryptedArray = Encoding.UTF8.GetBytes(plainText);
            using var objMD5 = MD5.Create();
            byte[] securityKeyArray = objMD5.ComputeHash(Encoding.UTF8.GetBytes(SecretKey));
            using var objTripleDES = TripleDES.Create();
            objTripleDES.Key = securityKeyArray;
            objTripleDES.Mode = CipherMode.ECB;
            objTripleDES.Padding = PaddingMode.PKCS7;
            using var objCryptoTransform = objTripleDES.CreateEncryptor();
            byte[] resultArray = objCryptoTransform.TransformFinalBlock(toEncryptedArray, 0, toEncryptedArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string DecryptCipherTextToPlainText(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            byte[] toEncryptArray = Convert.FromBase64String(cipherText);
            using var objMD5 = MD5.Create();
            byte[] securityKeyArray = objMD5.ComputeHash(Encoding.UTF8.GetBytes(SecretKey));
            using var objTripleDES = TripleDES.Create();
            objTripleDES.Key = securityKeyArray;
            objTripleDES.Mode = CipherMode.ECB;
            objTripleDES.Padding = PaddingMode.PKCS7;
            using var objCryptoTransform = objTripleDES.CreateDecryptor();
            byte[] resultArray = objCryptoTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }

        public static bool ValidarClave(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave)) return false;
            if (clave.Length < 8) return false;

            bool tieneMayuscula = clave.Any(char.IsUpper);
            bool tieneMinuscula = clave.Any(char.IsLower);
            bool tieneNumero = clave.Any(char.IsDigit);
            bool tieneCaracterEspecial = clave.Any(c => !char.IsLetterOrDigit(c));

            return tieneMayuscula && tieneMinuscula && tieneNumero && tieneCaracterEspecial;
        }
    }
}
