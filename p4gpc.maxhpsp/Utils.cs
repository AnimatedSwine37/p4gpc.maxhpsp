using p4gpc.maxhpsp.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p4gpc.maxhpsp
{
    internal unsafe class Utils
    {
        private static ILogger _logger;
        private static Config _config;

        internal static int BaseAddress { get; private set; }

        internal static unsafe void Initialise(ILogger logger, Config config)
        {
            _logger = logger;
            _config = config;
            using var thisProcess = Process.GetCurrentProcess();
            BaseAddress = thisProcess.MainModule!.BaseAddress.ToInt32();
        }

        internal static void LogDebug(string message)
        {
            if (_config.DebugEnabled)
                _logger.WriteLine($"[Max HP & SP] {message}");
        }

        internal static void Log(string message)
        {
            _logger.WriteLine($"[Max HP & SP] {message}");
        }

        internal static  void LogError(string message, Exception e)
        {
            _logger.WriteLine($"[Max HP & SP] {message}: {e.Message}", System.Drawing.Color.Red);
        }

        internal static void LogError(string message)
        {
            _logger.WriteLine($"[Max HP & SP] {message}", System.Drawing.Color.Red);
        }

        /// <summary>
        /// Pushes the value of an xmm register to the stack, saving it so it can be restored with PopXmm
        /// </summary>
        /// <param name="xmmNum">The number of the xmm register to push</param>
        /// <returns>A string of assembly instructions that will push the xmm register's value to the stack</returns>
        internal static string PushXmm(int xmmNum)
        {
            return // Save an xmm register 
                $"sub esp, 16\n" + // allocate space on stack
                $"movdqu dqword [esp], xmm{xmmNum}\n";
        }

        /// <summary>
        /// Pushes all xmm registers (0-7) to the stack, saving them to be restored with PopXmm
        /// </summary>
        /// <returns>A string of assembly instructions that will push all xmm register's values to the stack</returns>
        public static string PushXmm()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                sb.Append(PushXmm(i));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Pops the value of an xmm register from the stack, restoring it after being saved with PushXmm
        /// </summary>
        /// <param name="xmmNum">The number of the xmm register to pop</param>
        /// <returns>A string of assembly instructions that will pop the value from the stack back into the xmm register</returns>
        public static string PopXmm(int xmmNum)
        {
            return                 //Pop back the value from stack to xmm
                $"movdqu xmm{xmmNum}, dqword [esp]\n" +
                $"add esp, 16\n"; // re-align the stack
        }

        /// <summary>
        /// Pops all xmm registers (0-7) from the stack, restoring them after being saved with PushXmm
        /// </summary>
        /// <returns>A string of assembly instructions that will pop the values from the stack back into the xmm registers</returns>
        public static string PopXmm()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 7; i >= 0; i--)
            {
                sb.Append(PopXmm(i));
            }
            return sb.ToString();
        }
    }
}
