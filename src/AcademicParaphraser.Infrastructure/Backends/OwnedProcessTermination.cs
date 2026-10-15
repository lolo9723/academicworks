using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AcademicParaphraser.Infrastructure.Backends
{
    internal static class OwnedProcessTermination
    {
        internal static async Task StopAsync(Process? process)
        {
            if(process==null)return;
            try
            {
                if(process.HasExited)return;
                process.Kill();
                if(!await Task.Run(()=>process.WaitForExit(3000)).ConfigureAwait(false))
                    throw new InvalidOperationException("Sahip olunan dil işlemi zamanında kapanmadı.");
            }
            catch(InvalidOperationException) when(IsUnavailable(process)){}
            catch(System.ComponentModel.Win32Exception) when(IsUnavailable(process)){}
        }
        private static bool IsUnavailable(Process process)
        {
            try{return process.HasExited;}catch(InvalidOperationException){return true;}
        }
    }
}
