using p4gpc.maxhpsp.Template.Configuration;
using System.ComponentModel;
using static p4gpc.maxhpsp.Enums;

namespace p4gpc.maxhpsp.Configuration
{
    public class Config : Configurable<Config>
    {
        /*
            User Properties:
                - Please put all of your configurable properties here.

            By default, configuration saves as "Config.json" in mod user config folder.    
            Need more config files/classes? See Configuration.cs

            Available Attributes:
            - Category
            - DisplayName
            - Description
            - DefaultValue

            // Technically Supported but not Useful
            - Browsable
            - Localizable

            The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
        */

        [DisplayName("Debug Mode")]
        [Description("Logs additional information to the console that is useful for debugging.")]
        [DefaultValue(false)]
        public bool DebugEnabled { get; set; } = false;

        [DisplayName("Max HP")]
        [Description("Configures the max hp for ever character at every level where values of 0 will use the normal max\nYou can't actually configure it in this window just edit the json file")]
        public Dictionary<PartyMember, short[]> MaxHp { get; set; } = new()
        {
            { PartyMember.Protagonist, new short[99] },
            { PartyMember.Yosuke, new short[99] },
            { PartyMember.Chie, new short[99] },
            { PartyMember.Yukiko, new short[99] },
            { PartyMember.Kanji, new short[99] },
            { PartyMember.Rise, new short[99] },
            { PartyMember.Teddie, new short[99] },
            { PartyMember.Naoto, new short[99] },
        };

        [DisplayName("Max SP")]
        [Description("Configures the max sp for ever character at every level where values of 0 will use the normal max\nYou can't actually configure it in this window just edit the json file")]
        public Dictionary<PartyMember, short[]> MaxSp { get; set; } = new()
        {
            { PartyMember.Protagonist, new short[99] },
            { PartyMember.Yosuke, new short[99] },
            { PartyMember.Chie, new short[99] },
            { PartyMember.Yukiko, new short[99] },
            { PartyMember.Kanji, new short[99] },
            { PartyMember.Rise, new short[99] },
            { PartyMember.Teddie, new short[99] },
            { PartyMember.Naoto, new short[99] },
        };

    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        public override bool TryRunCustomConfiguration(Configurator configurator)
        {
            return base.TryRunCustomConfiguration(configurator);
        }
    }
}