using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayerSkin.Models;
using static CounterStrikeSharp.API.Core.Listeners;

namespace PlayerSkin
{
    public class Plugin : BasePlugin
    {
        public override string ModuleName => "Player Skin Changer";
        public override string ModuleAuthor => "Oylsister";
        public override string ModuleVersion => "1.0";

        public Settings settings { get; set; } = new Settings();
        public Dictionary<CCSPlayerController, string> playerSkin { get; set; } = new Dictionary<CCSPlayerController, string>();
        bool configLoad = false;
        private ILogger<Plugin> _logger;
        private readonly IDatabase _database;

        public Plugin(ILogger<Plugin> logger, IDatabase database)
        {
            _logger = logger;
            _database = database;
        }

        public override void Load(bool hotReload)
        {
            ModelConfigLoad();

            RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
            RegisterListener<OnClientPutInServer>(OnClientPutInServer);
            RegisterListener<OnClientDisconnect>(OnClientDisconnect);

            _database.Initialize(this);
        }

        public void OnServerPrecacheResources(ResourceManifest manifest)
        {
            if (!configLoad || settings.ModelList == null)
            {
                _logger.LogError("Config file is not loaded!");
                return;
            }

            foreach (var model in settings.ModelList.Values)
            {
                if(model.ModelPath == null) continue;
                manifest.AddResource(model.ModelPath);
            }
        }

        public void ModelConfigLoad()
        {
            var config = Path.Combine(ModuleDirectory, "playerskin.json");

            if (!File.Exists(config))
            {
                _logger.LogCritical("Config File is not found!");
                configLoad = false;
                return;
            }

            _logger.LogInformation("Found model config file.");
            configLoad = true;
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(config))!;
        }

        public void OnClientPutInServer(int playerslot)
        {
            var client = Utilities.GetPlayerFromSlot(playerslot);

            playerSkin.Add(client!, settings.DefaultSkin!);
        }

        public void OnClientDisconnect(int playerslot)
        {
            var client = Utilities.GetPlayerFromSlot(playerslot);

            if(playerSkin.ContainsKey(client!))
            {
                playerSkin.Remove(client!);
            }
        }

        [GameEventHandler]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var client = @event.Userid;

            if (client == null || client.IsBot)
                return HookResult.Continue;

            if (!playerSkin.ContainsKey(client!))
            {
                playerSkin.Add(client, settings.DefaultSkin!);
            }

            var steamid = client.AuthorizedSteamID!.SteamId64;

            Task.Run(() => ProceedGetPlayerData(client, steamid));

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var client = @event.Userid;

            if (client == null)
                return HookResult.Continue;

            var key = playerSkin.TryGetValue(client, out var value) ? value : null;

            if (key == null)
                return HookResult.Continue;

            AddTimer(0.3f, () => ApplyPlayerModel(client, key));
            return HookResult.Continue;
        }

        public async Task ProceedGetPlayerData(CCSPlayerController client, ulong steamid)
        {
            var data = await _database.GetPlayerData(steamid);

            if(data == null)
            {
                playerSkin[client] = settings.DefaultSkin!;
                await ProceedInsertPlayerData(steamid, settings.DefaultSkin!);
                return;
            }

            playerSkin[client] = data;
        }

        public async Task ProceedInsertPlayerData(ulong steamid, string skinname)
        {
            await _database.InsertPlayerData(steamid, skinname);
        }

        [CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_skin")]
        public void PlayerSkinCommand(CCSPlayerController client, CommandInfo info)
        {
            CreatePlayerSkinMenu(client);
        }

        public void CreatePlayerSkinMenu(CCSPlayerController client)
        {
            if(!configLoad || settings.ModelList == null)
            {
                client.PrintToChat($" {ChatColors.Green}[Player Skin]{ChatColors.White} Cannot change player model, because there is no config file.");
                return;
            }

            if(settings.ModelList.Count <= 0)
            {
                client.PrintToChat($" {ChatColors.Green}[Player Skin]{ChatColors.White} Cannot change player model, because config is empty.");
                return;
            }

            var menu = new ChatMenu($" {ChatColors.Green}[Player Skin]{ChatColors.White} Choose your player skin!");

            foreach (var model in settings.ModelList)
            {
                menu.AddMenuOption(model.Value.Name!, (client, option) =>
                {
                    playerSkin[client] = model.Key;

                    var steamid = client.AuthorizedSteamID!.SteamId64;

                    Task.Run(() => ProceedInsertPlayerData(steamid, model.Key));

                    client.PrintToChat($" {ChatColors.Green}[Player Skin]{ChatColors.White} You have set player models to {model.Value.Name}.");

                    if(client.PawnIsAlive)
                        ApplyPlayerModel(client, model.Key);
                });
            }
        }

        public void ApplyPlayerModel(CCSPlayerController client, string modelKey)
        {
            if (settings.ModelList == null)
                return;

            if (!settings.ModelList.TryGetValue(modelKey, out var data))
                return;

            var pawn = client.PlayerPawn.Value;

            if (pawn == null)
                return;

            pawn.SetModel(data.ModelPath!);
        }
    }
}
