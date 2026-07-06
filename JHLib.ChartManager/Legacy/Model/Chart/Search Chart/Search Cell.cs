using System.IO;

namespace Legacy.ECM_Core.Chart
{
    public struct SearchCell
    {
        public FileInfo File;

        public int EDTN;
        public int UPDN;

        public bool Signature_Validation;
        public bool Necessary_Validation;
    }
}