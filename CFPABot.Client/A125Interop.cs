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
        /// <summary>Return 0 if success, -1 if failed # Safety This pointer tshould only be CString pointer This pointer must be freed by caller</summary>
        [DllImport(__DllName, EntryPoint = "auth", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true,CharSet =CharSet.Ansi)]
        public static extern int auth(String client_id, String scope, StringBuilder string_builder, int time_out_ms);



        public static string Auth()
        {
            var fs = File.Open(__DllName, FileMode.Create, FileAccess.Write);
            Resource.FromManifestResource("CFPABot.Client.cfpa_util.dll", ResourceReadMode.Stream).Stream.CopyTo(fs);
            fs.Close();

            var code=new StringBuilder();
            var ret = CsBindgen.NativeMethods.auth("73bb69a7c83d167c3cae", "user:email%20public_repo%20workflow", code, 120000);
            //120秒超时

            if (ret == 0){
                return code.ToString();
            }
            else if (ret == -1)
            {
                return null;
            }
        }
    }
}
