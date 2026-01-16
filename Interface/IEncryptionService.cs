namespace KickOffEvent.Interface
{
    public interface IEncryptionService
    {
        string Encrypt(string data);
        string Decrypt(string encryptedData);
    }
}
