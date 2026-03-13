using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace UsurperRemake.Systems;

/// <summary>
/// Guild system for online multiplayer mode (v0.52.0).
/// Provides guild creation, membership management, guild bank, and shared perks.
/// Guild data is stored in SQLite tables for persistence across sessions.
///
/// Chat commands:
///   /guild          — show guild info
///   /gcreate name   — create a guild (costs 10,000 gold)
///   /ginvite player — invite a player to your guild
///   /gleave         — leave your current guild
///   /gkick player   — kick a member (leader only)
///   /ginfo name     — view info about any guild
///   /gc message     — guild chat (members only)
///   /gbank amount   — deposit gold into guild bank
/// </summary>
public class GuildSystem
{
    public static GuildSystem? Instance { get; private set; }

    private readonly string connectionString;

    // Cache of guild membership: username -> guild name (lowercased)
    private readonly ConcurrentDictionary<string, string> membershipCache = new(StringComparer.OrdinalIgnoreCase);

    // Pending guild invites: target username -> (guild name, inviter name, expiry)
    private readonly ConcurrentDictionary<string, GuildInvite> pendingInvites = new(StringComparer.OrdinalIgnoreCase);

    public const int GuildCreationCost = 10000;
    public const int MaxGuildMembers = 20;
    public const double GuildXPBonusPerMember = 0.02; // 2% per member
    public const double MaxGuildXPBonus = 0.10; // 10% cap

    public GuildSystem(string databasePath)
    {
        connectionString = $"Data Source={databasePath}";
        InitializeTables();
        LoadMembershipCache();
        Instance = this;
    }

    private void InitializeTables()
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS guilds (
                    name TEXT PRIMARY KEY COLLATE NOCASE,
                    display_name TEXT NOT NULL,
                    motto TEXT DEFAULT '',
                    leader_username TEXT NOT NULL,
                    bank_gold INTEGER DEFAULT 0,
                    created_at TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS guild_members (
                    username TEXT PRIMARY KEY COLLATE NOCASE,
                    guild_name TEXT NOT NULL COLLATE NOCASE,
                    rank TEXT NOT NULL DEFAULT 'Member',
                    joined_at TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (guild_name) REFERENCES guilds(name)
                );

                CREATE INDEX IF NOT EXISTS idx_guild_members_guild ON guild_members(guild_name);
            ";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to initialize guild tables: {ex.Message}");
        }
    }

    private void LoadMembershipCache()
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT username, guild_name FROM guild_members";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                membershipCache[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to load membership cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the guild name for a player, or null if not in a guild.
    /// </summary>
    public string? GetPlayerGuild(string username)
    {
        return membershipCache.TryGetValue(username, out var guild) ? guild : null;
    }

    /// <summary>
    /// Get the XP bonus multiplier for a player based on their guild size.
    /// Returns 1.0 if not in a guild.
    /// </summary>
    public double GetGuildXPMultiplier(string username)
    {
        var guildName = GetPlayerGuild(username);
        if (guildName == null) return 1.0;

        int memberCount = GetMemberCount(guildName);
        double bonus = Math.Min(memberCount * GuildXPBonusPerMember, MaxGuildXPBonus);
        return 1.0 + bonus;
    }

    /// <summary>
    /// Create a new guild. Returns null on success, error message on failure.
    /// </summary>
    public string? CreateGuild(string leaderUsername, string guildName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(guildName) || guildName.Length < 2 || guildName.Length > 30)
            return "Guild name must be 2-30 characters.";

        if (GetPlayerGuild(leaderUsername) != null)
            return "You are already in a guild. Leave first with /gleave.";

        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            // Check if guild name is taken
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM guilds WHERE name = @name COLLATE NOCASE";
                checkCmd.Parameters.AddWithValue("@name", guildName);
                if (Convert.ToInt64(checkCmd.ExecuteScalar()) > 0)
                    return $"A guild named '{guildName}' already exists.";
            }

            // Create guild
            using (var createCmd = conn.CreateCommand())
            {
                createCmd.CommandText = @"
                    INSERT INTO guilds (name, display_name, leader_username)
                    VALUES (@name, @display, @leader)";
                createCmd.Parameters.AddWithValue("@name", guildName.ToLowerInvariant());
                createCmd.Parameters.AddWithValue("@display", displayName);
                createCmd.Parameters.AddWithValue("@leader", leaderUsername.ToLowerInvariant());
                createCmd.ExecuteNonQuery();
            }

            // Add leader as first member
            using (var memberCmd = conn.CreateCommand())
            {
                memberCmd.CommandText = @"
                    INSERT INTO guild_members (username, guild_name, rank)
                    VALUES (@user, @guild, 'Leader')";
                memberCmd.Parameters.AddWithValue("@user", leaderUsername.ToLowerInvariant());
                memberCmd.Parameters.AddWithValue("@guild", guildName.ToLowerInvariant());
                memberCmd.ExecuteNonQuery();
            }

            membershipCache[leaderUsername] = guildName.ToLowerInvariant();
            return null; // success
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to create guild: {ex.Message}");
            return "Failed to create guild. Please try again.";
        }
    }

    /// <summary>
    /// Add a member to a guild. Returns null on success, error message on failure.
    /// </summary>
    public string? AddMember(string username, string guildName)
    {
        if (GetPlayerGuild(username) != null)
            return "That player is already in a guild.";

        int count = GetMemberCount(guildName);
        if (count >= MaxGuildMembers)
            return "Guild is full.";

        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO guild_members (username, guild_name, rank)
                VALUES (@user, @guild, 'Member')";
            cmd.Parameters.AddWithValue("@user", username.ToLowerInvariant());
            cmd.Parameters.AddWithValue("@guild", guildName.ToLowerInvariant());
            cmd.ExecuteNonQuery();

            membershipCache[username] = guildName.ToLowerInvariant();
            return null;
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to add member: {ex.Message}");
            return "Failed to add member.";
        }
    }

    /// <summary>
    /// Remove a member from their guild.
    /// </summary>
    public string? RemoveMember(string username)
    {
        var guildName = GetPlayerGuild(username);
        if (guildName == null)
            return "Not in a guild.";

        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            // Check if they're the leader
            bool isLeader;
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT leader_username FROM guilds WHERE name = @guild";
                checkCmd.Parameters.AddWithValue("@guild", guildName);
                var leader = checkCmd.ExecuteScalar()?.ToString();
                isLeader = string.Equals(leader, username, StringComparison.OrdinalIgnoreCase);
            }

            // Remove member
            using (var delCmd = conn.CreateCommand())
            {
                delCmd.CommandText = "DELETE FROM guild_members WHERE username = @user COLLATE NOCASE";
                delCmd.Parameters.AddWithValue("@user", username.ToLowerInvariant());
                delCmd.ExecuteNonQuery();
            }

            membershipCache.TryRemove(username, out _);

            // If leader left, promote next member or disband
            if (isLeader)
            {
                using var nextCmd = conn.CreateCommand();
                nextCmd.CommandText = "SELECT username FROM guild_members WHERE guild_name = @guild ORDER BY joined_at ASC LIMIT 1";
                nextCmd.Parameters.AddWithValue("@guild", guildName);
                var nextMember = nextCmd.ExecuteScalar()?.ToString();

                if (nextMember != null)
                {
                    // Promote next member to leader
                    using var promoteCmd = conn.CreateCommand();
                    promoteCmd.CommandText = @"
                        UPDATE guilds SET leader_username = @user WHERE name = @guild;
                        UPDATE guild_members SET rank = 'Leader' WHERE username = @user AND guild_name = @guild";
                    promoteCmd.Parameters.AddWithValue("@user", nextMember);
                    promoteCmd.Parameters.AddWithValue("@guild", guildName);
                    promoteCmd.ExecuteNonQuery();
                }
                else
                {
                    // No members left — disband
                    using var disbandCmd = conn.CreateCommand();
                    disbandCmd.CommandText = "DELETE FROM guilds WHERE name = @guild";
                    disbandCmd.Parameters.AddWithValue("@guild", guildName);
                    disbandCmd.ExecuteNonQuery();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to remove member: {ex.Message}");
            return "Failed to leave guild.";
        }
    }

    /// <summary>
    /// Deposit gold into the guild bank.
    /// </summary>
    public string? DepositGold(string username, long amount)
    {
        var guildName = GetPlayerGuild(username);
        if (guildName == null)
            return "Not in a guild.";
        if (amount <= 0)
            return "Amount must be positive.";

        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE guilds SET bank_gold = bank_gold + @amount WHERE name = @guild";
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@guild", guildName);
            cmd.ExecuteNonQuery();
            return null;
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to deposit: {ex.Message}");
            return "Failed to deposit gold.";
        }
    }

    /// <summary>
    /// Get guild info for display.
    /// </summary>
    public GuildInfo? GetGuildInfo(string guildName)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT g.display_name, g.motto, g.leader_username, g.bank_gold, g.created_at,
                       (SELECT COUNT(*) FROM guild_members WHERE guild_name = g.name) as member_count
                FROM guilds g WHERE g.name = @name COLLATE NOCASE";
            cmd.Parameters.AddWithValue("@name", guildName);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var info = new GuildInfo
            {
                DisplayName = reader.GetString(0),
                Motto = reader.IsDBNull(1) ? "" : reader.GetString(1),
                LeaderUsername = reader.GetString(2),
                BankGold = reader.GetInt64(3),
                CreatedAt = reader.GetString(4),
                MemberCount = reader.GetInt32(5)
            };

            // Load members
            using var membersCmd = conn.CreateCommand();
            membersCmd.CommandText = @"
                SELECT gm.username, gm.rank, p.display_name
                FROM guild_members gm
                LEFT JOIN players p ON gm.username = p.username COLLATE NOCASE
                WHERE gm.guild_name = @guild ORDER BY gm.rank DESC, gm.joined_at ASC";
            membersCmd.Parameters.AddWithValue("@guild", guildName);
            using var mReader = membersCmd.ExecuteReader();
            info.Members = new List<GuildMemberInfo>();
            while (mReader.Read())
            {
                info.Members.Add(new GuildMemberInfo
                {
                    Username = mReader.GetString(0),
                    Rank = mReader.GetString(1),
                    DisplayName = mReader.IsDBNull(2) ? mReader.GetString(0) : mReader.GetString(2)
                });
            }

            return info;
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to get guild info: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get all guilds ordered by member count (descending), then bank gold.
    /// Used by the Guild Board on Main Street.
    /// </summary>
    public List<GuildInfo> GetAllGuilds()
    {
        var guilds = new List<GuildInfo>();
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT g.name, g.display_name, g.motto, g.leader_username, g.bank_gold, g.created_at,
                       (SELECT COUNT(*) FROM guild_members WHERE guild_name = g.name) as member_count
                FROM guilds g
                ORDER BY member_count DESC, g.bank_gold DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                guilds.Add(new GuildInfo
                {
                    DisplayName = reader.GetString(1),
                    Motto = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    LeaderUsername = reader.GetString(3),
                    BankGold = reader.GetInt64(4),
                    CreatedAt = reader.GetString(5),
                    MemberCount = reader.GetInt32(6)
                });
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Instance?.LogError("GUILD", $"Failed to get all guilds: {ex.Message}");
        }
        return guilds;
    }

    /// <summary>
    /// Get online members of a guild for chat broadcast.
    /// </summary>
    public List<string> GetOnlineGuildMembers(string guildName)
    {
        var members = new List<string>();
        var server = Server.MudServer.Instance;
        if (server == null) return members;

        foreach (var kvp in membershipCache)
        {
            if (string.Equals(kvp.Value, guildName, StringComparison.OrdinalIgnoreCase))
            {
                if (server.ActiveSessions.ContainsKey(kvp.Key.ToLowerInvariant()))
                    members.Add(kvp.Key);
            }
        }
        return members;
    }

    /// <summary>
    /// Send a guild invite to a player.
    /// </summary>
    public void SendInvite(string targetUsername, string guildName, string inviterName)
    {
        pendingInvites[targetUsername] = new GuildInvite
        {
            GuildName = guildName,
            InviterName = inviterName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2)
        };
    }

    /// <summary>
    /// Accept a pending guild invite.
    /// </summary>
    public string? AcceptInvite(string username)
    {
        if (!pendingInvites.TryRemove(username, out var invite))
            return "No pending guild invite.";

        if (DateTime.UtcNow > invite.ExpiresAt)
            return "That invite has expired.";

        return AddMember(username, invite.GuildName);
    }

    /// <summary>
    /// Decline a pending guild invite.
    /// </summary>
    public bool DeclineInvite(string username)
    {
        return pendingInvites.TryRemove(username, out _);
    }

    /// <summary>
    /// Check if a player has a pending guild invite.
    /// </summary>
    public GuildInvite? GetPendingInvite(string username)
    {
        if (pendingInvites.TryGetValue(username, out var invite))
        {
            if (DateTime.UtcNow > invite.ExpiresAt)
            {
                pendingInvites.TryRemove(username, out _);
                return null;
            }
            return invite;
        }
        return null;
    }

    private int GetMemberCount(string guildName)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM guild_members WHERE guild_name = @guild COLLATE NOCASE";
            cmd.Parameters.AddWithValue("@guild", guildName);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        catch { return 0; }
    }

    /// <summary>
    /// Check if user is the guild leader.
    /// </summary>
    public bool IsGuildLeader(string username, string guildName)
    {
        try
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT leader_username FROM guilds WHERE name = @guild COLLATE NOCASE";
            cmd.Parameters.AddWithValue("@guild", guildName);
            var leader = cmd.ExecuteScalar()?.ToString();
            return string.Equals(leader, username, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}

public class GuildInfo
{
    public string DisplayName { get; set; } = "";
    public string Motto { get; set; } = "";
    public string LeaderUsername { get; set; } = "";
    public long BankGold { get; set; }
    public string CreatedAt { get; set; } = "";
    public int MemberCount { get; set; }
    public List<GuildMemberInfo> Members { get; set; } = new();
}

public class GuildMemberInfo
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Rank { get; set; } = "Member";
}

public class GuildInvite
{
    public string GuildName { get; set; } = "";
    public string InviterName { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
