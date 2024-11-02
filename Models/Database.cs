using Dapper;
using Microsoft.Data.Sqlite;

namespace PlayerSkin.Models
{
    public interface IDatabase
    {
        public Task Initialize(Plugin core);
        public Task<string?> GetPlayerData(ulong steamid);
        public Task InsertPlayerData(ulong steamid, string skinname);
    }

    public class Database : IDatabase
    {
        private SqliteConnection _connection;
        private Plugin? _core;

        public Database(SqliteConnection connection)
        {
            _connection = connection;
        }

        public async Task Initialize(Plugin core)
        {
            _core = core;

            _connection = new SqliteConnection($"Data Source={Path.Join(_core.ModuleDirectory, "playerskin.db")}");
            _connection.Open();

            await _connection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS player_skin (player_auth INT PRIMARY KEY, skin_name VARCHAR(64));");
        }

        public async Task<string?> GetPlayerData(ulong steamid)
        {
            var query = "SELECT * FROM player_skin WHERE player_auth = @Auth;";

            var result = await _connection.ExecuteReaderAsync(query, new
            {
                Auth = steamid
            });

            if (result == null)
                return null;

            if (!result.HasRows)
            {
                return null;
            }

            return (string?)result["skin_name"];
        }

        public async Task InsertPlayerData(ulong steamid, string skinname)
        {
            var query = "INSERT INTO player_skin (player_auth, skin_name) VALUES(@Auth, @Skinname) ON CONFLICT(player_auth) DO UPDATE SET skin_name = @Skinname";

            await _connection.ExecuteAsync(query, new
            {
                Auth = steamid,
                Skinname = skinname
            });
        }
    }
}
