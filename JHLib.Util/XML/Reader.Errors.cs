using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.XML
{
    public partial class FXReader
    {
        public void InvalidElementName([CallerMemberName] string caller = "")
        {
            Trace.WriteLine($"{caller} [InvalidElementName] : {ElementUTF8}");
        }

        public void InvalidContent([CallerMemberName] string caller = "")
        {
            Trace.WriteLine($"{caller} [InvalidContent] : {ContentUTF8}");
        }

        public void InvalidAttributeName([CallerMemberName] string caller = "")
        {
            Trace.WriteLine($"{caller} [InvalidAttributeName] : {AttrNameUTF8}");
        }

        public void InvalidAttributeValue([CallerMemberName] string caller = "")
        {
            Trace.WriteLine($"{caller} [InvalidAttributeValue] : {AttrValueUTF8}");
        }
    }
}