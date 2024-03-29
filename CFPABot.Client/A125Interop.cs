using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GammaLibrary;
using GammaLibrary.Extensions;

namespace CFPABot.Client
{
    internal class A125Interop
    {
        const string __DllName = "cfpa_util";
        /// <summary># Safety This pointer tshould only be CString pointer</summary>
        [DllImport(__DllName, EntryPoint = "auth", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        static extern unsafe byte* auth(byte* client_id, byte* scope);

        /// <summary># Safety This should only ever be called with a pointer that was earlier obtained by calling CString::into_raw</summary>
        [DllImport(__DllName, EntryPoint = "free_c_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        static extern unsafe void free_c_string(byte* str);


        public static unsafe string Auth()
        {
            var fs = File.Open(__DllName+".dll", FileMode.Create, FileAccess.Write);
            Resource.FromManifestResource("CFPABot.Client.cfpa_util.dll", ResourceReadMode.Stream).Stream.CopyTo(fs);
            fs.Close();
            byte* client_id = (byte*)Marshal.StringToHGlobalAnsi("73bb69a7c83d167c3cae");
            byte* scope = (byte*)Marshal.StringToHGlobalAnsi("user:email%20public_repo%20workflow");
            byte* cstr = (byte*)0;

            try
            {
                cstr = auth(client_id, scope);
                return new string((sbyte*)cstr);
            }
            finally
            {
                if (cstr != (byte*)0)
                {
                    free_c_string(cstr);
                }
            }
            
        }
    }
}
