using System;
using System.Collections.Generic;
using Npgsql;
using GromCore.Laser.Server.Database.Models;
using GromCore.Laser.Server.Utils;
using Newtonsoft.Json;
using GromCore.Laser.Server.Settings;

namespace GromCore.Laser.Server.Database
{
    public static class AccountLinkSystem
    {
        private static string ConnectionString;

        public static void Init(string user, string password, string ip)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.Host = ip;
            builder.Username = user;
            builder.Password = password;
            builder.Database = Configuration.Instance.DatabaseName;
            builder.Encoding = "UTF8";

            ConnectionString = builder.ToString();
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var checkTableCmd = new NpgsqlCommand(@"
                        SELECT COUNT(*)
                        FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                          AND table_name = 'account_links'", connection);

                    long tableExists = Convert.ToInt64(checkTableCmd.ExecuteScalar());

                    if (tableExists == 0)
                    {
                        var createTableCmd = new NpgsqlCommand(@"
                            CREATE TABLE account_links (
                              ""AccountId"" integer NOT NULL PRIMARY KEY,
                              ""AccountLogin"" varchar(255) NOT NULL,
                              ""AccountEmail"" varchar(255) DEFAULT NULL,
                              ""AccountPassword"" varchar(255) NOT NULL,
                              ""AccountLinkedToTelegram"" boolean NOT NULL DEFAULT false,
                              ""AccountTelegramLinkedId"" bigint DEFAULT NULL,
                              ""LinkToken"" varchar(255) DEFAULT NULL,
                              ""VerificationCode"" integer DEFAULT NULL,
                              ""VerificationCodeAlive"" timestamp DEFAULT NULL,
                              ""TelegramLinkCode"" varchar(255) DEFAULT NULL,
                              ""TelegramLinkCodeExpire"" timestamp DEFAULT NULL
                            );", connection);

                        createTableCmd.ExecuteNonQuery();
                        // Console.WriteLine("[AccountLinkSystem] Table 'account_links' successfully created!");
                    }
                    else
                    {
                        // Console.WriteLine("[AccountLinkSystem] Table 'account_links' exists. Successfully started!");
                    }

                    var checkRowsCmd = new NpgsqlCommand("SELECT COUNT(*) FROM account_links", connection);
                    long rowCount = Convert.ToInt64(checkRowsCmd.ExecuteScalar());

                    if (rowCount == 0)
                    {
                        // Console.WriteLine("[AccountLinkSystem] Table 'account_links' empty.");
                    }

                    connection.Close();
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"[AccountLinkSystem] ERROR: {ex.Message}, Trace: {ex.StackTrace}");
                }
            }
        }

        public static AccountLinkModel Create(int accountId, string login, string email, string hashedPassword)
        {
            var accountLink = new AccountLinkModel
            {
                AccountId = accountId,
                AccountLogin = login,
                AccountEmail = email,
                AccountPassword = hashedPassword,
                LinkToken = Helpers.GenerateToken(accountId),
                AccountLinkedToTelegram = false,
                AccountTelegramLinkedId = null,
                VerificationCode = 0,
                VerificationCodeAlive = DateTime.Now
            };

            Save(accountLink);
            return accountLink;
        }

        public static void Save(AccountLinkModel model)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                var cmd = new NpgsqlCommand(@"
                    INSERT INTO account_links 
                    (""AccountId"", ""AccountLogin"", ""AccountEmail"", ""AccountPassword"", ""AccountLinkedToTelegram"", ""AccountTelegramLinkedId"", ""LinkToken"", ""VerificationCode"", ""VerificationCodeAlive"", ""TelegramLinkCode"", ""TelegramLinkCodeExpire"")
                    VALUES 
                    (@id, @login, @email, @password, @tgLinked, @tgId, @token, @code, @codeAlive, @linkCode, @linkCodeExpire)
                    ON CONFLICT (""AccountId"") DO UPDATE SET
                        ""AccountLogin"" = @login,
                        ""AccountEmail"" = @email,
                        ""AccountPassword"" = @password,
                        ""AccountLinkedToTelegram"" = @tgLinked,
                        ""AccountTelegramLinkedId"" = @tgId,
                        ""LinkToken"" = @token,
                        ""VerificationCode"" = @code,
                        ""VerificationCodeAlive"" = @codeAlive,
                        ""TelegramLinkCode"" = @linkCode,
                        ""TelegramLinkCodeExpire"" = @linkCodeExpire", connection);

                cmd.Parameters.AddWithValue("@id", model.AccountId);
                cmd.Parameters.AddWithValue("@login", model.AccountLogin);
                cmd.Parameters.AddWithValue("@email", model.AccountEmail ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@password", model.AccountPassword);
                cmd.Parameters.AddWithValue("@tgLinked", model.AccountLinkedToTelegram);
                cmd.Parameters.AddWithValue("@tgId", model.AccountTelegramLinkedId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@token", model.LinkToken);
                cmd.Parameters.AddWithValue("@code", model.VerificationCode);
                cmd.Parameters.AddWithValue("@codeAlive", model.VerificationCodeAlive);
                cmd.Parameters.AddWithValue("@linkCode", model.TelegramLinkCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@linkCodeExpire", model.TelegramLinkCodeExpire != default ? model.TelegramLinkCodeExpire : (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        public static AccountLinkModel Load(int accountId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM account_links WHERE \"AccountId\" = @id", connection);
                cmd.Parameters.AddWithValue("@id", accountId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new AccountLinkModel
                        {
                            AccountId = reader.GetInt32(reader.GetOrdinal("AccountId")),
                            AccountLogin = reader.GetString(reader.GetOrdinal("AccountLogin")),
                            AccountEmail = reader.IsDBNull(reader.GetOrdinal("AccountEmail")) ? null : reader.GetString(reader.GetOrdinal("AccountEmail")),
                            AccountPassword = reader.GetString(reader.GetOrdinal("AccountPassword")),
                            AccountLinkedToTelegram = reader.GetBoolean(reader.GetOrdinal("AccountLinkedToTelegram")),
                            AccountTelegramLinkedId = reader.IsDBNull(reader.GetOrdinal("AccountTelegramLinkedId")) ? null : reader.GetInt64(reader.GetOrdinal("AccountTelegramLinkedId")),
                            LinkToken = reader.IsDBNull(reader.GetOrdinal("LinkToken")) ? null : reader.GetString(reader.GetOrdinal("LinkToken")),
                            VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? 0 : reader.GetInt32(reader.GetOrdinal("VerificationCode")),
                            VerificationCodeAlive = reader.IsDBNull(reader.GetOrdinal("VerificationCodeAlive")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("VerificationCodeAlive")),
                            TelegramLinkCode = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCode")) ? null : reader.GetString(reader.GetOrdinal("TelegramLinkCode")),
                            TelegramLinkCodeExpire = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCodeExpire")) ? default : reader.GetDateTime(reader.GetOrdinal("TelegramLinkCodeExpire"))
                        };
                    }
                }
            }

            return null;
        }

        public static AccountLinkModel LoadByLogin(string login)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM account_links WHERE \"AccountLogin\" = @login", connection);
                cmd.Parameters.AddWithValue("@login", login);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new AccountLinkModel
                        {
                            AccountId = reader.GetInt32(reader.GetOrdinal("AccountId")),
                            AccountLogin = reader.GetString(reader.GetOrdinal("AccountLogin")),
                            AccountEmail = reader.IsDBNull(reader.GetOrdinal("AccountEmail")) ? null : reader.GetString(reader.GetOrdinal("AccountEmail")),
                            AccountPassword = reader.GetString(reader.GetOrdinal("AccountPassword")),
                            AccountLinkedToTelegram = reader.GetBoolean(reader.GetOrdinal("AccountLinkedToTelegram")),
                            AccountTelegramLinkedId = reader.IsDBNull(reader.GetOrdinal("AccountTelegramLinkedId")) ? null : reader.GetInt64(reader.GetOrdinal("AccountTelegramLinkedId")),
                            LinkToken = reader.IsDBNull(reader.GetOrdinal("LinkToken")) ? null : reader.GetString(reader.GetOrdinal("LinkToken")),
                            VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? 0 : reader.GetInt32(reader.GetOrdinal("VerificationCode")),
                            VerificationCodeAlive = reader.IsDBNull(reader.GetOrdinal("VerificationCodeAlive")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("VerificationCodeAlive")),
                            TelegramLinkCode = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCode")) ? null : reader.GetString(reader.GetOrdinal("TelegramLinkCode")),
                            TelegramLinkCodeExpire = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCodeExpire")) ? default : reader.GetDateTime(reader.GetOrdinal("TelegramLinkCodeExpire"))
                        };
                    }
                }
            }

            return null;
        }

        public static AccountLinkModel LoadByTelegramId(long telegramId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM account_links WHERE \"AccountTelegramLinkedId\" = @tgId", connection);
                cmd.Parameters.AddWithValue("@tgId", telegramId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new AccountLinkModel
                        {
                            AccountId = reader.GetInt32(reader.GetOrdinal("AccountId")),
                            AccountLogin = reader.GetString(reader.GetOrdinal("AccountLogin")),
                            AccountEmail = reader.IsDBNull(reader.GetOrdinal("AccountEmail")) ? null : reader.GetString(reader.GetOrdinal("AccountEmail")),
                            AccountPassword = reader.GetString(reader.GetOrdinal("AccountPassword")),
                            AccountLinkedToTelegram = reader.GetBoolean(reader.GetOrdinal("AccountLinkedToTelegram")),
                            AccountTelegramLinkedId = reader.IsDBNull(reader.GetOrdinal("AccountTelegramLinkedId")) ? null : reader.GetInt64(reader.GetOrdinal("AccountTelegramLinkedId")),
                            LinkToken = reader.IsDBNull(reader.GetOrdinal("LinkToken")) ? null : reader.GetString(reader.GetOrdinal("LinkToken")),
                            VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? 0 : reader.GetInt32(reader.GetOrdinal("VerificationCode")),
                            VerificationCodeAlive = reader.IsDBNull(reader.GetOrdinal("VerificationCodeAlive")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("VerificationCodeAlive")),
                            TelegramLinkCode = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCode")) ? null : reader.GetString(reader.GetOrdinal("TelegramLinkCode")),
                            TelegramLinkCodeExpire = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCodeExpire")) ? default : reader.GetDateTime(reader.GetOrdinal("TelegramLinkCodeExpire"))
                        };
                    }
                }
            }

            return null;
        }

        public static AccountLinkModel GetByTelegramLinkCode(string code)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM account_links WHERE \"TelegramLinkCode\" = @code", connection);
                cmd.Parameters.AddWithValue("@code", code);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new AccountLinkModel
                        {
                            AccountId = reader.GetInt32(reader.GetOrdinal("AccountId")),
                            AccountLogin = reader.GetString(reader.GetOrdinal("AccountLogin")),
                            AccountEmail = reader.IsDBNull(reader.GetOrdinal("AccountEmail")) ? null : reader.GetString(reader.GetOrdinal("AccountEmail")),
                            AccountPassword = reader.GetString(reader.GetOrdinal("AccountPassword")),
                            AccountLinkedToTelegram = reader.GetBoolean(reader.GetOrdinal("AccountLinkedToTelegram")),
                            AccountTelegramLinkedId = reader.IsDBNull(reader.GetOrdinal("AccountTelegramLinkedId")) ? null : reader.GetInt64(reader.GetOrdinal("AccountTelegramLinkedId")),
                            LinkToken = reader.IsDBNull(reader.GetOrdinal("LinkToken")) ? null : reader.GetString(reader.GetOrdinal("LinkToken")),
                            VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? 0 : reader.GetInt32(reader.GetOrdinal("VerificationCode")),
                            VerificationCodeAlive = reader.IsDBNull(reader.GetOrdinal("VerificationCodeAlive")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("VerificationCodeAlive")),
                            TelegramLinkCode = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCode")) ? null : reader.GetString(reader.GetOrdinal("TelegramLinkCode")),
                            TelegramLinkCodeExpire = reader.IsDBNull(reader.GetOrdinal("TelegramLinkCodeExpire")) ? default : reader.GetDateTime(reader.GetOrdinal("TelegramLinkCodeExpire"))
                        };
                    }
                }
            }
            return null;
        }

        public static void SetTelegramLinkCode(int accountId, string code)
        {
            var link = Load(accountId);
            if (link == null) return;

            link.TelegramLinkCode = code;
            link.TelegramLinkCodeExpire = DateTime.Now.AddMinutes(5);
            Save(link);
        }

        public static bool UpdatePasswordWithTelegramConfirmation(int accountId, string newPassword)
        {
            var link = Load(accountId);
            if (link == null || link.AccountTelegramLinkedId == null)
                return false;

            link.AccountPassword = newPassword;
            Save(link);
            return true;
        }

        public static void UnlinkTelegram(int accountId)
        {
            var link = Load(accountId);
            if (link != null)
            {
                link.AccountTelegramLinkedId = null;
                link.AccountLinkedToTelegram = false;
                Save(link);
            }
        }

        public static bool Exists(int accountId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM account_links WHERE \"AccountId\" = @id", connection);
                cmd.Parameters.AddWithValue("@id", accountId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        public static bool ExistsByLogin(string login)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM account_links WHERE \"AccountLogin\" = @login", connection);
                cmd.Parameters.AddWithValue("@login", login);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        public static bool TryAuth(string login, string password, out AccountLinkModel accountLink)
        {
            accountLink = LoadByLogin(login);
            if (accountLink == null)
            {
                return false;
            }

            if (accountLink.AccountPassword == password)
            {
                return true;
            }

            accountLink = null;
            return false;
        }

        public static void Delete(int accountId)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = new NpgsqlCommand("DELETE FROM account_links WHERE \"AccountId\" = @id", connection);
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}