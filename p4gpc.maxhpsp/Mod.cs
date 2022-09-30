using p4gpc.maxhpsp.Configuration;
using p4gpc.maxhpsp.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using static p4gpc.maxhpsp.Enums;
using static Reloaded.Hooks.Definitions.X86.FunctionAttribute;
using IReloadedHooks = Reloaded.Hooks.Definitions.IReloadedHooks;

namespace p4gpc.maxhpsp
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private nuint currentLevel;

        private List<IAsmHook> _asmHooks = new();

        private IReverseWrapper<GetMaxHpFunction> _maxHpReverseWrapper;
        private IReverseWrapper<GetMaxSpFunction> _maxSpReverseWrapper;

        public Mod(ModContext context)
        {
            //Debugger.Launch();
            _modLoader = context.ModLoader;
            _hooks = context.Hooks!;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;
            var memory = new Memory();

            Utils.Initialise(_logger, _configuration);
            
            if(!_modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner))
            {
                Utils.LogError("Unable to get sig scanner controller, aborting launch. Please ensure that \"Library: Reloaded.Memory.Sigscan for Reloaded II\" is installed and up to date (v1.2.1 or higher)");
                return;
            }

            currentLevel = memory.Allocate(2);

            // Get current level hp
            startupScanner.AddMainModuleScan("6B C0 0B ?? ?? 0F B7 04 85 ?? ?? ?? ?? BA 10 02 00 00", (result) =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to get current level (hp) address, the mod will not work correctly");
                    return;
                }
                string[] function =
                {
                    "use32",
                    $"mov [{currentLevel}], ax"
                };
                _asmHooks.Add(_hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress, AsmHookBehaviour.ExecuteFirst).Activate());
                Utils.Log($"Activated get current level (hp) hook at 0x{result.Offset + Utils.BaseAddress:X}");
            });

            // Is enemy hp
            startupScanner.AddMainModuleScan("6B C0 0B ?? ?? 0F B7 04 85 ?? ?? ?? ?? BA 10 02 00 00", (result) =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find enemy hp address, the mod will not work correctly");
                    return;
                }
                string[] function =
                {
                    "use32",
                    $"mov word [{currentLevel}], 0"
                };
                _asmHooks.Add(_hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress - 0x3A, AsmHookBehaviour.ExecuteFirst).Activate());
                Utils.Log($"Activated enemy hp hook at 0x{result.Offset + Utils.BaseAddress - 0x3A:X}");
            });

            // Get current level sp
            startupScanner.AddMainModuleScan("6B C0 0B ?? ?? 0F B7 04 85 ?? ?? ?? ?? BA 13 02 00 00", (result) =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to get current level (sp) address, the mod will not work correctly");
                    return;
                }
                string[] function =
                {
                    "use32",
                    $"mov [{currentLevel}], ax"
                };
                _asmHooks.Add(_hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress, AsmHookBehaviour.ExecuteFirst).Activate());
                Utils.Log($"Activated get current level (sp) hook at 0x{result.Offset + Utils.BaseAddress - 0x3A:X}");
            });

            // Is enemy sp
            startupScanner.AddMainModuleScan("6B C0 0B ?? ?? 0F B7 04 85 ?? ?? ?? ?? BA 13 02 00 00", (result) =>
            {
                if(!result.Found)
                {
                    Utils.LogError($"Unable to find enemy sp address, the mod will not work correctly");
                    return;
                }
                string[] function =
                {
                    "use32",
                    $"mov word [{currentLevel}], 0"
                };
                _asmHooks.Add(_hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress - 0x3A, AsmHookBehaviour.ExecuteFirst).Activate());
                Utils.Log($"Activated enemy sp hook at 0x{result.Offset + Utils.BaseAddress - 0x3A:X}");
            });

            // Max hp
            startupScanner.AddMainModuleScan("0F 4F F8 66 ?? ?? 5F 5E 5B C3", (result) =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find max hp address, the mod will not work correctly");
                    return;
                }
                string[] function =
                {
                    "use32",
                    $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                    "mov ax, [esi + 2]", // party member id
                    $"mov cx, [{currentLevel}]",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(GetMaxHp, out _maxHpReverseWrapper)}",
                    "mov di, ax", // put the returned max sp into the register storing the max sp
                    $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
                };
                _asmHooks.Add(_hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress + 3, AsmHookBehaviour.ExecuteFirst).Activate());
                Utils.Log($"Activated set max hp hook at 0x{result.Offset + Utils.BaseAddress + 3:X}");
            });

            // Max sp
            startupScanner.AddMainModuleScan("0F 4F F8 66 ?? ?? 5F 5E 5B ?? ?? 5D C3", (result) =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find max hp address, the mod will not work correctly");
                    return;
                }
                string[] function =
                {
                    "use32",
                    $"{_hooks.Utilities.PushCdeclCallerSavedRegisters()}",
                    "mov ax, [esi + 2]", // party member id
                    $"mov cx, [{currentLevel}]",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(GetMaxSp, out _maxSpReverseWrapper)}",
                    "mov di, ax", // put the returned max sp into the register storing the max sp
                    $"{_hooks.Utilities.PopCdeclCallerSavedRegisters()}",
                };
                _asmHooks.Add(_hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress + 3, AsmHookBehaviour.ExecuteFirst).Activate());
                Utils.Log($"Activated set max sp hook at 0x{result.Offset + Utils.BaseAddress + 3:X}");
            });

        }

        private short GetMaxHp(PartyMember partyMember, short currentLevel, short currentMaxHp)
        {
            if (currentLevel <= 0 || partyMember <= 0 || partyMember > PartyMember.Naoto) // Is an enemy
                return currentMaxHp;
            if (!_configuration.MaxHp.TryGetValue(partyMember, out var maxHpArray))
                return currentMaxHp;
            return maxHpArray[currentLevel-1] == 0 ? currentMaxHp : maxHpArray[currentLevel-1];
        }

        private short GetMaxSp(PartyMember partyMember, short currentLevel, short currentMaxSp)
        {
            if (currentLevel == 0 || partyMember <= 0 || partyMember > PartyMember.Naoto) // Is an enemy
                return currentMaxSp;
            if (!_configuration.MaxSp.TryGetValue(partyMember, out var maxSpArray))
                return currentMaxSp;
            return maxSpArray[currentLevel-1] == 0 ? currentMaxSp : maxSpArray[currentLevel-1];
        }

        [Function(new Register[] { Register.eax, Register.ecx, Register.edi }, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate short GetMaxHpFunction(PartyMember partyMember, short currentLevel, short currentMaxHp);
            

        [Function(new Register[] { Register.eax, Register.ecx, Register.edi }, Register.eax, StackCleanup.Callee)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate short GetMaxSpFunction(PartyMember partyMember, short currentLevel, short currentMaxHp);

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}