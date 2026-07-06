namespace JHLib.ChartManager.ENC
{
    public class Certificate
    {
        public string name;

        public int? type = null;
        public string? status = null;
        public DateTime? installTime = null;

        public string? CN = null;
        public string? OU = null;
        public string? L = null;
        public string? O = null;
        public string? C = null;
        public string? S = null;

        public string? effectiveDate = null;
        public string? expirationDate = null;

        public PublicKey? publicKey = null;



        public Certificate(string name)
        {
            this.name = name;
        }



        public bool Authenticate(Signature signature)
        {
            if ((signature.sign != null) && (signature.digest != null) && (this.publicKey != null))
            {
                return this.publicKey.Authenticate(signature.digest.Value, signature.sign[1]);
            }
            else
            {
                return false;
            }
        }
    }
}