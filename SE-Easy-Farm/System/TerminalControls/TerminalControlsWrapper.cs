using System;
using EasyFarming.System.Config;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.GUI.TextPanel;

namespace EasyFarming.System.TerminalControls
{
    /// <summary>
    ///     Wrapper around <see cref="IMyTerminalControl" />, contains meta-information about the controls,
    ///     the intended script to have it, and its required methods
    /// </summary>
    public abstract class TerminalControlsWrapper
    {
        /// <summary>
        ///     Controls to be displayed on the Terminal
        /// </summary>
        public abstract IMyTerminalControl TerminalControl { get; }

        /// <summary>
        ///     Prefix for ID of every control
        /// </summary>
        protected virtual string IdPrefix { get; } = "EasyFarming_";
        

        /// <summary>
        ///     Getter for controls "Visible" property
        /// </summary>
        /// <param name="block">Reference block</param>
        /// <returns>Boolean indicating if the block is visible or not</returns>
        public virtual bool Visible(IMyTerminalBlock block)
        {
            var config = ConfigManager.GetConfigForBlock(block);
            return config != null;
        }


        /// <summary>
        ///     Create control for <see cref="TControlType" /> for <see cref="IMyTerminalBlock" />
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="TControlType">Type of the control</typeparam>
        /// <returns></returns>
        protected TControlType CreateControl<TControlType>(string id)
        {
            return MyAPIGateway.TerminalControls.CreateControl<TControlType, IMyTerminalBlock>(
                IdPrefix + id);
        }
    }
}